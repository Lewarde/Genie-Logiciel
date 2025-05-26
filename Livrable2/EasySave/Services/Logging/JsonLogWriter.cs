using System;
using System.IO;
using System.Text.Json;
using EasySave.Models;

namespace Logger
{
    public class JsonLogWriter : ILogWriter
    {
        private readonly string _logDirectory;

        public JsonLogWriter(string logDirectory)
        {
            _logDirectory = logDirectory;
        }

        public void WriteLog(LogEntry entry)
        {
            string logFileName = DateTime.Now.ToString("yyyy-MM-dd") + ".json";
            string logFilePath = Path.Combine(_logDirectory, logFileName);

            var logLine = new
            {
                Name = entry.JobName,
                FileSource = entry.SourceFile,
                FileTarget = entry.TargetFile,
                FileSize = entry.FileSize,
                FileTransferTime = Math.Round((double)entry.TransferTimeMs / 1000, 3), // En secondes
                EncryptionTime = entry.EncryptionTimeMs, // En millisecondes
                Time = entry.Timestamp.ToString("dd/MM/yyyy HH:mm:ss"), // Use entry's timestamp
                Message = entry.Message // New field for general messages
            };

            // Option to exclude null or empty fields from serialization if desired
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false // Typically, each log entry is a compact line
            };

            string json = JsonSerializer.Serialize(logLine, options);

            // For a simple approach where each log is a JSON object on a new line:
            File.AppendAllText(logFilePath, json + Environment.NewLine);
        }
    }
}