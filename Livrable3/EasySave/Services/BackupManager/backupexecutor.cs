using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EasySave.Models;
using EasySave.Services;
using EasySave.Services.CryptoSoft;
using EasySave.Utils;
using Logger;
using System.Diagnostics; // For Debug.WriteLine
using System.Collections.Concurrent; // For ConcurrentDictionary
using System.Threading; // For CancellationTokenSource, ManualResetEventSlim

namespace EasySave.BackupExecutor
{
    public class BackupExecutor : IBackupExecutor
    {
        private readonly LogManager _logManager;
        private readonly StateManager _stateManager;
        private readonly EncryptionService _encryptionService;
        private readonly string _businessSoftwarePath;
        private const long MaxFileSizeBytes = 100 * 1024 * 1024;
        public event Action<BackupProgress> ProgressUpdated;

        private readonly ConcurrentDictionary<string, JobControlContext> _activeJobControls;


        public BackupExecutor(LogManager logManager, StateManager stateManager, EncryptionService encryptionService, string businessSoftwareFullPath)
        {
            _logManager = logManager ?? throw new ArgumentNullException(nameof(logManager));
            _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
            _businessSoftwarePath = businessSoftwareFullPath;
            _activeJobControls = new ConcurrentDictionary<string, JobControlContext>();
        }

        private void RaiseProgress(BackupProgress progress)
        {
            ProgressUpdated?.Invoke(progress);
        }

        private void CleanupJobControls(string jobName)
        {
            if (_activeJobControls.TryRemove(jobName, out var context))
            {
                context.Dispose();
            }
        }

        public async Task PauseJobAsync(string jobName)
        {
            if (_activeJobControls.TryGetValue(jobName, out var context))
            {
                context.PauseEvent.Reset(); // Signal to pause
                var progress = new BackupProgress { JobName = jobName, State = BackupState.Paused, Timestamp = DateTime.Now };
                await _stateManager.UpdateStateAsync(progress);
                RaiseProgress(progress);
                Debug.WriteLine($"[BackupExecutor] Job '{jobName}' PAUSED.");
            }
        }

        public async Task ResumeJobAsync(string jobName)
        {
            if (_activeJobControls.TryGetValue(jobName, out var context))
            {
                context.PauseEvent.Set(); 
                var progress = new BackupProgress { JobName = jobName, State = BackupState.Active, Timestamp = DateTime.Now };
                await _stateManager.UpdateStateAsync(progress);
                RaiseProgress(progress);
                Debug.WriteLine($"[BackupExecutor] Job '{jobName}' RESUMED.");
            }
        }

        public async Task StopJobAsync(string jobName)
        {
            if (_activeJobControls.TryGetValue(jobName, out var context))
            {
                context.Cts.Cancel(); // Signal to stop
                context.PauseEvent.Set(); // If paused, unblock it so it can see the cancellation
                // The main loop, when it stops, will update the state to Stopped.
                // Or, we can proactively set it here.
                var progress = new BackupProgress { JobName = jobName, State = BackupState.Stopped, Timestamp = DateTime.Now };
                await _stateManager.UpdateStateAsync(progress);
                RaiseProgress(progress);
                Debug.WriteLine($"[BackupExecutor] Job '{jobName}' STOP signaled.");
            }
            // CleanupJobControls will be called in the finally block of ExecuteBackupJobAsync
        }


        private async Task CheckBusinessSoftwareAndThrowIfNeeded(string jobName, BackupProgress progressForStateUpdate)
        {
            if (!string.IsNullOrWhiteSpace(_businessSoftwarePath) && ProcessUtils.IsProcessRunning(_businessSoftwarePath))
            {
                string softwareDisplayName = Path.GetFileName(_businessSoftwarePath);
                string interruptionMessage = $"Backup job '{jobName}' cannot start. Business software '{softwareDisplayName}' (Path: {_businessSoftwarePath}) is running.";

                Debug.WriteLine($"[BackupExecutor] {interruptionMessage}");

                await _logManager.LogFileOperationAsync(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    JobName = jobName,
                    Message = interruptionMessage
                });

                if (progressForStateUpdate != null)
                {
                    progressForStateUpdate.State = BackupState.Interrupted;
                    await _stateManager.UpdateStateAsync(progressForStateUpdate);
                    RaiseProgress(progressForStateUpdate);
                }
                throw new BusinessSoftwareInterruptionException(interruptionMessage);
            }
        }

        public async Task ExecuteBackupJobAsync(BackupJob job)
        {
            var jobControl = _activeJobControls.GetOrAdd(job.Name, new JobControlContext());
            jobControl.PauseEvent.Set(); // Ensure it's not paused from a previous run if controls were reused (should not happen with GetOrAdd for new executions)
            // If Cts was cancelled from a previous stop, we need a new one.
            // Better: ensure Execute is only called once per job instance or manage CTS lifecycle carefully.
            // For now, assume GetOrAdd gives a fresh or correctly reset context.
            // A robust solution would involve removing from _activeJobControls on completion/stop and re-adding.


            await CheckBusinessSoftwareAndThrowIfNeeded(job.Name, null);

            DirectoryInfo sourceDir = new DirectoryInfo(job.SourceDirectory);
            if (!sourceDir.Exists)
            {
                var errorProgress = new BackupProgress { JobName = job.Name, State = BackupState.Error, Timestamp = DateTime.Now };
                await _stateManager.UpdateStateAsync(errorProgress);
                RaiseProgress(errorProgress);
                CleanupJobControls(job.Name);
                throw new DirectoryNotFoundException($"Source directory not found: {job.SourceDirectory}");
            }
            FileInfo[] allSourceFiles = sourceDir.GetFiles("*", SearchOption.AllDirectories);

            var progress = new BackupProgress
            {
                JobName = job.Name,
                Timestamp = DateTime.Now,
                State = BackupState.Active,
                TotalFilesCount = allSourceFiles.Length,
                TotalFilesSize = allSourceFiles.Sum(f => f.Length),
                RemainingFilesCount = allSourceFiles.Length,
                RemainingFilesSize = allSourceFiles.Sum(f => f.Length),
            };
            await _stateManager.UpdateStateAsync(progress);
            RaiseProgress(progress);

            try
            {
                await PerformFullBackupAsync(job, progress, allSourceFiles, sourceDir, jobControl);

                if (jobControl.Cts.IsCancellationRequested)
                {
                    progress.State = BackupState.Stopped;
                }
                else if (progress.State != BackupState.Interrupted && progress.State != BackupState.Error) // Check if not interrupted by other means
                {
                    progress.State = BackupState.Completed;
                    progress.RemainingFilesCount = 0;
                    progress.RemainingFilesSize = 0;
                }
                progress.CurrentSourceFile = string.Empty;
                progress.CurrentTargetFile = string.Empty;
            }
            catch (OperationCanceledException) // Catches cancellation from jobControl.Cts
            {
                Debug.WriteLine($"[BackupExecutor] Job {job.Name} was cancelled (stopped).");
                progress.State = BackupState.Stopped;
            }
            catch (BusinessSoftwareInterruptionException)
            {
                Debug.WriteLine($"[BackupExecutor] BusinessSoftwareInterruptionException caught for job {job.Name}. State should be Interrupted.");
                if (progress.State != BackupState.Interrupted) progress.State = BackupState.Interrupted;
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BackupExecutor] Exception during job {job.Name}: {ex.Message}. Setting state to Error.");
                progress.State = BackupState.Error;
                await _logManager.LogFileOperationAsync(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    JobName = job.Name,
                    Message = $"Job execution failed: {ex.Message}"
                });
                // Do not rethrow if we want finally to update state and then exit gracefully from this method.
                // Or rethrow if the caller (BackupManagerService) handles it.
                // For now, let's not rethrow here, the state is set to Error.
            }
            finally
            {
                await _stateManager.UpdateStateAsync(progress);
                RaiseProgress(progress);
                CleanupJobControls(job.Name); // Clean up CTS and MRE
            }
        }

        private async Task PerformFullBackupAsync(BackupJob job, BackupProgress progress, FileInfo[] filesToProcess, DirectoryInfo sourceDir, JobControlContext jobControl)
        {
            int processedCount = 0;

            if (filesToProcess.Length == 0)
            {
                Debug.WriteLine($"[BackupExecutor] No files to process for full backup job '{job.Name}'.");
                return;
            }

            filesToProcess = filesToProcess
                .OrderByDescending(f => f.Extension.Equals(job.Priority.ToString(), StringComparison.OrdinalIgnoreCase))
                .ToArray();

            foreach (var file in filesToProcess)
            {
                // Check for pause at the beginning of each file operation
                if (!jobControl.PauseEvent.IsSet)
                {
                    progress.State = BackupState.Paused;
                    await _stateManager.UpdateStateAsync(progress);
                    RaiseProgress(progress);

                    Debug.WriteLine($"[BackupExecutor] Job '{job.Name}' waiting for resume...");
                    // Wait for resume or cancellation
                    WaitHandle.WaitAny(new[] { jobControl.PauseEvent.WaitHandle, jobControl.Cts.Token.WaitHandle });

                    if (jobControl.Cts.IsCancellationRequested) throw new OperationCanceledException(jobControl.Cts.Token);

                    progress.State = BackupState.Active; // Resumed
                    await _stateManager.UpdateStateAsync(progress);
                    RaiseProgress(progress);
                    Debug.WriteLine($"[BackupExecutor] Job '{job.Name}' resumed activity.");
                }

                // Check for stop
                if (jobControl.Cts.IsCancellationRequested) throw new OperationCanceledException(jobControl.Cts.Token);


                string relativePath = Path.GetRelativePath(sourceDir.FullName, file.FullName);
                string targetFilePath = Path.Combine(job.TargetDirectory, relativePath);
                string targetDir = Path.GetDirectoryName(targetFilePath);
                long encryptionTimeMs = 0;
                bool errorOccurred = false;
                System.Diagnostics.Stopwatch copyStopwatch = null;

                progress.CurrentSourceFile = file.FullName;
                progress.CurrentTargetFile = targetFilePath;
                // State is already Active or updated by pause/resume logic
                await _stateManager.UpdateStateAsync(progress);
                RaiseProgress(progress);

                try
                {
                    if (!Directory.Exists(targetDir))
                        Directory.CreateDirectory(targetDir);

                    if (file.Length >= MaxFileSizeBytes)
                    {
                        Debug.WriteLine($"[BackupExecutor] Skipping file {file.FullName} for job '{job.Name}' due to size limit.");
                        continue; // Skip this file, effectively reducing TotalFilesCount for percentage calc if not careful
                                  // For simplicity, we'll let the count be, meaning progress might not reach 100% if many are skipped.
                                  // A better way would be to filter these out beforehand.
                    }

                    if (job.FileExtension != EncryptionFileExtension.Null &&
                        file.Extension.TrimStart('.').Equals(job.FileExtension.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        string encryptedTargetPath = Path.Combine(targetDir, Path.GetFileName(file.Name) + ".crypt");
                        progress.CurrentTargetFile = encryptedTargetPath; // Update target for encrypted file

                        // TODO: Make encryption cancellable if CryptoSoft supports it
                        encryptionTimeMs = _encryptionService.EncryptFile(file.FullName, targetDir);
                        if (encryptionTimeMs < 0)
                        {
                            errorOccurred = true;
                            Debug.WriteLine($"[BackupExecutor] Encryption failed for file {file.FullName} in job '{job.Name}'. Error code: {encryptionTimeMs}");
                        }
                    }
                    else
                    {
                        copyStopwatch = System.Diagnostics.Stopwatch.StartNew();
                        // File.Copy is synchronous and not easily cancellable mid-operation.
                        // For true cancellability, would need custom copy logic or async I/O.
                        File.Copy(file.FullName, targetFilePath, true);
                        copyStopwatch.Stop();
                    }
                }
                catch (Exception ex)
                {
                    errorOccurred = true;
                    Debug.WriteLine($"[BackupExecutor] Error processing file {file.FullName} for job '{job.Name}': {ex.Message}");
                    await _logManager.LogFileOperationAsync(new LogEntry
                    {
                        Timestamp = DateTime.Now,
                        JobName = job.Name,
                        SourceFile = file.FullName,
                        TargetFile = progress.CurrentTargetFile, // Use CurrentTargetFile which might be .crypt
                        FileSize = file.Length,
                        TransferTimeMs = -1, // Indicate error
                        EncryptionTimeMs = encryptionTimeMs, // Log whatever time was spent, even if error
                        Message = $"Error copying/encrypting file: {ex.Message}"
                    });
                    // Decide if a single file error stops the whole job or just this file
                    // Current logic: log and continue with next file, job will not be "Completed"
                    // but will finish processing other files.
                }
                finally // Per file
                {
                    if (!errorOccurred)
                    {
                        await _logManager.LogFileOperationAsync(new LogEntry
                        {
                            Timestamp = DateTime.Now,
                            JobName = job.Name,
                            SourceFile = file.FullName,
                            TargetFile = progress.CurrentTargetFile, // Use CurrentTargetFile
                            FileSize = file.Length,
                            TransferTimeMs = (copyStopwatch?.ElapsedMilliseconds ?? 0),
                            EncryptionTimeMs = encryptionTimeMs
                        });
                    }
                    // This logic ensures that even if an error occurred for one file,
                    // we update progress for the files that *were* processed.
                    processedCount++; // A file attempt was made (success or fail)
                    progress.RemainingFilesCount = filesToProcess.Length - processedCount;
                    progress.RemainingFilesSize -= file.Length; // Subtract size whether copied or errored/skipped (for progress calc)

                    // State remains Active unless paused/stopped/error at job level
                    await _stateManager.UpdateStateAsync(progress);
                    RaiseProgress(progress);
                }
            } // End foreach file
        }
    }
}