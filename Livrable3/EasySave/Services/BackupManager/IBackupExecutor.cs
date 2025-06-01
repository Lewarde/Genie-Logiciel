using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasySave.Models;

namespace EasySave.BackupExecutor
{
    internal interface IBackupExecutor
    {
        Task ExecuteBackupJobAsync(BackupJob job);
        Task PauseJobAsync(string jobName);
        Task ResumeJobAsync(string jobName);
        Task StopJobAsync(string jobName);

        event Action<BackupProgress> ProgressUpdated;

    }
}   