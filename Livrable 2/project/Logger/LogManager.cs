using System;
using System.IO;
using System.Threading.Tasks;
using EasySave.Models;

namespace Logger
{
    public class LogManager
    {
        private readonly string _logDirectory;
        private readonly object _lockObject = new();
        private ILogWriter _logWriter;

        public LogManager(ILogWriter logWriter)
        {
            _logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EasySave", "Logs");
            if (!Directory.Exists(_logDirectory))
                Directory.CreateDirectory(_logDirectory);

            _logWriter = logWriter;
        }

        public async Task LogFileOperationAsync(LogEntry logEntry)
        {
            await Task.Run(() => _logWriter.WriteLog(logEntry));
        }
    }
}