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

namespace EasySave.BackupExecutor
{
    public class BackupExecutor : IBackupExecutor
    {
        private readonly LogManager _logManager;
        private readonly StateManager _stateManager;
        private readonly EncryptionService _encryptionService;
        private readonly string _businessSoftwarePath;

        public event Action<BackupProgress> ProgressUpdated;

        public BackupExecutor(LogManager logManager, StateManager stateManager, EncryptionService encryptionService, string businessSoftwareFullPath)
        {
            _logManager = logManager ?? throw new ArgumentNullException(nameof(logManager));
            _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
            _businessSoftwarePath = businessSoftwareFullPath;
        }

        private void RaiseProgress(BackupProgress progress)
        {
            ProgressUpdated?.Invoke(progress);
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
            await CheckBusinessSoftwareAndThrowIfNeeded(job.Name, null);

            DirectoryInfo sourceDir = new DirectoryInfo(job.SourceDirectory);
            if (!sourceDir.Exists)
            {
                var errorProgress = new BackupProgress { JobName = job.Name, State = BackupState.Error, Timestamp = DateTime.Now };
                await _stateManager.UpdateStateAsync(errorProgress);
                RaiseProgress(errorProgress);
                throw new DirectoryNotFoundException($"Source directory not found: {job.SourceDirectory}");
            }
            FileInfo[] allSourceFiles = sourceDir.GetFiles("*", SearchOption.AllDirectories);

            var progress = new BackupProgress
            {
                JobName = job.Name,
                Timestamp = DateTime.Now,
                State = BackupState.Active,
                TotalFilesCount = allSourceFiles.Length, // For full backup, all files are initially considered
                TotalFilesSize = allSourceFiles.Sum(f => f.Length),
                RemainingFilesCount = allSourceFiles.Length,
                RemainingFilesSize = allSourceFiles.Sum(f => f.Length),
            };
            await _stateManager.UpdateStateAsync(progress);
            RaiseProgress(progress);

            try
            {
                 await PerformFullBackupAsync(job, progress, allSourceFiles, sourceDir);
                


                if (progress.State != BackupState.Interrupted) // Check if not interrupted by other means (though unlikely here)
                {
                    // Only mark as completed if no errors occurred that set state to Error
                    if (progress.State != BackupState.Error)
                    {
                        progress.State = BackupState.Completed;
                    }
                    // For completed jobs (even with some file errors), remaining should be 0
                    // If a critical error occurred, State would be Error and this might not be accurate,
                    // but the overall job state takes precedence.
                    progress.RemainingFilesCount = 0;
                    progress.RemainingFilesSize = 0;
                    progress.CurrentSourceFile = string.Empty;
                    progress.CurrentTargetFile = string.Empty;
                }
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
                // Log the main job error
                await _logManager.LogFileOperationAsync(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    JobName = job.Name,
                    Message = $"Job execution failed: {ex.Message}"
                });
                throw;
            }
            finally
            {
                await _stateManager.UpdateStateAsync(progress);
                RaiseProgress(progress);
            }
        }

        private async Task PerformFullBackupAsync(BackupJob job, BackupProgress progress, FileInfo[] filesToProcess, DirectoryInfo sourceDir)
        {
            int processedCount = 0;
            // For Full backup, TotalFilesCount and TotalFilesSize in 'progress' are already set correctly
            // based on all files in sourceDir from ExecuteBackupJobAsync.

            if (filesToProcess.Length == 0)
            {
                Debug.WriteLine($"[BackupExecutor] No files to process for full backup job '{job.Name}'.");
                // Progress state will be handled by ExecuteBackupJobAsync (likely becomes Completed if no files)
                return;
            }

            foreach (var file in filesToProcess)
            {
                // NO BUSINESS SOFTWARE CHECK HERE (done at the start of ExecuteBackupJobAsync)
                string relativePath = Path.GetRelativePath(sourceDir.FullName, file.FullName);
                string targetFilePath = Path.Combine(job.TargetDirectory, relativePath);
                string targetDir = Path.GetDirectoryName(targetFilePath);
                long encryptionTimeMs = 0;
                bool errorOccurred = false;
                System.Diagnostics.Stopwatch copyStopwatch = null;

                progress.CurrentSourceFile = file.FullName;
                progress.CurrentTargetFile = targetFilePath; // Default target, may change if encrypted
                await _stateManager.UpdateStateAsync(progress);
                RaiseProgress(progress);

                try
                {
                    if (!Directory.Exists(targetDir))
                        Directory.CreateDirectory(targetDir);

                    if (job.FileExtension != EncryptionFileExtension.Null &&
                        file.Extension.TrimStart('.').Equals(job.FileExtension.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        string encryptedTargetPath = Path.Combine(targetDir, Path.GetFileName(file.Name) + ".crypt");
                        progress.CurrentTargetFile = encryptedTargetPath; // Update for logging if encrypted

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
                        File.Copy(file.FullName, targetFilePath, true);
                        copyStopwatch.Stop();
                    }
                }
                catch (Exception ex)
                {
                    errorOccurred = true;
                    // If copyStopwatch was running, its time is irrelevant due to error.
                    // encryptionTimeMs can be set to a generic error code if it wasn't an encryption attempt that failed.
                    if (encryptionTimeMs >= 0) encryptionTimeMs = -2; // Generic file operation error
                    Debug.WriteLine($"[BackupExecutor] Error processing file {file.FullName} for job '{job.Name}': {ex.Message}");
                    // Log individual file error
                    await _logManager.LogFileOperationAsync(new LogEntry
                    {
                        Timestamp = DateTime.Now,
                        JobName = job.Name,
                        SourceFile = file.FullName,
                        TargetFile = progress.CurrentTargetFile, // Use the target file path from progress
                        FileSize = file.Length,
                        TransferTimeMs = -1, // Indicate error
                        EncryptionTimeMs = encryptionTimeMs,
                        Message = $"Error copying/encrypting file: {ex.Message}"
                    });
                }
                finally // This finally is for each file
                {
                    if (!errorOccurred) // Log successful operation only if no error occurred during try block
                    {
                        await _logManager.LogFileOperationAsync(new LogEntry
                        {
                            Timestamp = DateTime.Now,
                            JobName = job.Name,
                            SourceFile = file.FullName,
                            TargetFile = progress.CurrentTargetFile,
                            FileSize = file.Length,
                            TransferTimeMs = (copyStopwatch?.ElapsedMilliseconds ?? 0), // 0 if encrypted or error
                            EncryptionTimeMs = encryptionTimeMs
                        });
                    }
                    // else: error was already logged in the catch block for the file

                    if (!errorOccurred)
                    {
                        processedCount++;
                        // RemainingFilesCount and RemainingFilesSize are decremented from the totals.
                        progress.RemainingFilesCount = filesToProcess.Length - processedCount;
                        progress.RemainingFilesSize -= file.Length;
                    }
                    else
                    {
                        // If an error occurred for this file, we might not want to count it as "processed"
                        // in terms of successful completion. The overall job state might become "Error".
                        // We still decrement RemainingFilesCount to show progress through the list of files to attempt.
                        // However, if we want to reflect that the job didn't fully complete all its intended files,
                        // we might adjust this logic or rely on the final job State.
                        // For now, let's assume RemainingFilesCount always ticks down as we attempt each file.
                        progress.RemainingFilesCount = filesToProcess.Length - (processedCount + (errorOccurred ? 1 : 0));
                        // Don't subtract size if it errored, as it wasn't successfully backed up.
                        // This means progress.RemainingFilesSize might not reach 0 if errors occur.
                        // This behavior needs to be consistent with how "Completed" vs "Error" state is determined.
                        // Let's stick to: if a file errors, it's not "done" in terms of backup.
                        // So, we only update processedCount and sizes for successful files.
                        // The overall job will be marked Error if any file fails.
                        // The progress bar will still advance based on files attempted.
                        // To keep progress bar somewhat consistent with files ATTEMPTED:
                        // progress.RemainingFilesCount = filesToProcess.Length - (loop_iterator_index + 1);
                        // For simplicity, let's only decrement for successful files.
                        // The overall job state (Completed/Error) will be the main indicator.

                        // If a file errors, the job's overall state should become "Error".
                        // We can set it here, or let it bubble up and be set in ExecuteBackupJobAsync's catch.
                        // For now, just log and continue; the main catch in ExecuteBackupJobAsync will handle overall state.
                        progress.State = BackupState.Active; // Keep it active, but it might become Error later.
                                                             // Or, if we want to indicate partial success with errors:
                                                             // if (progress.State != BackupState.Error) progress.State = BackupState.ActiveWithErrors; (custom state)
                    }

                    await _stateManager.UpdateStateAsync(progress);
                    RaiseProgress(progress);
                }
            }
            // The overall job state (Completed/Error) is determined in ExecuteBackupJobAsync
            // after this loop finishes or if an unhandled exception occurs.
        }

        private async Task PerformDifferentialBackupAsync(BackupJob job, BackupProgress progress, FileInfo[] allSourceFiles, DirectoryInfo sourceDir)
        {
            int processedCount = 0;
            var filesToProcessList = new List<FileInfo>();
            long totalSizeOfFilesToProcess = 0;

            foreach (var file in allSourceFiles)
            {
                string relativePath = Path.GetRelativePath(sourceDir.FullName, file.FullName);
                string targetFilePath = Path.Combine(job.TargetDirectory, relativePath);
                string encryptedTargetFilePath = Path.Combine(job.TargetDirectory, relativePath + ".crypt");
                bool shouldCopy = false;

                if (job.FileExtension != EncryptionFileExtension.Null &&
                    file.Extension.TrimStart('.').Equals(job.FileExtension.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    if (!File.Exists(encryptedTargetFilePath) || File.GetLastWriteTimeUtc(file.FullName) > File.GetLastWriteTimeUtc(encryptedTargetFilePath))
                    {
                        shouldCopy = true;
                    }
                }
                else
                {
                    bool targetExists = File.Exists(targetFilePath);
                    if (!targetExists ||
                        File.GetLastWriteTimeUtc(file.FullName) > File.GetLastWriteTimeUtc(targetFilePath) ||
                        file.Length != (targetExists ? new FileInfo(targetFilePath).Length : -1))
                    {
                        shouldCopy = true;
                    }
                }

                if (shouldCopy)
                {
                    filesToProcessList.Add(file);
                    totalSizeOfFilesToProcess += file.Length;
                }
            }

            // Update progress totals based on actual files to be copied in differential mode
            progress.TotalFilesCount = filesToProcessList.Count;
            progress.TotalFilesSize = totalSizeOfFilesToProcess;
            progress.RemainingFilesCount = filesToProcessList.Count;
            progress.RemainingFilesSize = totalSizeOfFilesToProcess;
            await _stateManager.UpdateStateAsync(progress); // Update state with new totals
            RaiseProgress(progress); // Raise progress with new totals

            if (filesToProcessList.Count == 0)
            {
                Debug.WriteLine($"[BackupExecutor] No files to copy for differential backup job '{job.Name}'.");
                // The job will be marked as Completed by ExecuteBackupJobAsync if this is the case
                return;
            }

            foreach (var file in filesToProcessList)
            {
                string relativePath = Path.GetRelativePath(sourceDir.FullName, file.FullName);
                string targetFilePathInLoop = Path.Combine(job.TargetDirectory, relativePath);
                string targetDir = Path.GetDirectoryName(targetFilePathInLoop);
                long encryptionTimeMs = 0;
                bool errorOccurred = false;
                System.Diagnostics.Stopwatch copyStopwatch = null;

                progress.CurrentSourceFile = file.FullName;
                progress.CurrentTargetFile = targetFilePathInLoop;
                await _stateManager.UpdateStateAsync(progress);
                RaiseProgress(progress);

                try
                {
                    if (!Directory.Exists(targetDir))
                        Directory.CreateDirectory(targetDir);

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
                        copyStopwatch = System.Diagnostics.Stopwatch.StartNew();
                        File.Copy(file.FullName, targetFilePathInLoop, true);
                        copyStopwatch.Stop();
                    }
                }
                catch (Exception ex)
                {
                    errorOccurred = true;
                    if (encryptionTimeMs >= 0) encryptionTimeMs = -2;
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

                    if (!errorOccurred)
                    {
                        processedCount++;
                        progress.RemainingFilesCount = filesToProcessList.Count - processedCount;
                        progress.RemainingFilesSize -= file.Length;
                    }

                    await _stateManager.UpdateStateAsync(progress);
                    RaiseProgress(progress);
                }
            }
        }
    }
}