// Create this new file: EasySave.BackupExecutor/JobControlContext.cs
using System.Threading;

namespace EasySave.BackupExecutor
{
    public class JobControlContext
    {
        public CancellationTokenSource Cts { get; }
        public ManualResetEventSlim PauseEvent { get; }

        public JobControlContext()
        {
            Cts = new CancellationTokenSource();
            PauseEvent = new ManualResetEventSlim(true); // Initially not paused
        }

        public void Dispose()
        {
            Cts.Dispose();
            PauseEvent.Dispose();
        }
    }
}