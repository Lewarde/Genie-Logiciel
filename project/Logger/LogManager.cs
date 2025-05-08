using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using EasySave.Models;

namespace Logger
{
    public class LogManager
    {
        private readonly string _logDirectory;
        private readonly object _lockObject = new();

        public LogManager()
        {
            _logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EasySave", "Logs");
            if (!Directory.Exists(_logDirectory))
                Directory.CreateDirectory(_logDirectory);
        }

        public async Task LogFileOperationAsync(LogEntry logEntry)
        {
            try
            {
                string logFileName = DateTime.Now.ToString("yyyy-MM-dd") + ".json";
                string logFilePath = Path.Combine(_logDirectory, logFileName);

                var logLine = new
                {
                    Name = logEntry.JobName,
                    FileSource = logEntry.SourceFile,
                    FileTarget = logEntry.TargetFile,
                    FileSize = logEntry.FileSize,
                    FileTransferTime = Math.Round((double)logEntry.TransferTimeMs / 1000, 3),
                    time = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
                };

                string jsonLine = JsonSerializer.Serialize(logLine);

                lock (_lockObject)
                {
                    File.AppendAllText(logFilePath, jsonLine + ",\n");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to log file: {ex.Message}");
            }
        }
    }
}