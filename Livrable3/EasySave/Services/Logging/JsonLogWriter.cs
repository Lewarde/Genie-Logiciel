using System;
using System.IO;
using System.Text.Json;
using EasySave.Models;

namespace Logger
{
    // Implements a log writer that writes log entries in JSON format.
    public class JsonLogWriter : ILogWriter
    {
        private readonly string _logDirectory; // Directory where log files will be stored.
        private static readonly object _fileLock = new object(); // Lock object for thread-safe file operations.

        // Constructor initializes the log directory.
        public JsonLogWriter(string logDirectory)
        {
            _logDirectory = logDirectory;
        }

        // Writes a log entry to a JSON log file.
        public void WriteLog(LogEntry entry)
        {
            // Define the log file name using the current date.
            string logFileName = DateTime.Now.ToString("yyyy-MM-dd") + ".json";
            string logFilePath = Path.Combine(_logDirectory, logFileName);

            // Create an object to represent the log entry.
            var logLine = new
            {
                Name = entry.JobName,
                FileSource = entry.SourceFile,
                FileTarget = entry.TargetFile,
                FileSize = entry.FileSize,
                FileTransferTime = Math.Round((double)entry.TransferTimeMs / 1000, 3),
                EncryptionTime = entry.EncryptionTimeMs,
                Time = entry.Timestamp.ToString("dd/MM/yyyy HH:mm:ss"),
                Message = entry.Message
            };

            // Configure JSON serialization options.
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false
            };

            // Serialize the log entry to JSON.
            string json = JsonSerializer.Serialize(logLine, options);

            // Use a lock to ensure thread-safe file writing.
            lock (_fileLock)
            {
                try
                {
                    // Append the JSON log entry to the log file.
                    File.AppendAllText(logFilePath, json + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    // Log any errors that occur during file writing.
                    Console.WriteLine($"[JsonLogWriter] Error writing to log file {logFilePath}: {ex.Message}");
                }
            }
        }
    }
}
