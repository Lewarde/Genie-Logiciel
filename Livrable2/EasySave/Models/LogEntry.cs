using System;

namespace EasySave.Models
{
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string JobName { get; set; }
        public string SourceFile { get; set; }
        public string TargetFile { get; set; }
        public long FileSize { get; set; }
        public long TransferTimeMs { get; set; }
        public long EncryptionTimeMs { get; set; } // Nouveau champ pour le temps de chiffrement
        public string Message { get; set; } // For general messages, like interruptions or non-file specific logs

        public LogEntry()
        {
            Timestamp = DateTime.Now;
            JobName = string.Empty;
            SourceFile = string.Empty;
            TargetFile = string.Empty;
            Message = string.Empty; // Initialize to empty
            EncryptionTimeMs = 0;
        }
    }
}