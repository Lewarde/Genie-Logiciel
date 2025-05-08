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
        
        public LogEntry()
        {
            Timestamp = DateTime.Now;
            JobName = string.Empty;
            SourceFile = string.Empty;
            TargetFile = string.Empty;
        }
    }
}