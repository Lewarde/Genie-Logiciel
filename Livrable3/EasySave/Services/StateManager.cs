using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using EasySave.Models;

namespace EasySave.Services
{
    // Manages the state of backup jobs, including updating and storing state information.
    public class StateManager
    {
        private readonly string _stateDirectory; // Directory where state files are stored.
        private readonly string _allStatesFile; // Path to the file that aggregates all states.

        private static readonly object _stateFileLock = new object(); // Lock object for thread-safe file operations.

        // Constructor initializes the state directory and file paths.
        public StateManager()
        {
            // Set the state directory path.
            _stateDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EasySave", "States");

            // Create the state directory if it doesn't exist.
            if (!Directory.Exists(_stateDirectory))
                Directory.CreateDirectory(_stateDirectory);

            // Set the path for the aggregated states file.
            _allStatesFile = Path.Combine(_stateDirectory, "state_all.json");
        }

        // Asynchronously updates the state of a backup job.
        public async Task UpdateStateAsync(BackupProgress progress)
        {
            // Update the timestamp to the current time.
            progress.Timestamp = DateTime.Now;

            // Create an anonymous object to represent the current state.
            var state = new
            {
                Name = progress.JobName,
                SourceFilePath = progress.CurrentSourceFile,
                TargetFilePath = progress.CurrentTargetFile,
                State = progress.State.ToString().ToUpper(),
                TotalFilesToCopy = progress.TotalFilesCount,
                TotalFilesSize = progress.TotalFilesSize,
                NbFilesLeftToDo = progress.RemainingFilesCount,
                Progression = progress.TotalFilesCount == 0 ? 0 :
                              (int)(((double)(progress.TotalFilesCount - progress.RemainingFilesCount) / progress.TotalFilesCount) * 100)
            };

            // Configure JSON serialization options.
            var options = new JsonSerializerOptions { WriteIndented = true };

            // Set the path for the individual state file.
            string singleStatePath = Path.Combine(_stateDirectory, $"{progress.JobName}_state.json");

            // Use a lock to ensure thread-safe file operations.
            lock (_stateFileLock)
            {
                // Critical section: only one task enters at a time.
                // Write the current state to an individual state file.
                File.WriteAllText(singleStatePath, JsonSerializer.Serialize(state, options));

                // Collect all states from individual state files.
                List<object> allStates = new();
                foreach (string file in Directory.GetFiles(_stateDirectory, "*_state.json"))
                {
                    try
                    {
                        string content = File.ReadAllText(file);
                        var parsed = JsonSerializer.Deserialize<object>(content);
                        if (parsed != null)
                            allStates.Add(parsed);
                    }
                    catch { /* Ignore files that can't be read or parsed */ }
                }

                // Write all collected states to the aggregated states file.
                File.WriteAllText(_allStatesFile, JsonSerializer.Serialize(allStates, options));
            }
        }
    }
}
