using EasySave.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EasySave.Services
{
    public interface IBackupManager
    {
        List<BackupJob> GetAllJobs();
        void AddBackupJob(BackupJob job);
        void RemoveBackupJob(int index);
        Task ExecuteBackupJobAsync(BackupJob job);
    }
}