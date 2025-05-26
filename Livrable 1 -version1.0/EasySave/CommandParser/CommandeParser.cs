using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EasySave.Models;
using EasySave.Services;
using EasySave.Utils;

namespace EasySave.Core
{
    /// <summary>
    /// Implementation of ICommandParser that handles command line arguments
    /// </summary>
    public class CommandParser : ICommandParser
    {
        private readonly IBackupManager _backupManager;

        public CommandParser(IBackupManager backupManager)
        {
            _backupManager = backupManager ?? throw new ArgumentNullException(nameof(backupManager));
        }

        /// <summary>
        /// Parse command line arguments and execute the corresponding backup jobs
        /// </summary>
        public async Task ParseAndExecuteAsync(string[] args, IList<BackupJob> jobs)
        {
            if (args == null || args.Length == 0 || jobs == null || jobs.Count == 0)
            {
                return;
            }

            string jobsToRun = args[0];

            if (jobsToRun.Contains('-'))
            {
                // Range format (e.g. "1-3")
                string[] range = jobsToRun.Split('-');
                if (range.Length == 2 && int.TryParse(range[0], out int start) && int.TryParse(range[1], out int end))
                {
                    await RunJobsInRangeAsync(start, end, jobs);
                }
            }
            else if (jobsToRun.Contains(';'))
            {
                // Specific jobs format (e.g. "1;3")
                string[] jobIndices = jobsToRun.Split(';');
                List<int> indices = new List<int>();

                foreach (var index in jobIndices)
                {
                    if (int.TryParse(index, out int jobIndex))
                    {
                        indices.Add(jobIndex);
                    }
                }

                await RunSpecificJobsAsync(indices, jobs);
            }
            else if (int.TryParse(jobsToRun, out int singleJob))
            {
                // Single job
                await RunSingleJobAsync(singleJob, jobs);
            }
        }

        private async Task RunJobsInRangeAsync(int start, int end, IList<BackupJob> jobs)
        {
            for (int i = start; i <= end; i++)
            {
                await RunSingleJobAsync(i, jobs);
            }
        }

        private async Task RunSpecificJobsAsync(List<int> indices, IList<BackupJob> jobs)
        {
            foreach (int index in indices)
            {
                await RunSingleJobAsync(index, jobs);
            }
        }

        private async Task RunSingleJobAsync(int jobIndex, IList<BackupJob> jobs)
        {
            // Job indices are 1-based in user interface
            int arrayIndex = jobIndex - 1;

            if (arrayIndex >= 0 && arrayIndex < jobs.Count)
            {
                var job = jobs[arrayIndex];
                await _backupManager.ExecuteBackupJobAsync(job);
            }
            else
            {
                Console.WriteLine(LanguageManager.GetString("InvalidJobIndex"));
            }
        }
    }
}