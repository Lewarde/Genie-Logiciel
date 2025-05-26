using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using EasySave.Models;

namespace Logger
{
    public class XmlLogWriter : ILogWriter
    {
        private readonly string _logDirectory;
        private static readonly object _fileLock = new object();

        public XmlLogWriter(string logDirectory)
        {
            _logDirectory = logDirectory;
        }

        public void WriteLog(LogEntry entry)
        {
            string logFileName = DateTime.Now.ToString("yyyy-MM-dd") + ".xml";
            string logFilePath = Path.Combine(_logDirectory, logFileName);

            var newLogEntry = new XmlLogEntry
            {
                JobName = entry.JobName,
                SourceFile = entry.SourceFile,
                TargetFile = entry.TargetFile,
                FileSize = entry.FileSize,
                TransferTimeSec = Math.Round((double)entry.TransferTimeMs / 1000, 3),
                EncryptionTimeMs = entry.EncryptionTimeMs,
                Timestamp = entry.Timestamp.ToString("dd/MM/yyyy HH:mm:ss"), // Use entry's timestamp
                Message = entry.Message // New field for general messages
            };

            lock (_fileLock)
            {
                List<XmlLogEntry> entries;
                var serializer = new XmlSerializer(typeof(List<XmlLogEntry>), new XmlRootAttribute("LogEntries"));

                if (File.Exists(logFilePath))
                {
                    try
                    {
                        using (var reader = new StreamReader(logFilePath))
                        {
                            // Avoid deserializing an empty file which causes an error
                            if (reader.Peek() >= 0)
                            {
                                entries = (List<XmlLogEntry>)serializer.Deserialize(reader);
                            }
                            else
                            {
                                entries = new List<XmlLogEntry>();
                            }
                        }
                    }
                    catch
                    {
                        entries = new List<XmlLogEntry>();
                    }
                }
                else
                {
                    entries = new List<XmlLogEntry>();
                }

                entries.Add(newLogEntry);

                using (var writer = new StreamWriter(logFilePath, false))
                {
                    serializer.Serialize(writer, entries);
                }
            }
        }

        [Serializable]
        public class XmlLogEntry
        {
            public string JobName { get; set; }
            public string SourceFile { get; set; }
            public string TargetFile { get; set; }
            public long FileSize { get; set; }
            public double TransferTimeSec { get; set; }
            public long EncryptionTimeMs { get; set; }
            public string Timestamp { get; set; }
            [System.Xml.Serialization.XmlElement(IsNullable = true)] // Allow null/empty message
            public string Message { get; set; } // New field
        }
    }
}