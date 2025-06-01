using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using EasySave.Models; // For BackupJob
using EasySave.Utils;   // For LanguageManager (for error messages)

namespace EasySave.Config
{
    public static class ConfigManager
    {
        private static readonly string AppDataDir;
        private static readonly string JobsConfigFilePath;
        private static readonly string AppSettingsFilePath;

        static ConfigManager()
        {
            AppDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave");

            if (!Directory.Exists(AppDataDir))
            {
                Directory.CreateDirectory(AppDataDir);
            }

            JobsConfigFilePath = Path.Combine(AppDataDir, "backup_jobs.json");
            AppSettingsFilePath = Path.Combine(AppDataDir, "app_settings.json");
        }

        public static async Task<List<BackupJob>> LoadJobsAsync()
        {
            if (File.Exists(JobsConfigFilePath))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(JobsConfigFilePath);
                    var backupJobs = JsonSerializer.Deserialize<List<BackupJob>>(json);
                    return backupJobs ?? new List<BackupJob>();
                }
                catch (Exception ex)
                {
                    // In a real app, use a proper logging mechanism or ViewModel feedback
                    Console.WriteLine($"Error loading backup jobs configuration: {ex.Message}");
                    return new List<BackupJob>();
                }
            }
            return new List<BackupJob>();
        }

        public static async Task SaveJobsAsync(List<BackupJob> jobs)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(jobs, options);
                await File.WriteAllTextAsync(JobsConfigFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving backup jobs configuration: {ex.Message}");
            }
        }

        public static async Task<AppSettingsData> LoadAppSettingsAsync()
        {
            if (File.Exists(AppSettingsFilePath))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(AppSettingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettingsData>(json);
                    return settings ?? new AppSettingsData();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading application settings: {ex.Message}");
                    return new AppSettingsData();
                }
            }
            return new AppSettingsData();
        }

        public static async Task SaveAppSettingsAsync(AppSettingsData settings)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(settings, options);
                await File.WriteAllTextAsync(AppSettingsFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving application settings: {ex.Message}");
            }
        }
    }
}