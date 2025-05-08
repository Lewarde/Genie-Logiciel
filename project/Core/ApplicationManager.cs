using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using EasySave.Models;
using EasySave.Services;
using EasySave.Utils;

namespace EasySave.Core
{
    public class ApplicationManager
    {
        private readonly BackupManager _backupManager;
        private readonly string _configFilePath;
        
        public ApplicationManager()
        {
            _backupManager = new BackupManager();
            
            // Define configuration file path in user profile
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave");
                
            // Create directory if it doesn't exist
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }
            
            _configFilePath = Path.Combine(appDataPath, "config.json");
        }
        
        public async Task InitializeAsync()
        {
            // Load existing backup jobs if available
            if (File.Exists(_configFilePath))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(_configFilePath);
                    var backupJobs = JsonSerializer.Deserialize<List<BackupJob>>(json);
                    
                    if (backupJobs != null)
                    {
                        foreach (var job in backupJobs)
                        {
                            _backupManager.AddBackupJob(job);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(LanguageManager.GetString("ErrorLoadingConfig") + ex.Message);
                }
            }
        }
        
        public async Task SaveConfigurationAsync()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_backupManager.GetAllJobs(), options);
                await File.WriteAllTextAsync(_configFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine(LanguageManager.GetString("ErrorSavingConfig") + ex.Message);
            }
        }
        
        public async Task ProcessCommandLineArgs(string[] args)
        {
            // Process command line arguments
            if (args.Length > 0)
            {
                string jobsToRun = args[0];
                
                if (jobsToRun.Contains('-'))
                {
                    // Range format (e.g. "1-3")
                    string[] range = jobsToRun.Split('-');
                    if (range.Length == 2 && int.TryParse(range[0], out int start) && int.TryParse(range[1], out int end))
                    {
                        await RunJobsInRangeAsync(start, end);
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
                    
                    await RunSpecificJobsAsync(indices);
                }
                else if (int.TryParse(jobsToRun, out int singleJob))
                {
                    // Single job
                    await RunSingleJobAsync(singleJob);
                }
            }
        }
        
        private async Task RunJobsInRangeAsync(int start, int end)
        {
            for (int i = start; i <= end; i++)
            {
                await RunSingleJobAsync(i);
            }
        }
        
        private async Task RunSpecificJobsAsync(List<int> indices)
        {
            foreach (int index in indices)
            {
                await RunSingleJobAsync(index);
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
            await SaveConfigurationAsync();
            
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
            
            await SaveConfigurationAsync();
            
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
            await SaveConfigurationAsync();
            
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
                    await RunJobsInRangeAsync(1, _backupManager.GetAllJobs().Count);
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
    }
}