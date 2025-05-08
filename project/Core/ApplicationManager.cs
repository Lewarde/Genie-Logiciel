using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using EasySave.Models;
using EasySave.Services;
using EasySave.Utils;

namespace EasySave.Core
{
    /// <summary>
    /// Main application manager that coordinates between different components
    /// </summary>
    public class ApplicationManager
    {
        private readonly IBackupManager _backupManager;
        private readonly IMenuManager _menuManager;
        private readonly ICommandParser _commandParser;
        private readonly string _configFilePath;

        public ApplicationManager()
        {
            // Initialize backup manager
            _backupManager = new EasySave.BackupManager.BackupManager(); // Ensure BackupManager is a class, not just a namespace

            // Initialize menu manager and command parser with references to necessary components
            _menuManager = new MenuManager(_backupManager, SaveConfigurationAsync);
            _commandParser = new CommandParser(_backupManager);

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

        /// <summary>
        /// Initialize the application by loading configuration
        /// </summary>
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

        /// <summary>
        /// Save current configuration to file
        /// </summary>
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

        /// <summary>
        /// Process command line arguments
        /// </summary>
        public async Task ProcessCommandLineArgs(string[] args)
        {
            await _commandParser.ParseAndExecuteAsync(args, _backupManager.GetAllJobs());
        }

        /// <summary>
        /// Start the interactive menu interface
        /// </summary>
        public async Task StartInteractiveMenuAsync()
        {
            await _menuManager.StartInteractiveMenuAsync();
        }
    }
}