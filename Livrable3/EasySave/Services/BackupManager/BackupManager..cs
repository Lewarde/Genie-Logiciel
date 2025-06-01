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
using EasySave.BackupExecutor; // Correct namespace for BackupExecutor
using EasySave.Services.CryptoSoft;
using EasySave.Config;

namespace EasySave.BackupManager // Corrected namespace
{
    public class BackupManager : EasySave.Services.IBackupManager // Implements an interface, likely from a different namespace
    {
        private readonly List<BackupJob> _backupJobs;
        private readonly StateManager _stateManager;
        private readonly LogManager _logManager;
        private readonly EasySave.BackupExecutor.BackupExecutor _backupExecutorService;
        private readonly EncryptionService _encryptionService;
        private readonly AppSettingsData _appSettings;

        // Keep track of running jobs to manage them (e.g., for parallel execution, stopping all, etc.)
        private readonly ConcurrentDictionary<string, Task> _runningJobTasks;


        public BackupManager(AppSettingsData appSettings)
        {
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _backupJobs = new List<BackupJob>();
            _stateManager = new StateManager();

            if (LogManager.Instance == null) // Ensure LogManager is initialized
                throw new InvalidOperationException("LogManager not initialized. Call LogManager.Initialize() first.");
            _logManager = LogManager.Instance;

            _encryptionService = new EncryptionService(); // Assuming this is correctly initialized

            // The BackupExecutor service that handles the actual backup logic
            _backupExecutorService = new EasySave.BackupExecutor.BackupExecutor(
                _logManager,
                _stateManager,
                _encryptionService,
                _appSettings.BusinessSoftwareProcessName // Pass the business software path
            );

            _runningJobTasks = new ConcurrentDictionary<string, Task>();
        }

        public List<BackupJob> GetAllJobs() => _backupJobs;

        public void AddBackupJob(BackupJob job)
        {
            if (!_backupJobs.Any(bj => bj.Name.Equals(job.Name, StringComparison.OrdinalIgnoreCase)))
            {
                _backupJobs.Add(job);
            }
        }

        public void RemoveBackupJob(int index) // Typically by ID or unique name is safer than index if list reorders
        {
            if (index >= 0 && index < _backupJobs.Count)
                _backupJobs.RemoveAt(index);
        }

        public void RemoveBackupJob(BackupJob job) // Remove by object reference or a unique identifier
        {
            var jobToRemove = _backupJobs.FirstOrDefault(bj => bj.Name.Equals(job.Name, StringComparison.OrdinalIgnoreCase));
            if (jobToRemove != null)
                _backupJobs.Remove(jobToRemove);
        }

        public async Task ExecuteBackupJobAsync(BackupJob job)
        {
            // Wrap the execution in a task and store it if needed for parallel management
            // For single execution, direct await is fine.
            try
            {
                if (!Directory.Exists(job.SourceDirectory))
                    throw new DirectoryNotFoundException($"{LanguageManager.GetString("SourceDirNotFound")}: {job.SourceDirectory}");

                if (!Directory.Exists(job.TargetDirectory))
                    Directory.CreateDirectory(job.TargetDirectory);

                await _backupExecutorService.ExecuteBackupJobAsync(job);
            }
            catch (BusinessSoftwareInterruptionException)
            {
                // Logged and state updated by BackupExecutor, rethrow for MainViewModel to handle UI
                throw;
            }
            catch (Exception ex)
            {
                // Generic error handling, ensure state is updated
                var errorProgress = new BackupProgress
                {
                    JobName = job.Name,
                    State = BackupState.Error,
                    Timestamp = DateTime.Now
                };
                await _stateManager.UpdateStateAsync(errorProgress); // Ensure state reflects error

                await _logManager.LogFileOperationAsync(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    JobName = job.Name,
                    Message = $"Backup job '{job.Name}' failed at BackupManager level: {ex.Message}"
                });
                throw; // Rethrow for MainViewModel to handle UI
            }
        }

        public async Task PauseJobAsync(string jobName)
        {
            await _backupExecutorService.PauseJobAsync(jobName);
        }

        public async Task ResumeJobAsync(string jobName)
        {
            await _backupExecutorService.ResumeJobAsync(jobName);
        }

        public async Task StopJobAsync(string jobName)
        {
            await _backupExecutorService.StopJobAsync(jobName);
        }


        public void StartAllJobsInParallel(List<BackupJob> jobsToRun)
        {
            if (jobsToRun == null) return;

            foreach (var job in jobsToRun)
            {
                if (job == null || string.IsNullOrEmpty(job.Name)) continue;

                // Check if the job is already running and not completed.
                // The IsJobRunning check might be more robust if it considers tasks not yet completed.
                if (_runningJobTasks.TryGetValue(job.Name, out var existingTask) && !existingTask.IsCompleted)
                {
                    System.Diagnostics.Debug.WriteLine($"[BackupManager] Job '{job.Name}' is already running or queued. Skipping.");
                    continue;
                }

                // Task.Run to execute the job asynchronously
                var task = Task.Run(async () => await ExecuteBackupJobAsync(job));
                _runningJobTasks.AddOrUpdate(job.Name, task, (key, oldTask) =>
                {
                    // If there was an old task, this replacement logic might need to be more careful
                    // e.g., ensure oldTask is completed or cancelled. For simplicity here, just replace.
                    return task;
                });
                System.Diagnostics.Debug.WriteLine($"[BackupManager] Queued job '{job.Name}' for parallel execution.");
            }
        }

        public async Task WaitForAllJobsAsync()
        {
            // Wait for all tasks currently in _runningJobTasks to complete
            // This might need refinement if jobs are added/removed dynamically while waiting
            await Task.WhenAll(_runningJobTasks.Values.ToList()); // ToList to make a snapshot
        }

        public bool IsJobRunning(string jobName) // Check if a specific job's task is active
        {
            return _runningJobTasks.TryGetValue(jobName, out var task) && !task.IsCompleted;
        }

        public void ClearFinishedJobs() // Clean up completed/faulted tasks from the tracking dictionary
        {
            foreach (var kvp in _runningJobTasks.ToList()) // ToList to iterate over a snapshot
            {
                if (kvp.Value.IsCompleted) // IsCompleted is true for RanToCompletion, Faulted, or Canceled
                {
                    _runningJobTasks.TryRemove(kvp.Key, out _);
                }
            }
        }

        public EasySave.BackupExecutor.BackupExecutor GetBackupExecutor() // Expose executor for progress updates
        {
            return _backupExecutorService;
        }
    }
}