using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EasySave.Models;
using EasySave.Services;
using EasySave.Utils;
using Logger;

namespace EasySave.BackupManager
{
    public class BackupManager : IBackupManager // Ensure this matches the class declaration
    {
        private readonly List<BackupJob> _backupJobs;
        private readonly StateManager _stateManager;
        private readonly LogManager _logManager;
        private readonly BackupExecutor.BackupExecutor _backupExecutor;

        public BackupManager()
        {
            _backupJobs = new List<BackupJob>();
            _stateManager = new StateManager();
            _logManager = LogManager.Instance; // 🔥 correction ici
            _backupExecutor = new BackupExecutor.BackupExecutor(_logManager, _stateManager);
        }


        public List<BackupJob> GetAllJobs() => _backupJobs;

        public void AddBackupJob(BackupJob job) => _backupJobs.Add(job);

        public void RemoveBackupJob(int index)
        {
            if (index >= 0 && index < _backupJobs.Count)
            {
                _backupJobs.RemoveAt(index);
            }
        }

        public async Task ExecuteBackupJobAsync(BackupJob job)
        {
            Console.WriteLine(LanguageManager.GetString("ExecutingJob") + job.Name);

            try
            {
                if (!Directory.Exists(job.SourceDirectory))
                {
                    Console.WriteLine(LanguageManager.GetString("SourceDirNotFound"));
                    return;
                }

                if (!Directory.Exists(job.TargetDirectory))
                    Directory.CreateDirectory(job.TargetDirectory);

                var progress = new BackupProgress
                {
                    JobName = job.Name,
                    State = BackupState.Active,
                    Timestamp = DateTime.Now
                };

                var sourceDir = new DirectoryInfo(job.SourceDirectory);
                var files = sourceDir.GetFiles("*", SearchOption.AllDirectories);

                await _stateManager.UpdateStateAsync(progress);

                await _backupExecutor.ExecuteBackupJobAsync(job);


                progress.State = BackupState.Completed;
                progress.RemainingFilesCount = 0;
                progress.RemainingFilesSize = 0;
                await _stateManager.UpdateStateAsync(progress);

                Console.WriteLine(LanguageManager.GetString("BackupCompleted"));
            }
            catch (Exception ex)
            {
                Console.WriteLine(LanguageManager.GetString("BackupError") + ex.Message);

                var errorProgress = new BackupProgress
                {
                    JobName = job.Name,
                    State = BackupState.Error,
                    Timestamp = DateTime.Now
                };

                await _stateManager.UpdateStateAsync(errorProgress);
            }
        }


    }
}

