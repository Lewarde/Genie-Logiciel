using System;
using System.IO;
using System.Threading.Tasks;
using EasySave.Models;

namespace Logger
{
    // Manages logging operations using a specified log format.
    public class LogManager
    {
        private static LogManager _instance; // Singleton instance of LogManager.
        private static readonly object _initLock = new(); // Lock object for thread-safe initialization.

        private readonly string _logDirectory; // Directory where logs will be stored.
        private readonly object _lockObject = new(); // Lock object for thread-safe operations.
        private readonly ILogWriter _logWriter; // Log writer instance for writing logs.

        // Private constructor initializes the log directory and log writer.
        private LogManager(string format)
        {
            // Set the log directory path.
            _logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "EasySave", "Logs");

            // Create the log directory if it doesn't exist.
            if (!Directory.Exists(_logDirectory))
                Directory.CreateDirectory(_logDirectory);

            // Initialize the appropriate log writer based on the specified format.
            _logWriter = format == "XML"
                ? new XmlLogWriter(_logDirectory)
                : new JsonLogWriter(_logDirectory);
        }

        // Initializes the LogManager singleton instance.
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

        // Provides access to the LogManager singleton instance.
        public static LogManager Instance
        {
            get
            {
                if (_instance == null)
                    throw new InvalidOperationException("LogManager not initialized. Call Initialize() first.");
                return _instance;
            }
        }

        // Asynchronously logs a file operation.
        public async Task LogFileOperationAsync(LogEntry logEntry)
        {
            await Task.Run(() => _logWriter.WriteLog(logEntry));
        }
    }
}
