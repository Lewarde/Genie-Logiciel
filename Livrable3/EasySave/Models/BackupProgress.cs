using System;

namespace EasySave.Models
{
    public class BackupProgress
    {
        public string JobName { get; set; }
        public DateTime Timestamp { get; set; }
        public BackupState State { get; set; }
        public int TotalFilesCount { get; set; }
        public long TotalFilesSize { get; set; }
        public int RemainingFilesCount { get; set; }
        public long RemainingFilesSize { get; set; }
        public string CurrentSourceFile { get; set; }
        public string CurrentTargetFile { get; set; }
        public int Progress => TotalFilesCount > 0 ? (int)(((double)(TotalFilesCount - RemainingFilesCount) / TotalFilesCount) * 100) : 0;

        public BackupProgress()
        {
            JobName = string.Empty;
            Timestamp = DateTime.Now;
            State = BackupState.Inactive;
            CurrentSourceFile = string.Empty;
            CurrentTargetFile = string.Empty;
        }
    }

    public enum BackupState
    {
        Inactive,   // Not yet started or after completion/error/stop
        Active,     // Currently processing files
        Paused,     // Execution is paused by user
        Stopped,    // Execution is stopped by user
        Completed,  // All files processed successfully
        Error,      // An error occurred during execution
        Interrupted // Interrupted by business software
    }
}