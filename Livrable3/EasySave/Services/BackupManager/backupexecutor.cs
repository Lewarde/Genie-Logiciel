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
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading;

namespace EasySave.BackupExecutor
{
    // Manages backup job execution, including pausing, resuming, and stopping jobs.
    public class BackupExecutor : IBackupExecutor
    {
        private readonly LogManager _logManager;
        private readonly StateManager _stateManager;
        private readonly EncryptionService _encryptionService;
        private readonly string _businessSoftwarePath;
        private const long MaxFileSizeBytes = 100 * 1024 * 1024; // Max file size for backup.
        public event Action<BackupProgress> ProgressUpdated; // Event for progress updates.

        private readonly ConcurrentDictionary<string, JobControlContext> _activeJobControls;

        // Constructor initializes services and paths.
        public BackupExecutor(LogManager logManager, StateManager stateManager, EncryptionService encryptionService, string businessSoftwareFullPath)
        {
            _logManager = logManager ?? throw new ArgumentNullException(nameof(logManager));
            _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
            _businessSoftwarePath = businessSoftwareFullPath;
            _activeJobControls = new ConcurrentDictionary<string, JobControlContext>();
        }

        // Raises progress update event.
        private void RaiseProgress(BackupProgress progress)
        {
            ProgressUpdated?.Invoke(progress);
        }

        // Cleans up job control resources.
        private void CleanupJobControls(string jobName)
        {
            if (_activeJobControls.TryRemove(jobName, out var context))
            {
                context.Dispose();
            }
        }

        // Pauses a backup job.
        public async Task PauseJobAsync(string jobName)
        {
            if (_activeJobControls.TryGetValue(jobName, out var context))
            {
                context.PauseEvent.Reset();
                var progress = new BackupProgress { JobName = jobName, State = BackupState.Paused, Timestamp = DateTime.Now };
                await _stateManager.UpdateStateAsync(progress);
                RaiseProgress(progress);
                Debug.WriteLine($"[BackupExecutor] Job '{jobName}' PAUSED.");
            }
        }

        // Resumes a paused backup job.
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

        // Stops a backup job.
        public async Task StopJobAsync(string jobName)
        {
            if (_activeJobControls.TryGetValue(jobName, out var context))
            {
                context.Cts.Cancel();
                context.PauseEvent.Set();
                var progress = new BackupProgress { JobName = jobName, State = BackupState.Stopped, Timestamp = DateTime.Now };
                await _stateManager.UpdateStateAsync(progress);
                RaiseProgress(progress);
                Debug.WriteLine($"[BackupExecutor] Job '{jobName}' STOP signaled.");
            }
        }

        // Checks if business software is running and throws an exception if it is.
        private async Task CheckBusinessSoftwareAndThrowIfNeeded(string jobName, BackupProgress progressForStateUpdate)
        {
            // Check if the business software is running.
            if (!string.IsNullOrWhiteSpace(_businessSoftwarePath) && ProcessUtils.IsProcessRunning(_businessSoftwarePath))
            {
                string softwareDisplayName = Path.GetFileName(_businessSoftwarePath);
                string interruptionMessage = $"Backup job '{jobName}' cannot start. Business software '{softwareDisplayName}' (Path: {_businessSoftwarePath}) is running.";

                Debug.WriteLine($"[BackupExecutor] {interruptionMessage}");

                // Log the interruption.
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

        // Executes a backup job.
        public async Task ExecuteBackupJobAsync(BackupJob job)
        {
            // Initialize job control context.
            var jobControl = _activeJobControls.GetOrAdd(job.Name, new JobControlContext());
            jobControl.PauseEvent.Set();

            // Check if business software is running.
            await CheckBusinessSoftwareAndThrowIfNeeded(job.Name, null);

            // Check if source directory exists.
            DirectoryInfo sourceDir = new DirectoryInfo(job.SourceDirectory);
            if (!sourceDir.Exists)
            {
                var errorProgress = new BackupProgress { JobName = job.Name, State = BackupState.Error, Timestamp = DateTime.Now };
                await _stateManager.UpdateStateAsync(errorProgress);
                RaiseProgress(errorProgress);
                CleanupJobControls(job.Name);
                throw new DirectoryNotFoundException($"Source directory not found: {job.SourceDirectory}");
            }

            // Get all files from the source directory.
            FileInfo[] allSourceFiles = sourceDir.GetFiles("*", SearchOption.AllDirectories);

            // Initialize progress tracking.
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
                // Perform the backup process.
                await PerformFullBackupAsync(job, progress, allSourceFiles, sourceDir, jobControl);

                // Update progress state based on job completion status.
                if (jobControl.Cts.IsCancellationRequested)
                {
                    progress.State = BackupState.Stopped;
                }
                else if (progress.State != BackupState.Interrupted && progress.State != BackupState.Error)
                {
                    progress.State = BackupState.Completed;
                    progress.RemainingFilesCount = 0;
                    progress.RemainingFilesSize = 0;
                }
                progress.CurrentSourceFile = string.Empty;
                progress.CurrentTargetFile = string.Empty;
            }
            catch (OperationCanceledException)
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
            }
            finally
            {
                // Ensure progress is updated and cleanup job controls.
                await _stateManager.UpdateStateAsync(progress);
                RaiseProgress(progress);
                CleanupJobControls(job.Name);
            }
        }

        // Performs the full backup process.
        private async Task PerformFullBackupAsync(BackupJob job, BackupProgress progress, FileInfo[] filesToProcess, DirectoryInfo sourceDir, JobControlContext jobControl)
        {
            int processedCount = 0;

            // Return if there are no files to process.
            if (filesToProcess.Length == 0)
            {
                Debug.WriteLine($"[BackupExecutor] No files to process for full backup job '{job.Name}'.");
                return;
            }

            // Order files by priority extension.
            filesToProcess = filesToProcess
                .OrderByDescending(f => f.Extension.Equals(job.Priority.ToString(), StringComparison.OrdinalIgnoreCase))
                .ToArray();

            // Process each file.
            foreach (var file in filesToProcess)
            {
                // Handle pause event.
                if (!jobControl.PauseEvent.IsSet)
                {
                    progress.State = BackupState.Paused;
                    await _stateManager.UpdateStateAsync(progress);
                    RaiseProgress(progress);

                    Debug.WriteLine($"[BackupExecutor] Job '{job.Name}' waiting for resume...");
                    WaitHandle.WaitAny(new[] { jobControl.PauseEvent.WaitHandle, jobControl.Cts.Token.WaitHandle });

                    if (jobControl.Cts.IsCancellationRequested) throw new OperationCanceledException(jobControl.Cts.Token);

                    progress.State = BackupState.Active;
                    await _stateManager.UpdateStateAsync(progress);
                    RaiseProgress(progress);
                    Debug.WriteLine($"[BackupExecutor] Job '{job.Name}' resumed activity.");
                }

                // Check for cancellation request.
                if (jobControl.Cts.IsCancellationRequested) throw new OperationCanceledException(jobControl.Cts.Token);

                // Prepare file paths.
                string relativePath = Path.GetRelativePath(sourceDir.FullName, file.FullName);
                string targetFilePath = Path.Combine(job.TargetDirectory, relativePath);
                string targetDir = Path.GetDirectoryName(targetFilePath);
                long encryptionTimeMs = 0;
                bool errorOccurred = false;
                System.Diagnostics.Stopwatch copyStopwatch = null;

                // Update progress with current file info.
                progress.CurrentSourceFile = file.FullName;
                progress.CurrentTargetFile = targetFilePath;

                await _stateManager.UpdateStateAsync(progress);
                RaiseProgress(progress);

                try
                {
                    // Create target directory if it doesn't exist.
                    if (!Directory.Exists(targetDir))
                        Directory.CreateDirectory(targetDir);

                    // Skip large files.
                    if (file.Length >= MaxFileSizeBytes)
                    {
                        Debug.WriteLine($"[BackupExecutor] Skipping file {file.FullName} for job '{job.Name}' due to size limit.");
                        continue;
                    }

                    // Handle file encryption if needed.
                    if (job.FileExtension != EncryptionFileExtension.Null &&
                        file.Extension.TrimStart('.').Equals(job.FileExtension.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        string encryptedTargetPath = Path.Combine(targetDir, Path.GetFileName(file.Name) + ".crypt");
                        progress.CurrentTargetFile = encryptedTargetPath;
                        encryptionTimeMs = _encryptionService.EncryptFile(file.FullName, targetDir);
                        if (encryptionTimeMs < 0)
                        {
                            errorOccurred = true;
                            Debug.WriteLine($"[BackupExecutor] Encryption failed for file {file.FullName} in job '{job.Name}'. Error code: {encryptionTimeMs}");
                        }
                    }
                    else
                    {
                        // Copy file if no encryption is needed.
                        copyStopwatch = System.Diagnostics.Stopwatch.StartNew();
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
                        TargetFile = progress.CurrentTargetFile,
                        FileSize = file.Length,
                        TransferTimeMs = -1,
                        EncryptionTimeMs = encryptionTimeMs,
                        Message = $"Error copying/encrypting file: {ex.Message}"
                    });
                }
                finally
                {
                    // Log successful file operations.
                    if (!errorOccurred)
                    {
                        await _logManager.LogFileOperationAsync(new LogEntry
                        {
                            Timestamp = DateTime.Now,
                            JobName = job.Name,
                            SourceFile = file.FullName,
                            TargetFile = progress.CurrentTargetFile,
                            FileSize = file.Length,
                            TransferTimeMs = (copyStopwatch?.ElapsedMilliseconds ?? 0),
                            EncryptionTimeMs = encryptionTimeMs
                        });
                    }

                    // Update progress counters.
                    processedCount++;
                    progress.RemainingFilesCount = filesToProcess.Length - processedCount;
                    progress.RemainingFilesSize -= file.Length;

                    await _stateManager.UpdateStateAsync(progress);
                    RaiseProgress(progress);
                }
            }
        }
    }
}
