using System;
using System.Threading.Tasks;
using EasySave.Models;
using EasySave.Services;
using EasySave.Utils;

namespace EasySave.Core
{
    /// <summary>
    /// Implementation of IMenuManager that handles interactive menu operations
    /// </summary>
    public class MenuManager : IMenuManager
    {
        private readonly IBackupManager _backupManager;
        private readonly Func<Task> _saveConfigurationAsync;

        public MenuManager(IBackupManager backupManager, Func<Task> saveConfigurationAsync)
        {
            _backupManager = backupManager ?? throw new ArgumentNullException(nameof(backupManager));
            _saveConfigurationAsync = saveConfigurationAsync ?? throw new ArgumentNullException(nameof(saveConfigurationAsync));
        }

        /// <summary>
        /// Starts the interactive menu system
        /// </summary>
        public async Task StartInteractiveMenuAsync()
        {
            bool exit = false;

            while (!exit)
            {
                DisplayMainMenu();

                string input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        await ManageBackupJobsAsync();
                        break;
                    case "2":
                        await ExecuteBackupJobsAsync();
                        break;
                    case "3":
                        exit = true;
                        break;
                    default:
                        Console.WriteLine(LanguageManager.GetString("InvalidOption"));
                        break;
                }
            }
        }

        private void DisplayMainMenu()
        {
            Console.Clear();
            Console.WriteLine("===== EasySave 1.0 =====");
            Console.WriteLine("1. " + LanguageManager.GetString("ManageBackupJobs"));
            Console.WriteLine("2. " + LanguageManager.GetString("ExecuteBackupJobs"));
            Console.WriteLine("3. " + LanguageManager.GetString("Exit"));
            Console.Write(LanguageManager.GetString("EnterChoice") + " ");
        }

        private async Task ManageBackupJobsAsync()
        {
            bool back = false;

            while (!back)
            {
                DisplayJobManagementMenu();

                string input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        await CreateBackupJobAsync();
                        break;
                    case "2":
                        await ModifyBackupJobAsync();
                        break;
                    case "3":
                        await DeleteBackupJobAsync();
                        break;
                    case "4":
                        DisplayBackupJobs();
                        Console.ReadKey();
                        break;
                    case "5":
                        back = true;
                        break;
                    default:
                        Console.WriteLine(LanguageManager.GetString("InvalidOption"));
                        break;
                }
            }
        }

        private void DisplayJobManagementMenu()
        {
            Console.Clear();
            Console.WriteLine("===== " + LanguageManager.GetString("ManageBackupJobs") + " =====");
            Console.WriteLine("1. " + LanguageManager.GetString("CreateBackupJob"));
            Console.WriteLine("2. " + LanguageManager.GetString("ModifyBackupJob"));
            Console.WriteLine("3. " + LanguageManager.GetString("DeleteBackupJob"));
            Console.WriteLine("4. " + LanguageManager.GetString("DisplayBackupJobs"));
            Console.WriteLine("5. " + LanguageManager.GetString("Back"));
            Console.Write(LanguageManager.GetString("EnterChoice") + " ");
        }

        private async Task CreateBackupJobAsync()
        {
            Console.Clear();
            Console.WriteLine("===== " + LanguageManager.GetString("CreateBackupJob") + " =====");

            if (_backupManager.GetAllJobs().Count >= 5)
            {
                Console.WriteLine(LanguageManager.GetString("MaxJobsReached"));
                Console.ReadKey();
                return;
            }

            // Get job details
            Console.Write(LanguageManager.GetString("EnterJobName") + " ");
            string name = Console.ReadLine();

            Console.Write(LanguageManager.GetString("EnterSourceDir") + " ");
            string sourceDir = Console.ReadLine();

            Console.Write(LanguageManager.GetString("EnterTargetDir") + " ");
            string targetDir = Console.ReadLine();

            Console.WriteLine(LanguageManager.GetString("SelectBackupType"));
            Console.WriteLine("1. " + LanguageManager.GetString("FullBackup"));
            Console.WriteLine("2. " + LanguageManager.GetString("DifferentialBackup"));
            Console.Write(LanguageManager.GetString("EnterChoice") + " ");

            string typeInput = Console.ReadLine();
            BackupType type = typeInput == "2" ? BackupType.Differential : BackupType.Full;

            // Create and add the job
            var job = new BackupJob
            {
                Name = name,
                SourceDirectory = sourceDir,
                TargetDirectory = targetDir,
                Type = type
            };

            _backupManager.AddBackupJob(job);
            await _saveConfigurationAsync();

            Console.WriteLine(LanguageManager.GetString("JobCreatedSuccessfully"));
            Console.ReadKey();
        }

        private async Task ModifyBackupJobAsync()
        {
            Console.Clear();
            DisplayBackupJobs();

            if (_backupManager.GetAllJobs().Count == 0)
            {
                Console.ReadKey();
                return;
            }

            Console.Write(LanguageManager.GetString("EnterJobIndexToModify") + " ");
            if (!int.TryParse(Console.ReadLine(), out int index) || index < 1 || index > _backupManager.GetAllJobs().Count)
            {
                Console.WriteLine(LanguageManager.GetString("InvalidJobIndex"));
                Console.ReadKey();
                return;
            }

            // Get the job to modify
            var job = _backupManager.GetAllJobs()[index - 1];

            Console.Write(LanguageManager.GetString("EnterJobName") + $" ({job.Name}): ");
            string name = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(name))
            {
                job.Name = name;
            }

            Console.Write(LanguageManager.GetString("EnterSourceDir") + $" ({job.SourceDirectory}): ");
            string sourceDir = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(sourceDir))
            {
                job.SourceDirectory = sourceDir;
            }

            Console.Write(LanguageManager.GetString("EnterTargetDir") + $" ({job.TargetDirectory}): ");
            string targetDir = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(targetDir))
            {
                job.TargetDirectory = targetDir;
            }

            Console.WriteLine(LanguageManager.GetString("SelectBackupType") + $" ({job.Type}):");
            Console.WriteLine("1. " + LanguageManager.GetString("FullBackup"));
            Console.WriteLine("2. " + LanguageManager.GetString("DifferentialBackup"));
            Console.Write(LanguageManager.GetString("EnterChoice") + " ");

            string typeInput = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(typeInput))
            {
                job.Type = typeInput == "2" ? BackupType.Differential : BackupType.Full;
            }

            await _saveConfigurationAsync();

            Console.WriteLine(LanguageManager.GetString("JobModifiedSuccessfully"));
            Console.ReadKey();
        }

        private async Task DeleteBackupJobAsync()
        {
            Console.Clear();
            DisplayBackupJobs();

            if (_backupManager.GetAllJobs().Count == 0)
            {
                Console.ReadKey();
                return;
            }

            Console.Write(LanguageManager.GetString("EnterJobIndexToDelete") + " ");
            if (!int.TryParse(Console.ReadLine(), out int index) || index < 1 || index > _backupManager.GetAllJobs().Count)
            {
                Console.WriteLine(LanguageManager.GetString("InvalidJobIndex"));
                Console.ReadKey();
                return;
            }

            _backupManager.RemoveBackupJob(index - 1);
            await _saveConfigurationAsync();

            Console.WriteLine(LanguageManager.GetString("JobDeletedSuccessfully"));
            Console.ReadKey();
        }

        private void DisplayBackupJobs()
        {
            Console.WriteLine("===== " + LanguageManager.GetString("BackupJobs") + " =====");

            var jobs = _backupManager.GetAllJobs();

            if (jobs.Count == 0)
            {
                Console.WriteLine(LanguageManager.GetString("NoJobsCreated"));
                return;
            }

            for (int i = 0; i < jobs.Count; i++)
            {
                var job = jobs[i];
                Console.WriteLine($"{i + 1}. {job.Name} ({job.Type})");
                Console.WriteLine($"   {LanguageManager.GetString("Source")}: {job.SourceDirectory}");
                Console.WriteLine($"   {LanguageManager.GetString("Target")}: {job.TargetDirectory}");
                Console.WriteLine();
            }
        }

        private async Task ExecuteBackupJobsAsync()
        {
            Console.Clear();
            DisplayBackupJobs();

            if (_backupManager.GetAllJobs().Count == 0)
            {
                Console.ReadKey();
                return;
            }

            Console.WriteLine(LanguageManager.GetString("ExecutionOptions"));
            Console.WriteLine("1. " + LanguageManager.GetString("ExecuteSingleJob"));
            Console.WriteLine("2. " + LanguageManager.GetString("ExecuteAllJobs"));
            Console.WriteLine("3. " + LanguageManager.GetString("Back"));
            Console.Write(LanguageManager.GetString("EnterChoice") + " ");

            string input = Console.ReadLine();

            switch (input)
            {
                case "1":
                    Console.Write(LanguageManager.GetString("EnterJobIndexToExecute") + " ");
                    if (int.TryParse(Console.ReadLine(), out int index))
                    {
                        await RunSingleJobAsync(index);
                    }
                    else
                    {
                        Console.WriteLine(LanguageManager.GetString("InvalidJobIndex"));
                    }
                    Console.ReadKey();
                    break;
                case "2":
                    await RunAllJobsAsync();
                    Console.ReadKey();
                    break;
                case "3":
                    return;
                default:
                    Console.WriteLine(LanguageManager.GetString("InvalidOption"));
                    Console.ReadKey();
                    break;
            }
        }

        private async Task RunSingleJobAsync(int jobIndex)
        {
            // Job indices are 1-based in user interface
            int arrayIndex = jobIndex - 1;

            if (arrayIndex >= 0 && arrayIndex < _backupManager.GetAllJobs().Count)
            {
                var job = _backupManager.GetAllJobs()[arrayIndex];
                await _backupManager.ExecuteBackupJobAsync(job);
            }
            else
            {
                Console.WriteLine(LanguageManager.GetString("InvalidJobIndex"));
            }
        }

        private async Task RunAllJobsAsync()
        {
            var jobs = _backupManager.GetAllJobs();
            for (int i = 1; i <= jobs.Count; i++)
            {
                await RunSingleJobAsync(i);
            }
        }
    }
}