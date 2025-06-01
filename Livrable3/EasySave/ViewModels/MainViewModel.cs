using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using EasySave.Config;
using EasySave.Models;
using EasySave.Utils;
using Logger;
using EasySave.Wpf.Views;
using EasySave.Commands;
using System.IO;
using System.Diagnostics;
using System.Net.Sockets;
using EasySave.Services;

namespace EasySave.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private SocketService _socketService;
        private EasySave.BackupManager.BackupManager _backupManagerService; // Use the correct BackupManager
        private ObservableCollection<BackupJobViewModel> _backupJobs;
        private BackupJobViewModel _selectedBackupJob;
        private AppSettingsData _appSettings;

        private static Socket clientSocket;
        private string _currentLanguage;
        private string _currentLogFormat;
        private bool _isExecutingAnyJob; // Renamed for clarity
        private string _globalStatusMessage;

        public ICommand StartAllCommand { get; }

        public ObservableCollection<BackupJobViewModel> BackupJobs
        {
            get => _backupJobs;
            set => SetProperty(ref _backupJobs, value);
        }

        public BackupJobViewModel SelectedBackupJob
        {
            get => _selectedBackupJob;
            set => SetProperty(ref _selectedBackupJob, value);
        }

        public List<string> AvailableLanguages { get; } = new List<string> { "en", "fr" };
        public List<string> AvailableLogFormats { get; } = new List<string> { "JSON", "XML" };

        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (SetProperty(ref _currentLanguage, value))
                {
                    LanguageManager.SetLanguage(value);
                    if (_appSettings != null)
                    {
                        _appSettings.Language = value;
                        ConfigManager.SaveAppSettingsAsync(_appSettings);
                    }
                    // OnPropertyChanged(nameof(AddButtonText)); // Assuming AddButtonText is localized
                    LocalizeDynamicUIText();
                }
            }
        }

        private void LocalizeDynamicUIText()
        {

        }

        public string CurrentLogFormat
        {
            get => _currentLogFormat;
            set
            {
                if (SetProperty(ref _currentLogFormat, value))
                {
                    LogManager.Initialize(value); // Re-initialize LogManager with new format
                    if (_appSettings != null)
                    {
                        _appSettings.LogFormat = value;
                        ConfigManager.SaveAppSettingsAsync(_appSettings);
                    }
                }
            }
        }

        public string BusinessSoftwareNameSetting
        {
            get => _appSettings?.BusinessSoftwareProcessName ?? string.Empty;
            set
            {
                if (_appSettings != null)
                {
                    if (EqualityComparer<string>.Default.Equals(_appSettings.BusinessSoftwareProcessName, value))
                        return;

                    _appSettings.BusinessSoftwareProcessName = value;
                    OnPropertyChanged(nameof(BusinessSoftwareNameSetting));
                    ConfigManager.SaveAppSettingsAsync(_appSettings);
                }
            }
        }

        public string GlobalStatusMessage
        {
            get => _globalStatusMessage;
            set => SetProperty(ref _globalStatusMessage, value);
        }

        public bool IsExecutingAnyJob // True if any job is in a state considered "executing" (Active, Paused)
        {
            get => _isExecutingAnyJob;
            private set // Setter should be private, updated internally
            {
                if (SetProperty(ref _isExecutingAnyJob, value))
                {
                    OnPropertyChanged(nameof(AreGlobalControlsEnabled));
                    // This will affect StartAllCommand's CanExecute
                    ((RelayCommand)StartAllCommand).RaiseCanExecuteChanged();
                    // And other global command buttons like Add, Edit, Delete job, Execute Selected/All (sequential)
                    // These might need their own RelayCommands or a shared CanExecute logic.
                }
            }
        }

        // Controls like Add, Edit, Delete, Execute All (Sequential), Language, Log Format, Business Software
        public bool AreGlobalControlsEnabled => !IsExecutingAnyJob;


        // public string AddButtonText => LanguageManager.GetString("CreateBackupJob"); // Example for localized button text

        public MainViewModel()
        {
            BackupJobs = new ObservableCollection<BackupJobViewModel>();
            StartAllCommand = new RelayCommand(async () => await StartAllBackupsInParallelAsync(), () => AreGlobalControlsEnabled && _backupManagerService != null);
            _socketService = new SocketService(8080); // Utiliser le port de votre choix
            try
            {
                _socketService.Start();
            }
            catch (Exception ex)
            {
                // Gérer l'échec du démarrage du serveur (par exemple, port déjà utilisé)
                GlobalStatusMessage = $"Error starting communication server: {ex.Message}";
                MessageBox.Show(GlobalStatusMessage, "Server Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void HandleUninitializedService(string operationAttemptKey)
        {
            string operationAttempt = LanguageManager.GetString(operationAttemptKey);
            string message = $"{operationAttempt}: {LanguageManager.GetString("ServiceNotInitialized")}";
            GlobalStatusMessage = message;
            MessageBox.Show(message, LanguageManager.GetString("ServiceErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public async Task InitializeAsync()
        {
            try
            {
                LanguageManager.Initialize(); // Initialize translations first
                _appSettings = await ConfigManager.LoadAppSettingsAsync();
                if (_appSettings == null) _appSettings = new AppSettingsData();

                CurrentLanguage = _appSettings.Language; // Set language, triggers PropertyChanged and LanguageManager.SetLanguage

                string logFormat = string.IsNullOrEmpty(_appSettings.LogFormat) ? "JSON" : _appSettings.LogFormat;
                // CurrentLogFormat setter will initialize LogManager
                CurrentLogFormat = logFormat; // Set log format, triggers PropertyChanged and LogManager.Initialize

                OnPropertyChanged(nameof(BusinessSoftwareNameSetting)); // Notify UI of initial BS name

                _backupManagerService = new EasySave.BackupManager.BackupManager(_appSettings);
                if (_backupManagerService.GetBackupExecutor() != null)
                {
                    _backupManagerService.GetBackupExecutor().ProgressUpdated += OnBackupProgressUpdated;
                }

                var jobModelsFromConfig = await ConfigManager.LoadJobsAsync();
                BackupJobs.Clear();
                foreach (var jobModel in jobModelsFromConfig)
                {
                    _backupManagerService.AddBackupJob(jobModel); // Add to service's internal list
                    var jobVm = new BackupJobViewModel(jobModel, _backupManagerService); // Pass service for commands
                    jobVm.ResetState(); // Set initial UI state
                    BackupJobs.Add(jobVm);
                }
                GlobalStatusMessage = LanguageManager.GetString("WelcomeMessage");
            }
            catch (Exception ex)
            {
                GlobalStatusMessage = $"{LanguageManager.GetString("InitializationError")}: {ex.Message}";
                MessageBox.Show(GlobalStatusMessage, LanguageManager.GetString("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                UpdateOverallExecutionState(); // Initial check
            }
        }

        private async void OnBackupProgressUpdated(BackupProgress progress)
        {
            // Ensure UI updates happen on the UI thread
            if (Application.Current?.Dispatcher.CheckAccess() == false)
            {
                Application.Current.Dispatcher.Invoke(() => UpdateJobAndOverallState(progress));
            }
            else
            {
                UpdateJobAndOverallState(progress);
            }

            var allJobsCurrentState = BackupJobs.Select(vm => new
            {
                Name = vm.Name,
                State = vm.IsPaused ? "PAUSED" : (vm.IsExecuting ? "ACTIVE" : vm.StatusMessage.ToUpperInvariant()),
                TotalFilesToCopy = vm.JobModel.Name == progress.JobName ? progress.TotalFilesCount : GetTotalFilesForJob(vm.Name),
                TotalFilesSize = vm.JobModel.Name == progress.JobName ? progress.TotalFilesSize : GetTotalSizeForJob(vm.Name),
                NbFilesLeftToDo = vm.JobModel.Name == progress.JobName ? progress.RemainingFilesCount : GetRemainingFilesForJob(vm.Name),
                Progression = vm.CurrentProgressPercentage,
                CurrentSourceFile = vm.JobModel.Name == progress.JobName ? progress.CurrentSourceFile : "",
                CurrentTargetFile = vm.JobModel.Name == progress.JobName ? progress.CurrentTargetFile : ""
            }).ToList();

            if (_socketService != null)
            {
                await _socketService.SendProgressToClientsAsync(allJobsCurrentState);
            }
        }


        private int GetTotalFilesForJob(string jobName)
        {

            var jobVm = BackupJobs.FirstOrDefault(j => j.Name == jobName);

            return 0;
        }
        private long GetTotalSizeForJob(string jobName)
        {
            return 0; 
        }
        private int GetRemainingFilesForJob(string jobName)
        {
            var jobVm = BackupJobs.FirstOrDefault(j => j.Name == jobName);

            return 0;
        }

        private void UpdateJobAndOverallState(BackupProgress progress)
        {
            var jobVm = BackupJobs.FirstOrDefault(j => j.JobModel.Name == progress.JobName);
            if (jobVm != null)
            {
                jobVm.UpdateProgress(progress);
            }
            else
            {
                Debug.WriteLine($"[MainViewModel] Progress update for unknown job: {progress.JobName}");
            }
            UpdateOverallExecutionState();
        }

        private void UpdateOverallExecutionState()
        {
            // Any job is active or paused
            IsExecutingAnyJob = BackupJobs.Any(j => j.IsExecuting);

            // Update global status message based on overall state
            if (!IsExecutingAnyJob)
            {
                // Check if any job ended in error or was stopped to set an appropriate final message
                if (BackupJobs.Any(j => j.StatusMessage == LanguageManager.GetString("StatusError") || j.StatusMessage == LanguageManager.GetString("StatusInterrupted")))
                {
                    GlobalStatusMessage = LanguageManager.GetString("AllJobsCompletedWithIssues");
                }

            }

        }


        private async Task<bool> CheckAndLogBusinessSoftwareAsync(string jobName)
        {
            string businessSoftwarePath = _appSettings?.BusinessSoftwareProcessName;
            if (string.IsNullOrWhiteSpace(businessSoftwarePath)) return false;

            if (ProcessUtils.IsProcessRunning(businessSoftwarePath))
            {
                string softwareDisplayName = Path.GetFileName(businessSoftwarePath);
                string message = string.Format(LanguageManager.GetString("BusinessSoftwarePreventingJob"), jobName, softwareDisplayName);
                GlobalStatusMessage = message; // Show in status bar
                MessageBox.Show(message, LanguageManager.GetString("OperationAborted"), MessageBoxButton.OK, MessageBoxImage.Warning);
                await LogManager.Instance.LogFileOperationAsync(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    JobName = jobName,
                    Message = $"{message} (Path: {businessSoftwarePath})" // Log with path for details
                });
                // Update specific job VM state to Interrupted
                var jobVm = BackupJobs.FirstOrDefault(j => j.Name == jobName);
                jobVm?.UpdateProgress(new BackupProgress { JobName = jobName, State = BackupState.Interrupted });
                UpdateOverallExecutionState();
                return true;
            }
            return false;
        }

        public async Task AddBackupJobAsync(Window owner)
        {
            if (_backupManagerService == null) { HandleUninitializedService("AddJobOperation"); return; }
            if (!AreGlobalControlsEnabled) return; // Prevent action if something is running

            var newJobModel = new BackupJob();
            var newJobVm = new BackupJobViewModel(newJobModel, _backupManagerService); // Pass service
            newJobVm.ResetState();
            var editWindow = new EditBackupJobWindow(newJobVm) { Owner = owner };

            if (editWindow.ShowDialog() == true)
            {
                _backupManagerService.AddBackupJob(newJobModel); // Add to service's list
                BackupJobs.Add(newJobVm); // Add to UI list
                await SaveJobsConfigurationAsync();
                GlobalStatusMessage = LanguageManager.GetString("JobCreatedSuccessfully");
            }
        }

        public async Task EditBackupJobAsync(Window owner)
        {
            if (_backupManagerService == null) { HandleUninitializedService("EditJobOperation"); return; }
            if (SelectedBackupJob == null)
            {
                MessageBox.Show(LanguageManager.GetString("InvalidJobIndex"), LanguageManager.GetString("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (!AreGlobalControlsEnabled) return;

            // Clone the model for editing, so changes are only applied on OK.
            var originalJobModel = SelectedBackupJob.JobModel;
            var jobModelCloneForEditing = new BackupJob
            {
                Name = originalJobModel.Name, // Name might be editable or not, depending on requirements
                SourceDirectory = originalJobModel.SourceDirectory,
                TargetDirectory = originalJobModel.TargetDirectory,
                FileExtension = originalJobModel.FileExtension,
                Priority = originalJobModel.Priority
            };
            // Use a temporary ViewModel for the dialog, passing the cloned model and service
            var tempViewModelForEditing = new BackupJobViewModel(jobModelCloneForEditing, _backupManagerService);
            var editWindow = new EditBackupJobWindow(tempViewModelForEditing) { Owner = owner };

            if (editWindow.ShowDialog() == true)
            {
                // Apply changes from the clone back to the original model
                originalJobModel.Name = tempViewModelForEditing.Name; // Update Name in model
                originalJobModel.SourceDirectory = tempViewModelForEditing.SourceDirectory;
                originalJobModel.TargetDirectory = tempViewModelForEditing.TargetDirectory;
                originalJobModel.FileExtension = tempViewModelForEditing.FileExtension;
                originalJobModel.Priority = tempViewModelForEditing.Priority;

                // Update the existing ViewModel in the list to reflect model changes
                SelectedBackupJob.UpdateModel(originalJobModel);
                await SaveJobsConfigurationAsync();
                GlobalStatusMessage = LanguageManager.GetString("JobModifiedSuccessfully");
            }
        }

        public async Task DeleteBackupJobAsync()
        {
            if (_backupManagerService == null) { HandleUninitializedService("DeleteJobOperation"); return; }
            if (SelectedBackupJob == null)
            {
                MessageBox.Show(LanguageManager.GetString("InvalidJobIndex"), LanguageManager.GetString("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (!AreGlobalControlsEnabled) return;

            var result = MessageBox.Show(
                string.Format(LanguageManager.GetString("ConfirmDeleteJob"), SelectedBackupJob.Name),
                LanguageManager.GetString("Confirmation"), MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _backupManagerService.RemoveBackupJob(SelectedBackupJob.JobModel); // Remove from service
                BackupJobs.Remove(SelectedBackupJob); // Remove from UI
                SelectedBackupJob = null; // Clear selection
                await SaveJobsConfigurationAsync();
                GlobalStatusMessage = LanguageManager.GetString("JobDeletedSuccessfully");
            }
        }

        public async Task ExecuteSelectedJobAsync()
        {
            if (_backupManagerService == null) { HandleUninitializedService("ExecuteSingleJobOperation"); return; }
            if (SelectedBackupJob == null)
            {
                MessageBox.Show(LanguageManager.GetString("InvalidJobIndex"), LanguageManager.GetString("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (SelectedBackupJob.IsExecuting) // Already running (active or paused)
            {
                MessageBox.Show(LanguageManager.GetString("JobAlreadyRunning"), LanguageManager.GetString("Info"), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (await CheckAndLogBusinessSoftwareAsync(SelectedBackupJob.Name)) return; // BS check

            SelectedBackupJob.ResetState(); // Prepare VM for execution
            // IsExecutingAnyJob will be true once BackupExecutor updates state to Active via ProgressUpdated
            UpdateOverallExecutionState();


            try
            {
                GlobalStatusMessage = $"{LanguageManager.GetString("ExecutingJob")} {SelectedBackupJob.Name}...";
                await _backupManagerService.ExecuteBackupJobAsync(SelectedBackupJob.JobModel);
                // Final state (Completed, Error, Stopped) is set by BackupExecutor via ProgressUpdated
            }
            catch (BusinessSoftwareInterruptionException bsie) // Already handled by CheckAndLog or by BackupExecutor
            {
                GlobalStatusMessage = string.Format(LanguageManager.GetString("JobInterruptedWithMessage"), SelectedBackupJob.Name, bsie.Message);
                // No need for MessageBox here if already shown by CheckAndLog or if state update is enough
            }
            catch (Exception ex) // Catch other exceptions from ExecuteBackupJobAsync itself
            {
                GlobalStatusMessage = $"{LanguageManager.GetString("BackupError")} {SelectedBackupJob.Name}: {ex.Message}";
                MessageBox.Show(GlobalStatusMessage, LanguageManager.GetString("ExecutionErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                // Ensure VM state reflects error if not already done by ProgressUpdated
                SelectedBackupJob.UpdateProgress(new BackupProgress { JobName = SelectedBackupJob.Name, State = BackupState.Error });
            }
            finally
            {
                UpdateOverallExecutionState(); // Re-evaluate global state
            }
        }

        public async Task ExecuteAllJobsAsync() // Sequential execution
        {
            if (_backupManagerService == null) { HandleUninitializedService("ExecuteAllJobsOperation"); return; }
            if (!AreGlobalControlsEnabled) return;


            GlobalStatusMessage = LanguageManager.GetString("StartingAllJobs");
            bool anErrorOccurredOverall = false;
            string overallErrorMessageAccumulator = string.Empty;
            bool cancelledByUserByBusinessSoftware = false;

            // Reset all jobs before starting
            foreach (var jobVm in BackupJobs) { jobVm.ResetState(); }
            UpdateOverallExecutionState(); // IsExecutingAnyJob will become true as jobs start

            try
            {
                foreach (var jobVm in BackupJobs.ToList()) // ToList for safe iteration if collection could change (not expected here)
                {
                    if (jobVm.IsExecuting) continue; // Skip if somehow already running

                    if (await CheckAndLogBusinessSoftwareAsync(jobVm.Name))
                    {
                        string softwareDisplayName = Path.GetFileName(_appSettings?.BusinessSoftwareProcessName ?? "Business Software");
                        string message = string.Format(LanguageManager.GetString("JobSkippedBusinessSoftware"), jobVm.Name, softwareDisplayName);
                        overallErrorMessageAccumulator += message + "\n";
                        anErrorOccurredOverall = true; // Consider this an issue for the overall run
                        // jobVm state already updated to Interrupted by CheckAndLogBusinessSoftwareAsync

                        var userChoice = MessageBox.Show(
                            $"{message}\n\n{LanguageManager.GetString("ContinueWithOtherJobs")}",
                            LanguageManager.GetString("BusinessSoftwareDetected"), MessageBoxButton.YesNo, MessageBoxImage.Warning);
                        if (userChoice == MessageBoxResult.No)
                        {
                            cancelledByUserByBusinessSoftware = true;
                            break;
                        }
                        continue; // Skip this job and continue with the next
                    }

                    GlobalStatusMessage = $"{LanguageManager.GetString("ExecutingJob")} {jobVm.Name}...";
                    try
                    {
                        await _backupManagerService.ExecuteBackupJobAsync(jobVm.JobModel);
                        // Final state will be set by ProgressUpdated
                    }
                    catch (BusinessSoftwareInterruptionException bsie) // Should be caught by BackupExecutor first
                    {
                        anErrorOccurredOverall = true;
                        string msg = string.Format(LanguageManager.GetString("JobInterruptedWithMessage"), jobVm.Name, bsie.Message);
                        overallErrorMessageAccumulator += msg + "\n";
                        // No need for MessageBox here, BackupExecutor handles state. User already asked about BS.
                    }
                    catch (Exception ex) // Other errors during this specific job's execution
                    {
                        anErrorOccurredOverall = true;
                        string msg = $"{LanguageManager.GetString("BackupError")} {jobVm.Name}: {ex.Message}";
                        overallErrorMessageAccumulator += msg + "\n";
                        jobVm.UpdateProgress(new BackupProgress { JobName = jobVm.Name, State = BackupState.Error }); // Ensure UI reflects error

                        var result = MessageBox.Show( // Ask user if they want to continue after an error
                            $"{msg}\n\n{LanguageManager.GetString("ContinueWithOtherJobs")}",
                            LanguageManager.GetString("ExecutionErrorTitle"), MessageBoxButton.YesNo, MessageBoxImage.Error);
                        if (result == MessageBoxResult.No) { cancelledByUserByBusinessSoftware = true; break; } // User chose to stop all
                    }

                    // If a job was paused and then resumed, and then the "ExecuteAll" continues, this is fine.
                    // If a job was STOPPED by the user, ExecuteBackupJobAsync would complete (with Stopped state).
                    // The loop here would just move to the next job.
                }

                // After all jobs attempted
                if (cancelledByUserByBusinessSoftware)
                {
                    GlobalStatusMessage = LanguageManager.GetString("AllJobsExecutionCancelled");
                }
                else if (anErrorOccurredOverall)
                {
                    GlobalStatusMessage = LanguageManager.GetString("AllJobsCompletedWithIssues");
                    Debug.WriteLine("--- Issues during sequential all jobs execution ---");
                    Debug.WriteLine(overallErrorMessageAccumulator);
                    Debug.WriteLine("--------------------------------------------------");
                }
                else
                {
                    GlobalStatusMessage = LanguageManager.GetString("AllJobsCompletedSuccessfully");
                }
            }
            catch (Exception ex) // Catch-all for unexpected errors in the loop itself
            {
                GlobalStatusMessage = $"{LanguageManager.GetString("GenericErrorDuringAllJobs")} {ex.Message}";
                MessageBox.Show(GlobalStatusMessage, LanguageManager.GetString("ExecutionErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                UpdateOverallExecutionState(); // Final update to global execution state and button enables
                                               // Ensure any jobs that were "Executing" but not completed (e.g. due to cancellation of loop) are reset or marked appropriately
                foreach (var jobVm in BackupJobs)
                {
                    if (jobVm.IsExecuting && (jobVm.StatusMessage.Contains("Actif") || jobVm.StatusMessage.Contains("Active")))
                    {
                        jobVm.ResetState(); // Or set to a specific "Cancelled by User" state
                    }
                }
            }
        }

        private async Task StartAllBackupsInParallelAsync()
        {
            if (_backupManagerService == null) { HandleUninitializedService("StartAllBackupsOperation"); return; }
            if (!AreGlobalControlsEnabled) return;

            GlobalStatusMessage = LanguageManager.GetString("StartingAllJobs");
            foreach (var jobVm in BackupJobs) { jobVm.ResetState(); } // Reset before starting
            UpdateOverallExecutionState(); // IsExecutingAnyJob will become true

            // Business Software Check for all jobs before starting any in parallel
            bool businessSoftwareRunning = false;
            foreach (var jobVm in BackupJobs)
            {
                if (await CheckAndLogBusinessSoftwareAsync(jobVm.Name))
                {
                    businessSoftwareRunning = true; // Mark that BS is running for at least one job.
                    // jobVm state is updated to Interrupted by CheckAndLog.
                }
            }

            if (businessSoftwareRunning)
            {
                // Ask ONCE if user wants to proceed with non-blocked jobs, or cancel all.
                var userChoice = MessageBox.Show(
                    $"{LanguageManager.GetString("BusinessSoftwareDetectedForSome")}\n\n{LanguageManager.GetString("ContinueWithNonBlockedJobs")}",
                    LanguageManager.GetString("BusinessSoftwareDetected"), MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (userChoice == MessageBoxResult.No)
                {
                    GlobalStatusMessage = LanguageManager.GetString("AllJobsExecutionCancelled");
                    UpdateOverallExecutionState();
                    return; // User cancelled all parallel execution
                }
                // If Yes, proceed to run only jobs not marked as Interrupted.
            }


            try
            {
                // Filter out jobs that were marked as Interrupted by the BS check
                var jobsToRun = BackupJobs.Where(vm => vm.JobModel.Name != null &&
                                              _backupManagerService.GetAllJobs().Any(j => j.Name == vm.JobModel.Name) &&
                                              vm.StatusMessage != LanguageManager.GetString("StatusInterrupted")) // Check StatusMessage for Interrupted state
                                     .Select(vm => vm.JobModel)
                                     .ToList();

                _backupManagerService.StartAllJobsInParallel(jobsToRun); // Modified to take a list
                await _backupManagerService.WaitForAllJobsAsync(); // Wait for all these started tasks
                _backupManagerService.ClearFinishedJobs(); // Clean up tracking

                // Final status determination after all parallel jobs are done
                bool anyJobHadIssues = BackupJobs.Any(vm =>
                    vm.StatusMessage == LanguageManager.GetString("StatusError") ||
                    vm.StatusMessage == LanguageManager.GetString("StatusInterrupted")); // Check final states

                if (anyJobHadIssues)
                {
                    GlobalStatusMessage = LanguageManager.GetString("AllJobsCompletedWithIssues");
                }
                else if (BackupJobs.All(vm => vm.StatusMessage == LanguageManager.GetString("StatusCompleted") ||
                                             (!vm.IsExecuting && vm.StatusMessage == LanguageManager.GetString("StatusReady")))) // If some were not run (e.g. filtered out)
                {
                    GlobalStatusMessage = LanguageManager.GetString("AllJobsCompletedSuccessfully");
                }
                else
                {
                    // Catch-all for mixed states or unexpected outcomes
                    GlobalStatusMessage = LanguageManager.GetString("AllOperationsFinished");
                }

            }
            catch (Exception ex)
            {
                GlobalStatusMessage = $"{LanguageManager.GetString("ErrorDuringAllJobsExecution")}: {ex.Message}";
                MessageBox.Show(GlobalStatusMessage, LanguageManager.GetString("ExecutionErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                UpdateOverallExecutionState(); // Final state update
            }
        }
        private void OnApplicationExit(object sender, ExitEventArgs e)
        {
            _socketService?.Stop();
        }


        private async Task SaveJobsConfigurationAsync()
        {
            if (_backupManagerService == null)
            {
                System.Diagnostics.Debug.WriteLine("SaveJobsConfigurationAsync: _backupManagerService is null. Configuration not saved.");
                return;
            }
            // Ensure we are saving the list of jobs known by the BackupManagerService
            await ConfigManager.SaveJobsAsync(_backupManagerService.GetAllJobs().ToList());
        }
    }
}