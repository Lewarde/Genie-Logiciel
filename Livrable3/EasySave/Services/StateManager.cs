using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using EasySave.Models;

namespace EasySave.Services
{
    public class StateManager
    {
        private readonly string _stateDirectory;
        private readonly string _allStatesFile;

        // 🔐 Verrou statique partagé par toutes les instances
        private static readonly object _stateFileLock = new object();

        public StateManager()
        {
            _stateDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EasySave", "States");
            if (!Directory.Exists(_stateDirectory))
                Directory.CreateDirectory(_stateDirectory);

            _allStatesFile = Path.Combine(_stateDirectory, "state_all.json");
        }

        public async Task UpdateStateAsync(BackupProgress progress)
        {
            progress.Timestamp = DateTime.Now;

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

            var options = new JsonSerializerOptions { WriteIndented = true };

            string singleStatePath = Path.Combine(_stateDirectory, $"{progress.JobName}_state.json");

            lock (_stateFileLock)
            {
                // 🔒 Bloc critique : une seule tâche y entre à la fois
                File.WriteAllText(singleStatePath, JsonSerializer.Serialize(state, options));

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

                File.WriteAllText(_allStatesFile, JsonSerializer.Serialize(allStates, options));
            }
        }
    }
}