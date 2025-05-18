using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EasySave.Models;
using EasySave.Utils;
using Logger;

namespace EasySave.Services
{
    public class BackupManager
    {
        private readonly List<BackupJob> _backupJobs;
        private readonly StateManager _stateManager;
        private readonly LogManager _logManager;

        public BackupManager()
        {
            _backupJobs = new List<BackupJob>();
            _stateManager = new StateManager();
            _logManager = LogManager.Instance;
        }

        public List<BackupJob> GetAllJobs() => _backupJobs;

        public void AddBackupJob(BackupJob job) => _backupJobs.Add(job);

        public void RemoveBackupJob(int index)
        {
            if (index >= 0 && index < _backupJobs.Count)
                _backupJobs.RemoveAt(index);
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

                if (job.Type == BackupType.Full)
                    await PerformFullBackupAsync(job, progress, files, sourceDir);
                else
                    await PerformDifferentialBackupAsync(job, progress, files, sourceDir);

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

        private async Task PerformFullBackupAsync(BackupJob job, BackupProgress progress, FileInfo[] files, DirectoryInfo sourceDir)
        {
            int processedCount = 0;

            foreach (var file in files)
            {
                string relativePath = file.FullName.Substring(sourceDir.FullName.Length).TrimStart(Path.DirectorySeparatorChar);
                string targetPath = Path.Combine(job.TargetDirectory, relativePath);
                string targetDir = Path.GetDirectoryName(targetPath);

                progress.CurrentSourceFile = file.FullName;
                progress.CurrentTargetFile = targetPath;
                await _stateManager.UpdateStateAsync(progress);

                try
                {
                    if (!Directory.Exists(targetDir))
                        Directory.CreateDirectory(targetDir);

                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    File.Copy(file.FullName, targetPath, true);
                    stopwatch.Stop();

                    await _logManager.LogFileOperationAsync(new LogEntry
                    {
                        JobName = job.Name,
                        SourceFile = file.FullName,
                        TargetFile = targetPath,
                        FileSize = file.Length,
                        TransferTimeMs = stopwatch.ElapsedMilliseconds
                    });

                    processedCount++;
                    progress.RemainingFilesCount = files.Length - processedCount;
                    progress.RemainingFilesSize -= file.Length;
                    await _stateManager.UpdateStateAsync(progress);

                    int percentage = (int)(processedCount * 100.0 / files.Length);
                    Console.Write($"\r{LanguageManager.GetString("Progress")}: {percentage}% ({processedCount}/{files.Length})");
                }
                catch (Exception ex)
                {
                    await _logManager.LogFileOperationAsync(new LogEntry
                    {
                        JobName = job.Name,
                        SourceFile = file.FullName,
                        TargetFile = targetPath,
                        FileSize = file.Length,
                        TransferTimeMs = -1
                    });

                    Console.WriteLine();
                    Console.WriteLine(LanguageManager.GetString("ErrorCopyingFile") + file.FullName + ": " + ex.Message);
                }
            }

            Console.WriteLine();
        }

        private async Task PerformDifferentialBackupAsync(BackupJob job, BackupProgress progress, FileInfo[] files, DirectoryInfo sourceDir)
        {
            int processedCount = 0;
            var filesToCopyList = new List<FileInfo>();
            long totalSize = 0;

            foreach (var file in files)
            {
                string relativePath = file.FullName.Substring(sourceDir.FullName.Length).TrimStart(Path.DirectorySeparatorChar);
                string targetPath = Path.Combine(job.TargetDirectory, relativePath);

                if (!File.Exists(targetPath) ||
                    File.GetLastWriteTime(file.FullName) > File.GetLastWriteTime(targetPath) ||
                    file.Length != new FileInfo(targetPath).Length)
                {
                    filesToCopyList.Add(file);
                    totalSize += file.Length;
                }
            }

            progress.TotalFilesCount = filesToCopyList.Count;
            progress.TotalFilesSize = totalSize;
            progress.RemainingFilesCount = filesToCopyList.Count;
            progress.RemainingFilesSize = totalSize;
            await _stateManager.UpdateStateAsync(progress);

            if (filesToCopyList.Count == 0)
            {
                Console.WriteLine(LanguageManager.GetString("NoFilesToCopy"));
                return;
            }

            foreach (var file in filesToCopyList)
            {
                string relativePath = file.FullName.Substring(sourceDir.FullName.Length).TrimStart(Path.DirectorySeparatorChar);
                string targetPath = Path.Combine(job.TargetDirectory, relativePath);
                string targetDir = Path.GetDirectoryName(targetPath);

                progress.CurrentSourceFile = file.FullName;
                progress.CurrentTargetFile = targetPath;
                await _stateManager.UpdateStateAsync(progress);

                try
                {
                    if (!Directory.Exists(targetDir))
                        Directory.CreateDirectory(targetDir);

                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    File.Copy(file.FullName, targetPath, true);
                    stopwatch.Stop();

                    await _logManager.LogFileOperationAsync(new LogEntry
                    {
                        JobName = job.Name,
                        SourceFile = file.FullName,
                        TargetFile = targetPath,
                        FileSize = file.Length,
                        TransferTimeMs = stopwatch.ElapsedMilliseconds
                    });

                    processedCount++;
                    progress.RemainingFilesCount = filesToCopyList.Count - processedCount;
                    progress.RemainingFilesSize -= file.Length;
                    await _stateManager.UpdateStateAsync(progress);

                    int percentage = (int)(processedCount * 100.0 / filesToCopyList.Count);
                    Console.Write($"\r{LanguageManager.GetString("Progress")}: {percentage}% ({processedCount}/{filesToCopyList.Count})");
                }
                catch (Exception ex)
                {
                    await _logManager.LogFileOperationAsync(new LogEntry
                    {
                        JobName = job.Name,
                        SourceFile = file.FullName,
                        TargetFile = targetPath,
                        FileSize = file.Length,
                        TransferTimeMs = -1
                    });

                    Console.WriteLine();
                    Console.WriteLine(LanguageManager.GetString("ErrorCopyingFile") + file.FullName + ": " + ex.Message);
                }
            }

            Console.WriteLine();
        }
    }
}
