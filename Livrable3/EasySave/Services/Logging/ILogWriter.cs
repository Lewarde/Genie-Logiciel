using EasySave.Models;

namespace Logger
{
    public interface ILogWriter
    {
        void WriteLog(LogEntry entry);
    }
}