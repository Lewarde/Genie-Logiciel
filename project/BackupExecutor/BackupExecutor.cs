using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasySave.Models;
using EasySave.Services;
using EasySave.Utils;
using Logger;

namespace EasySave.BackupExecutor
{
    public class BackupExecutor : IBackupExecutor
    {
        private readonly LogManager _logManager;
        private readonly StateManager _stateManager;

        public BackupExecutor(LogManager logManager, StateManager stateManager)
        {
            _logManager = logManager;
            _stateManager = stateManager;
        }

        public async Task ExecuteBackupJobAsync(BackupJob job)
        {
            DirectoryInfo sourceDir = new DirectoryInfo(job.SourceDirectory);
            FileInfo[] files = sourceDir.GetFiles("*", SearchOption.AllDirectories);

            var progress = new BackupProgress
            {
                JobName = job.Name,
                Timestamp = DateTime.Now,
                State = BackupState.Active,
                TotalFilesCount = files.Length,
                TotalFilesSize = files.Sum(f => f.Length),
                RemainingFilesCount = files.Length,
                RemainingFilesSize = files.Sum(f => f.Length),
            };

            if (job.Type == BackupType.Full)
                await PerformFullBackupAsync(job, progress, files, sourceDir);
            else
                await PerformDifferentialBackupAsync(job, progress, files, sourceDir);

            progress.State = BackupState.Inactive;
            await _stateManager.UpdateStateAsync(progress);
        }

        private async Task PerformFullBackupAsync(BackupJob job, BackupProgress progress, FileInfo[] files, DirectoryInfo sourceDir)
        {
            int processedCount = 0;
            Console.WriteLine($"[DEBUG] Nombre de fichiers trouvés : {files.Length}");

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
