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
                FileTransferTime = Math.Round((double)entry.TransferTimeMs / 1000, 3),
                Time = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
            };

            string json = JsonSerializer.Serialize(logLine);
            File.AppendAllText(logFilePath, json + Environment.NewLine);
        }
    }
}