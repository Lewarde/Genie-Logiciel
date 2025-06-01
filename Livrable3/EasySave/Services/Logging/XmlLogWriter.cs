using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using EasySave.Models;

namespace Logger
{
    // Implements a log writer that writes log entries in XML format.
    public class XmlLogWriter : ILogWriter
    {
        private readonly string _logDirectory; // Directory where log files will be stored.
        private static readonly object _fileLock = new object(); // Lock object for thread-safe file operations.

        // Constructor initializes the log directory.
        public XmlLogWriter(string logDirectory)
        {
            _logDirectory = logDirectory;
        }

        // Writes a log entry to an XML log file.
        public void WriteLog(LogEntry entry)
        {
            // Define the log file name using the current date.
            string logFileName = DateTime.Now.ToString("yyyy-MM-dd") + ".xml";
            string logFilePath = Path.Combine(_logDirectory, logFileName);

            // Create a new XML log entry from the provided log entry.
            var newLogEntry = new XmlLogEntry
            {
                JobName = entry.JobName,
                SourceFile = entry.SourceFile,
                TargetFile = entry.TargetFile,
                FileSize = entry.FileSize,
                TransferTimeSec = Math.Round((double)entry.TransferTimeMs / 1000, 3),
                EncryptionTimeMs = entry.EncryptionTimeMs,
                Timestamp = entry.Timestamp.ToString("dd/MM/yyyy HH:mm:ss"),
                Message = entry.Message
            };

            lock (_fileLock)
            {
                List<XmlLogEntry> entries;
                var serializer = new XmlSerializer(typeof(List<XmlLogEntry>), new XmlRootAttribute("LogEntries"));

                // Load existing log entries if the log file exists.
                if (File.Exists(logFilePath))
                {
                    try
                    {
                        using (var reader = new StreamReader(logFilePath))
                        {
                            // Avoid deserializing an empty file which causes an error.
                            entries = reader.Peek() >= 0
                                ? (List<XmlLogEntry>)serializer.Deserialize(reader)
                                : new List<XmlLogEntry>();
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

                // Add the new log entry to the list.
                entries.Add(newLogEntry);

                // Serialize and write the updated list of log entries back to the file.
                using (var writer = new StreamWriter(logFilePath, false))
                {
                    serializer.Serialize(writer, entries);
                }
            }
        }

        // Represents a log entry in XML format.
        [Serializable]
        public class XmlLogEntry
        {
            public string JobName { get; set; } // Name of the job.
            public string SourceFile { get; set; } // Source file path.
            public string TargetFile { get; set; } // Target file path.
            public long FileSize { get; set; } // Size of the file.
            public double TransferTimeSec { get; set; } // Time taken to transfer the file in seconds.
            public long EncryptionTimeMs { get; set; } // Time taken to encrypt the file in milliseconds.
            public string Timestamp { get; set; } // Timestamp of the log entry.
            [System.Xml.Serialization.XmlElement(IsNullable = true)] // Allow null or empty message.
            public string Message { get; set; } // General message associated with the log entry.
        }
    }
}
