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

            // Format de l’état
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

            // Sauvegarder l’état individuel
            string singleStatePath = Path.Combine(_stateDirectory, $"{progress.JobName}_state.json");
            await File.WriteAllTextAsync(singleStatePath, JsonSerializer.Serialize(state, options));

            // Générer l’ensemble des états dans un fichier global
            List<object> allStates = new();
            foreach (string file in Directory.GetFiles(_stateDirectory, "*_state.json"))
            {
                string content = await File.ReadAllTextAsync(file);
                try
                {
                    var parsed = JsonSerializer.Deserialize<object>(content);
                    if (parsed != null)
                        allStates.Add(parsed);
                }
                catch { }
            }

            await File.WriteAllTextAsync(_allStatesFile, JsonSerializer.Serialize(allStates, options));
        }
    }
}
