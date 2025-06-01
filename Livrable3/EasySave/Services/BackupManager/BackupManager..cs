using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EasySave.Models;
using EasySave.Services;
using EasySave.Utils;
using Logger;
using EasySave.BackupExecutor;
using EasySave.Services.CryptoSoft;
using EasySave.Config;

namespace EasySave.BackupManager
{
    // Manages backup jobs, including execution, pausing, resuming, and stopping.
    public class BackupManager : EasySave.Services.IBackupManager
    {
        private readonly List<BackupJob> _backupJobs;
        private readonly StateManager _stateManager;
        private readonly LogManager _logManager;
        private readonly EasySave.BackupExecutor.BackupExecutor _backupExecutorService;
        private readonly EncryptionService _encryptionService;
        private readonly AppSettingsData _appSettings;

        // Tracks running jobs for management purposes.
        private readonly ConcurrentDictionary<string, Task> _runningJobTasks;

        // Constructor initializes services and settings.
        public BackupManager(AppSettingsData appSettings)
        {
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _backupJobs = new List<BackupJob>();
            _stateManager = new StateManager();

            if (LogManager.Instance == null)
                throw new InvalidOperationException("LogManager not initialized. Call LogManager.Initialize() first.");
            _logManager = LogManager.Instance;

            _encryptionService = new EncryptionService();

            // Initialize BackupExecutor service.
            _backupExecutorService = new EasySave.BackupExecutor.BackupExecutor(
                _logManager,
                _stateManager,
                _encryptionService,
                _appSettings.BusinessSoftwareProcessName
            );

            _runningJobTasks = new ConcurrentDictionary<string, Task>();
        }

        // Returns all backup jobs.
        public List<BackupJob> GetAllJobs() => _backupJobs;

        // Adds a new backup job if it doesn't already exist.
        public void AddBackupJob(BackupJob job)
        {
            if (!_backupJobs.Any(bj => bj.Name.Equals(job.Name, StringComparison.OrdinalIgnoreCase)))
            {
                _backupJobs.Add(job);
            }
        }

        // Removes a backup job by index.
        public void RemoveBackupJob(int index)
        {
            if (index >= 0 && index < _backupJobs.Count)
                _backupJobs.RemoveAt(index);
        }

        // Removes a backup job by job object.
        public void RemoveBackupJob(BackupJob job)
        {
            var jobToRemove = _backupJobs.FirstOrDefault(bj => bj.Name.Equals(job.Name, StringComparison.OrdinalIgnoreCase));
            if (jobToRemove != null)
                _backupJobs.Remove(jobToRemove);
        }

        // Executes a backup job asynchronously.
        public async Task ExecuteBackupJobAsync(BackupJob job)
        {
            try
            {
                // Check if source directory exists.
                if (!Directory.Exists(job.SourceDirectory))
                    throw new DirectoryNotFoundException($"{LanguageManager.GetString("SourceDirNotFound")}: {job.SourceDirectory}");

                // Create target directory if it doesn't exist.
                if (!Directory.Exists(job.TargetDirectory))
                    Directory.CreateDirectory(job.TargetDirectory);

                // Execute the backup job.
                await _backupExecutorService.ExecuteBackupJobAsync(job);
            }
            catch (BusinessSoftwareInterruptionException)
            {
                // Exception is logged and state updated by BackupExecutor.
                throw;
            }
            catch (Exception ex)
            {
                // Handle generic errors.
                var errorProgress = new BackupProgress
                {
                    JobName = job.Name,
                    State = BackupState.Error,
                    Timestamp = DateTime.Now
                };
                await _stateManager.UpdateStateAsync(errorProgress);

                await _logManager.LogFileOperationAsync(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    JobName = job.Name,
                    Message = $"Backup job '{job.Name}' failed at BackupManager level: {ex.Message}"
                });
                throw;
            }
        }

        // Pauses a running backup job.
        public async Task PauseJobAsync(string jobName)
        {
            await _backupExecutorService.PauseJobAsync(jobName);
        }

        // Resumes a paused backup job.
        public async Task ResumeJobAsync(string jobName)
        {
            await _backupExecutorService.ResumeJobAsync(jobName);
        }

        // Stops a running backup job.
        public async Task StopJobAsync(string jobName)
        {
            await _backupExecutorService.StopJobAsync(jobName);
        }

        // Starts all specified backup jobs in parallel.
        public void StartAllJobsInParallel(List<BackupJob> jobsToRun)
        {
            if (jobsToRun == null) return;

            foreach (var job in jobsToRun)
            {
                if (job == null || string.IsNullOrEmpty(job.Name)) continue;

                // Skip if job is already running or queued.
                if (_runningJobTasks.TryGetValue(job.Name, out var existingTask) && !existingTask.IsCompleted)
                {
                    System.Diagnostics.Debug.WriteLine($"[BackupManager] Job '{job.Name}' is already running or queued. Skipping.");
                    continue;
                }

                // Queue job for parallel execution.
                var task = Task.Run(async () => await ExecuteBackupJobAsync(job));
                _runningJobTasks.AddOrUpdate(job.Name, task, (key, oldTask) => task);
                System.Diagnostics.Debug.WriteLine($"[BackupManager] Queued job '{job.Name}' for parallel execution.");
            }
        }

        // Waits for all running jobs to complete.
        public async Task WaitForAllJobsAsync()
        {
            await Task.WhenAll(_runningJobTasks.Values.ToList());
        }

        // Checks if a job is currently running.
        public bool IsJobRunning(string jobName)
        {
            return _runningJobTasks.TryGetValue(jobName, out var task) && !task.IsCompleted;
        }

        // Clears finished jobs from the running jobs dictionary.
        public void ClearFinishedJobs()
        {
            foreach (var kvp in _runningJobTasks.ToList())
            {
                if (kvp.Value.IsCompleted)
                {
                    _runningJobTasks.TryRemove(kvp.Key, out _);
                }
            }
        }

        // Returns the BackupExecutor instance.
        public EasySave.BackupExecutor.BackupExecutor GetBackupExecutor()
        {
            return _backupExecutorService;
        }
    }
}


