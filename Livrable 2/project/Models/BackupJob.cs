using System;

namespace EasySave.Models
{
    public class BackupJob
    {
        public string Name { get; set; }
        public string SourceDirectory { get; set; }
        public string TargetDirectory { get; set; }
        public BackupType Type { get; set; }
        
        public BackupJob()
        {
            Name = string.Empty;
            SourceDirectory = string.Empty;
            TargetDirectory = string.Empty;
            Type = BackupType.Full;
        }
    }
    
    public enum BackupType
    {
        Full,
        Differential
    }
}