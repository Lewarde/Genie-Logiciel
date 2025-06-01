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
            PauseEvent = new ManualResetEventSlim(true); 
        }

        public void Dispose()
        {
            Cts.Dispose();
            PauseEvent.Dispose();
        }
    }
}