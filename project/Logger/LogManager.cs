using System;
using System.IO;
using System.Threading.Tasks;
using EasySave.Models;

namespace Logger
{
    public class LogManager
    {
        private static LogManager _instance;
        private static readonly object _initLock = new();

        private readonly string _logDirectory;
        private readonly object _lockObject = new();
        private readonly ILogWriter _logWriter;

        private LogManager(string format)
        {
            _logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "EasySave", "Logs");

            if (!Directory.Exists(_logDirectory))
                Directory.CreateDirectory(_logDirectory);

            _logWriter = format == "XML"
                ? new XmlLogWriter(_logDirectory)
                : new JsonLogWriter(_logDirectory);
        }

        public static void Initialize(string format)
        {
            lock (_initLock)
            {
                if (_instance == null)
                {
                    _instance = new LogManager(format);
                }
            }
        }

        public static LogManager Instance
        {
            get
            {
                if (_instance == null)
                    throw new InvalidOperationException("LogManager not initialized. Call Initialize() first.");
                return _instance;
            }
        }

        public async Task LogFileOperationAsync(LogEntry logEntry)
        {
            await Task.Run(() => _logWriter.WriteLog(logEntry));
        }
    }
}