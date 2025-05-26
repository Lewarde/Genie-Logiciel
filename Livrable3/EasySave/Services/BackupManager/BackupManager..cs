using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; // AJOUT DE CETTE DIRECTIVE USING
using System.Threading.Tasks;
using EasySave.Models;
using EasySave.Services;
using EasySave.Utils;
using Logger;
using EasySave.BackupExecutor;
using EasySave.Services.CryptoSoft;
using EasySave.Config; // Required for AppSettingsData

namespace EasySave.BackupManager
{

    public class BackupManager : EasySave.Services.IBackupManager
    {
        private readonly List<BackupJob> _backupJobs;
        private readonly StateManager _stateManager;
        private readonly LogManager _logManager;
        private readonly EasySave.BackupExecutor.BackupExecutor _backupExecutorService;
        private readonly EncryptionService _encryptionService;
        private readonly AppSettingsData _appSettings; // Store app settings

        // Modified constructor to accept AppSettingsData
        public BackupManager(AppSettingsData appSettings)
        {
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _backupJobs = new List<BackupJob>();
            _stateManager = new StateManager();
            if (LogManager.Instance == null)
                throw new InvalidOperationException("LogManager not initialized. Call LogManager.Initialize() first.");
            _logManager = LogManager.Instance;

            _encryptionService = new EncryptionService();
            // Pass the business software name to BackupExecutor
            _backupExecutorService = new EasySave.BackupExecutor.BackupExecutor(
                _logManager,
                _stateManager,
                _encryptionService,
                _appSettings.BusinessSoftwareProcessName // Pass it here
            );
        }


        public EasySave.BackupExecutor.BackupExecutor GetBackupExecutor()
        {
            return _backupExecutorService;
        }

        public List<BackupJob> GetAllJobs() => _backupJobs;

        public void AddBackupJob(BackupJob job)
        {
            if (!_backupJobs.Any(bj => bj.Name.Equals(job.Name, StringComparison.OrdinalIgnoreCase))) // Prevent duplicates by name
            {
                _backupJobs.Add(job);
            }
        }

        public void RemoveBackupJob(int index)
        {
            if (index >= 0 && index < _backupJobs.Count)
            {
                _backupJobs.RemoveAt(index);
            }
        }

        public void RemoveBackupJob(BackupJob job)
        {
            var jobToRemove = _backupJobs.FirstOrDefault(bj => bj.Name.Equals(job.Name, StringComparison.OrdinalIgnoreCase));
            if (jobToRemove != null)
            {
                _backupJobs.Remove(jobToRemove);
            }
        }


        public async Task ExecuteBackupJobAsync(BackupJob job)
        {
            // Note: The pre-execution check for business software is now primarily in MainViewModel
            // and an initial check within BackupExecutor.ExecuteBackupJobAsync.
            // This method focuses on the execution itself.
            try
            {
                if (!Directory.Exists(job.SourceDirectory))
                {
                    throw new DirectoryNotFoundException($"{LanguageManager.GetString("SourceDirNotFound")}: {job.SourceDirectory}");
                }

                if (!Directory.Exists(job.TargetDirectory))
                {
                    Directory.CreateDirectory(job.TargetDirectory);
                }

                await _backupExecutorService.ExecuteBackupJobAsync(job);
            }
            catch (BusinessSoftwareInterruptionException)
            {
                // Logged by BackupExecutor. Rethrow for MainViewModel to update UI.
                throw;
            }
            catch (Exception ex) // Catch other execution errors
            {
                var errorProgress = new BackupProgress
                {
                    JobName = job.Name,
                    State = BackupState.Error,
                    Timestamp = DateTime.Now
                };
                await _stateManager.UpdateStateAsync(errorProgress);
                // Log the error explicitly if not already logged by a lower layer for this specific context
                await _logManager.LogFileOperationAsync(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    JobName = job.Name,
                    Message = $"Backup job '{job.Name}' failed: {ex.Message}"
                });
                throw; // Re-throw for ViewModel to catch and display
            }
        }
    }
}