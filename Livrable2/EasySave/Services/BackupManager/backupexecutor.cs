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
        private readonly string _businessSoftwarePath; // Changed name for clarity

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

        // Helper method to check for business software.
        // This will now only be called once at the start of ExecuteBackupJobAsync.
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
                    progressForStateUpdate.State = BackupState.Interrupted; // Or a new state like "BlockedByBusinessSoftware"
                    await _stateManager.UpdateStateAsync(progressForStateUpdate);
                    RaiseProgress(progressForStateUpdate);
                }
                throw new BusinessSoftwareInterruptionException(interruptionMessage);
            }
        }

        public async Task ExecuteBackupJobAsync(BackupJob job)
        {
            // --- SINGLE CHECK AT THE START OF THE JOB ---
            // We pass 'null' for progressForStateUpdate initially as progress object isn't fully formed yet.
            // If an interruption occurs here, the job effectively doesn't start.
            await CheckBusinessSoftwareAndThrowIfNeeded(job.Name, null);
            // --- END OF SINGLE CHECK ---

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
                TotalFilesCount = allSourceFiles.Length,
                TotalFilesSize = allSourceFiles.Sum(f => f.Length),
                RemainingFilesCount = allSourceFiles.Length,
                RemainingFilesSize = allSourceFiles.Sum(f => f.Length),
            };
            await _stateManager.UpdateStateAsync(progress);
            RaiseProgress(progress);

            try
            {
                if (job.Type == BackupType.Full)
                    await PerformFullBackupAsync(job, progress, allSourceFiles, sourceDir);
                else
                    await PerformDifferentialBackupAsync(job, progress, allSourceFiles, sourceDir);

                if (progress.State != BackupState.Interrupted)
                {
                    progress.State = BackupState.Completed;
                    progress.RemainingFilesCount = 0;
                    progress.RemainingFilesSize = 0;
                    progress.CurrentSourceFile = string.Empty;
                    progress.CurrentTargetFile = string.Empty;
                }
            }
            catch (BusinessSoftwareInterruptionException) // Should not happen if check is only at start and throws
            {
                // This catch block might become redundant here if the check is ONLY at the very start
                // and throws before this try block is deeply entered.
                // However, keeping it for safety in case of future refactoring.
                Debug.WriteLine($"[BackupExecutor] BusinessSoftwareInterruptionException caught during job {job.Name}. State should be Interrupted.");
                if (progress.State != BackupState.Interrupted) progress.State = BackupState.Interrupted; // Ensure state is set
                throw;
            }
            catch (Exception)
            {
                progress.State = BackupState.Error;
                throw;
            }
            finally
            {
                await _stateManager.UpdateStateAsync(progress);
                RaiseProgress(progress);
            }
        }

        // In PerformFullBackupAsync and PerformDifferentialBackupAsync,
        // REMOVE any calls to CheckBusinessSoftwareAndThrowIfNeeded or similar.
        // The check is now done only once in ExecuteBackupJobAsync.

        private async Task PerformFullBackupAsync(BackupJob job, BackupProgress progress, FileInfo[] filesToProcess, DirectoryInfo sourceDir)
        {
            int processedCount = 0;
            progress.TotalFilesCount = filesToProcess.Length;
            progress.TotalFilesSize = filesToProcess.Sum(f => f.Length);
            progress.RemainingFilesCount = filesToProcess.Length;
            progress.RemainingFilesSize = progress.TotalFilesSize;
            // No need to update state here again if it was just done in ExecuteBackupJobAsync
            // RaiseProgress(progress); // Already raised

            if (filesToProcess.Length == 0) return;

            foreach (var file in filesToProcess)
            {
                // NO BUSINESS SOFTWARE CHECK HERE
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
                        progress.RemainingFilesCount = filesToProcess.Length - (processedCount + (errorOccurred ? 1 : 0));
                        progress.State = BackupState.Active;

                        await _stateManager.UpdateStateAsync(progress);
                        RaiseProgress(progress);
                    }
                }
            }
        }

        private async Task PerformDifferentialBackupAsync(BackupJob job, BackupProgress progress, FileInfo[] allSourceFiles, DirectoryInfo sourceDir)
        {
            int processedCount = 0;
            var filesToProcessList = new List<FileInfo>();
            long totalSizeOfFilesToProcess = 0;

            // Déterminer les fichiers à traiter (nouveaux ou modifiés)
            foreach (var file in allSourceFiles)
            {
                string relativePath = Path.GetRelativePath(sourceDir.FullName, file.FullName);
                string targetFilePath = Path.Combine(job.TargetDirectory, relativePath);
                string encryptedTargetFilePath = Path.Combine(job.TargetDirectory, relativePath + ".crypt");

                bool shouldCopy = false; // DÉCLARATION DE SHOULDCOPY ICI

                if (job.FileExtension != EncryptionFileExtension.Null &&
                    file.Extension.TrimStart('.').Equals(job.FileExtension.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    // Fichier à chiffrer : vérifier l'existence de la version chiffrée
                    if (!File.Exists(encryptedTargetFilePath) || File.GetLastWriteTimeUtc(file.FullName) > File.GetLastWriteTimeUtc(encryptedTargetFilePath))
                    {
                        shouldCopy = true;
                    }
                }
                else
                {
                    // Fichier normal : vérifier la version non chiffrée
                    bool targetExists = File.Exists(targetFilePath);
                    if (!targetExists ||
                        File.GetLastWriteTimeUtc(file.FullName) > File.GetLastWriteTimeUtc(targetFilePath) ||
                        file.Length != (targetExists ? new FileInfo(targetFilePath).Length : -1)) // -1 si n'existe pas pour la comparaison de taille
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

            progress.TotalFilesCount = filesToProcessList.Count;
            progress.TotalFilesSize = totalSizeOfFilesToProcess;
            progress.RemainingFilesCount = filesToProcessList.Count;
            progress.RemainingFilesSize = totalSizeOfFilesToProcess;
            // Pas besoin de mettre à jour l'état ici à nouveau si cela a été fait dans ExecuteBackupJobAsync
            // RaiseProgress(progress); // Déjà levé

            if (filesToProcessList.Count == 0)
            {
                // Optionnel : logguer ou mettre à jour le statut si aucun fichier n'est à copier en différentiel
                Debug.WriteLine($"[BackupExecutor] No files to copy for differential backup job '{job.Name}'.");
                return;
            }

            // Traiter les fichiers sélectionnés
            foreach (var file in filesToProcessList)
            {
                // PAS DE VÉRIFICATION DU LOGICIEL MÉTIER ICI
                string relativePath = Path.GetRelativePath(sourceDir.FullName, file.FullName);
                string targetFilePathInLoop = Path.Combine(job.TargetDirectory, relativePath); // Renommé pour éviter confusion avec variable extérieure à la boucle
                string targetDir = Path.GetDirectoryName(targetFilePathInLoop);
                long encryptionTimeMs = 0;
                bool errorOccurred = false;
                System.Diagnostics.Stopwatch copyStopwatch = null;

                progress.CurrentSourceFile = file.FullName;
                progress.CurrentTargetFile = targetFilePathInLoop; // Cible par défaut
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
                        progress.CurrentTargetFile = encryptedTargetPath; // Mettre à jour pour le log

                        encryptionTimeMs = _encryptionService.EncryptFile(file.FullName, targetDir);
                        if (encryptionTimeMs < 0) errorOccurred = true;
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
                    encryptionTimeMs = -2; // Erreur de copie/opération
                    Debug.WriteLine($"[BackupExecutor] Error processing file {file.FullName} for job '{job.Name}': {ex.Message}");
                    // Gérer l'erreur : logger et potentiellement marquer le travail comme ayant des erreurs
                }
                finally
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

                    if (!errorOccurred)
                    {
                        processedCount++;
                        progress.RemainingFilesCount = filesToProcessList.Count - processedCount;
                        progress.RemainingFilesSize -= file.Length;
                    }
                    // else : si une erreur, RemainingFilesCount/Size ne sont pas décrémentés pour ce fichier.

                    await _stateManager.UpdateStateAsync(progress);
                    RaiseProgress(progress);
                    // Pas de throw ici pour permettre aux autres fichiers d'être traités.
                    // L'état global du job (Error/Completed) sera déterminé à la fin de ExecuteBackupJobAsync.
                }
            }
        }
    }
    }