using System.Collections.Generic;
using System.Threading.Tasks;
using EasySave.Models;

namespace EasySave.Core
{
    /// <summary>
    /// Interface for parsing command line arguments
    /// </summary>
    public interface ICommandParser
    {
        /// <summary>
        /// Parse command line arguments to determine which jobs should be executed
        /// </summary>
        /// <param name="args">Command line arguments array</param>
        /// <param name="jobs">List of available backup jobs</param>
        /// <returns>Task to execute jobs based on parsed arguments</returns>
        Task ParseAndExecuteAsync(string[] args, IList<BackupJob> jobs);
    }
}