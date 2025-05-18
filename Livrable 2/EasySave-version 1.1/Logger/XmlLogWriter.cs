using System;
using System.IO;
using System.Xml.Serialization;
using EasySave.Models;

namespace Logger
{
    public class XmlLogWriter : ILogWriter
    {
        private readonly string _logDirectory;

        public XmlLogWriter(string logDirectory)
        {
            _logDirectory = logDirectory;
        }

        public void WriteLog(LogEntry entry)
        {
            string logFileName = DateTime.Now.ToString("yyyy-MM-dd") + ".xml";
            string logFilePath = Path.Combine(_logDirectory, logFileName);

            var logEntry = new XmlLogEntry
            {
                JobName = entry.JobName,
                SourceFile = entry.SourceFile,
                TargetFile = entry.TargetFile,
                FileSize = entry.FileSize,
                TransferTimeSec = Math.Round((double)entry.TransferTimeMs / 1000, 3),
                Timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
            };

            var serializer = new XmlSerializer(typeof(XmlLogEntry));
            using var writer = new StreamWriter(logFilePath, append: true);
            serializer.Serialize(writer, logEntry);
        }

        [Serializable]
        public class XmlLogEntry
        {
            public string JobName { get; set; }
            public string SourceFile { get; set; }
            public string TargetFile { get; set; }
            public long FileSize { get; set; }
            public double TransferTimeSec { get; set; }
            public string Timestamp { get; set; }
        }
    }
}