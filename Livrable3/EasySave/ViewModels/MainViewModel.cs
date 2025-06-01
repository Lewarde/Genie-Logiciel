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
using System.Net.Sockets; // For network communication (sockets)
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

        private static Socket clientSocket; // This field seems unused in the current context.
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
                    LanguageManager.SetLanguage(value); // Update the application's language
                    if (_appSettings != null)
                    {
                        _appSettings.Language = value; // Save the new language to settings
                        ConfigManager.SaveAppSettingsAsync(_appSettings);
                    }
                    LocalizeDynamicUIText();
                }
            }
        }

        // Placeholder for updating UI elements that need manual text refresh after language change.
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
                        _appSettings.LogFormat = value; // Save the new log format to settings
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

                }
            }
        }

        // Controls like Add, Edit, Delete, Execute All (Sequential), Language, Log Format, Business Software
        public bool AreGlobalControlsEnabled => !IsExecutingAnyJob;


        // public string AddButtonText => LanguageManager.GetString("CreateBackupJob"); // Example for localized button text

        public MainViewModel()
        {
            BackupJobs = new ObservableCollection<BackupJobViewModel>();
            // Command to start all backups, can only execute if global controls are enabled and service is up
            StartAllCommand = new RelayCommand(async () => await StartAllBackupsInParallelAsync(), () => AreGlobalControlsEnabled && _backupManagerService != null);
            _socketService = new SocketService(8080); // Use any port you prefer
            try
            {
                _socketService.Start(); // Try to start the communication server
            }
            catch (Exception ex)
            {
                // Handle server start failure (e.g., port already in use)
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
                if (_appSettings == null) _appSettings = new AppSettingsData(); // If no settings, create default

                CurrentLanguage = _appSettings.Language; // Set language, triggers PropertyChanged and LanguageManager.SetLanguage

                string logFormat = string.IsNullOrEmpty(_appSettings.LogFormat) ? "JSON" : _appSettings.LogFormat;
                // CurrentLogFormat setter will initialize LogManager
                CurrentLogFormat = logFormat; // Set log format, triggers PropertyChanged and LogManager.Initialize

                OnPropertyChanged(nameof(BusinessSoftwareNameSetting)); // Notify UI of initial BS name

                _backupManagerService = new EasySave.BackupManager.BackupManager(_appSettings);
                if (_backupManagerService.GetBackupExecutor() != null)
                {
                    // Subscribe to progress updates from the backup process
                    _backupManagerService.GetBackupExecutor().ProgressUpdated += OnBackupProgressUpdated;
                }

                var jobModelsFromConfig = await ConfigManager.LoadJobsAsync(); // Load saved backup jobs
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
                UpdateOverallExecutionState(); // Initial check of execution state
            }
        }

        private async void OnBackupProgressUpdated(BackupProgress progress)
        {
            // Ensure UI updates happen on the UI thread
            if (Application.Current?.Dispatcher.CheckAccess() == false) // Check if we are on the UI thread
            {
                // If not, switch to UI thread to update UI elements
                Application.Current.Dispatcher.Invoke(() => UpdateJobAndOverallState(progress));
            }
            else
            {
                UpdateJobAndOverallState(progress);
            }

            // Prepare data to send over socket
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
                // Send current progress of all jobs to any connected clients
                await _socketService.SendProgressToClientsAsync(allJobsCurrentState);
            }
        }

        // Placeholder: Implement logic to get actual value.
        private int GetTotalFilesForJob(string jobName)
        {
            var jobVm = BackupJobs.FirstOrDefault(j => j.Name == jobName);
            // TODO: This should ideally fetch pre-calculated or live data for jobs not currently emitting progress.
            return 0;
        }
        // Placeholder: Implement logic to get actual value.
        private long GetTotalSizeForJob(string jobName)
        {
            // TODO: Fetch pre-calculated or live data.
            return 0;
        }
        // Placeholder: Implement logic to get actual value.
        private int GetRemainingFilesForJob(string jobName)
        {
            var jobVm = BackupJobs.FirstOrDefault(j => j.Name == jobName);
            // TODO: Fetch pre-calculated or live data.
            return 0;
        }

        private void UpdateJobAndOverallState(BackupProgress progress)
        {
            var jobVm = BackupJobs.FirstOrDefault(j => j.JobModel.Name == progress.JobName);
            if (jobVm != null)
            {
                jobVm.UpdateProgress(progress); // Update the specific job's view model
            }
            else
            {
                Debug.WriteLine($"[MainViewModel] Progress update for unknown job: {progress.JobName}");
            }
            UpdateOverallExecutionState(); // Refresh the overall application state
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
                // If no jobs are executing and none had issues, it might mean they completed successfully or are ready.
                // More specific "all successful" message might be set by the methods calling this.
            }
        }

        // Checks if specified business software is running. If so, logs and alerts user.
        private async Task<bool> CheckAndLogBusinessSoftwareAsync(string jobName)
        {
            string businessSoftwarePath = _appSettings?.BusinessSoftwareProcessName;
            if (string.IsNullOrWhiteSpace(businessSoftwarePath)) return false; // No software configured to check

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
                UpdateOverallExecutionState(); // Refresh global state as a job is now interrupted
                return true; // Business software is running
            }
            return false; // Business software is not running
        }

        public async Task AddBackupJobAsync(Window owner)
        {
            if (_backupManagerService == null) { HandleUninitializedService("AddJobOperation"); return; }
            if (!AreGlobalControlsEnabled) return; // Prevent action if something is running

            var newJobModel = new BackupJob();
            var newJobVm = new BackupJobViewModel(newJobModel, _backupManagerService); // Pass service
            newJobVm.ResetState(); // Set initial UI state for the new job VM
            var editWindow = new EditBackupJobWindow(newJobVm) { Owner = owner }; // Show dialog to edit job details

            if (editWindow.ShowDialog() == true) // If user confirms in dialog
            {
                _backupManagerService.AddBackupJob(newJobModel); // Add to service's list
                BackupJobs.Add(newJobVm); // Add to UI list
                await SaveJobsConfigurationAsync(); // Save the new job to config file
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
            if (!AreGlobalControlsEnabled) return; // Prevent action if something is running

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

            if (editWindow.ShowDialog() == true) // If user confirms changes
            {
                // Apply changes from the clone back to the original model
                originalJobModel.Name = tempViewModelForEditing.Name; // Update Name in model
                originalJobModel.SourceDirectory = tempViewModelForEditing.SourceDirectory;
                originalJobModel.TargetDirectory = tempViewModelForEditing.TargetDirectory;
                originalJobModel.FileExtension = tempViewModelForEditing.FileExtension;
                originalJobModel.Priority = tempViewModelForEditing.Priority;

                // Update the existing ViewModel in the list to reflect model changes
                SelectedBackupJob.UpdateModel(originalJobModel);
                await SaveJobsConfigurationAsync(); // Save changes to config file
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
            if (!AreGlobalControlsEnabled) return; // Prevent action if something is running

            var result = MessageBox.Show(
                string.Format(LanguageManager.GetString("ConfirmDeleteJob"), SelectedBackupJob.Name),
                LanguageManager.GetString("Confirmation"), MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes) // If user confirms deletion
            {
                _backupManagerService.RemoveBackupJob(SelectedBackupJob.JobModel); // Remove from service
                BackupJobs.Remove(SelectedBackupJob); // Remove from UI
                SelectedBackupJob = null; // Clear selection
                await SaveJobsConfigurationAsync(); // Update config file
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

            if (await CheckAndLogBusinessSoftwareAsync(SelectedBackupJob.Name)) return; // BS check, aborts if software is running

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

        public async Task ExecuteAllJobsAsync() // Sequential execution: one job after another
        {
            if (_backupManagerService == null) { HandleUninitializedService("ExecuteAllJobsOperation"); return; }
            if (!AreGlobalControlsEnabled) return; // Don't start if already busy


            GlobalStatusMessage = LanguageManager.GetString("StartingAllJobs");
            bool anErrorOccurredOverall = false;
            string overallErrorMessageAccumulator = string.Empty;
            bool cancelledByUserByBusinessSoftware = false;

            // Reset all jobs before starting
            foreach (var jobVm in BackupJobs) { jobVm.ResetState(); }
            UpdateOverallExecutionState(); // IsExecutingAnyJob will become true as jobs start

            try
            {
                // ToList for safe iteration if collection could change (not expected here, but good practice)
                foreach (var jobVm in BackupJobs.ToList())
                {
                    if (jobVm.IsExecuting) continue; // Skip if somehow already running (e.g., manually started during this loop - unlikely)

                    if (await CheckAndLogBusinessSoftwareAsync(jobVm.Name)) // Check for business software before each job
                    {
                        string softwareDisplayName = Path.GetFileName(_appSettings?.BusinessSoftwareProcessName ?? "Business Software");
                        string message = string.Format(LanguageManager.GetString("JobSkippedBusinessSoftware"), jobVm.Name, softwareDisplayName);
                        overallErrorMessageAccumulator += message + "\n";
                        anErrorOccurredOverall = true; // Consider this an issue for the overall run
                        // jobVm state already updated to Interrupted by CheckAndLogBusinessSoftwareAsync

                        var userChoice = MessageBox.Show(
                            $"{message}\n\n{LanguageManager.GetString("ContinueWithOtherJobs")}",
                            LanguageManager.GetString("BusinessSoftwareDetected"), MessageBoxButton.YesNo, MessageBoxImage.Warning);
                        if (userChoice == MessageBoxResult.No) // User chose to stop all further jobs
                        {
                            cancelledByUserByBusinessSoftware = true;
                            break; // Exit the loop
                        }
                        continue; // Skip this job and continue with the next
                    }

                    GlobalStatusMessage = $"{LanguageManager.GetString("ExecutingJob")} {jobVm.Name}...";
                    try
                    {
                        await _backupManagerService.ExecuteBackupJobAsync(jobVm.JobModel);
                        // Final state will be set by ProgressUpdated events from BackupExecutor
                    }
                    catch (BusinessSoftwareInterruptionException bsie) // Should be caught by BackupExecutor first and state updated
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
                    // If a job was still marked as actively running (e.g. user cancelled loop before it finished)
                    if (jobVm.IsExecuting && (jobVm.StatusMessage.Contains("Actif") || jobVm.StatusMessage.Contains("Active")))
                    {
                        jobVm.ResetState(); // Or set to a specific "Cancelled by User" state
                    }
                }
            }
        }

        private async Task StartAllBackupsInParallelAsync() // Parallel execution: all jobs start around the same time
        {
            if (_backupManagerService == null) { HandleUninitializedService("StartAllBackupsOperation"); return; }
            if (!AreGlobalControlsEnabled) return; // Don't start if already busy

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

            if (businessSoftwareRunning) // If any job is blocked by business software
            {
                // Ask ONCE if user wants to proceed with non-blocked jobs, or cancel all.
                var userChoice = MessageBox.Show(
                    $"{LanguageManager.GetString("BusinessSoftwareDetectedForSome")}\n\n{LanguageManager.GetString("ContinueWithNonBlockedJobs")}",
                    LanguageManager.GetString("BusinessSoftwareDetected"), MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (userChoice == MessageBoxResult.No) // User chose not to proceed
                {
                    GlobalStatusMessage = LanguageManager.GetString("AllJobsExecutionCancelled");
                    UpdateOverallExecutionState(); // Update state, no jobs will run
                    return; // User cancelled all parallel execution
                }
                // If Yes, proceed to run only jobs not marked as Interrupted.
            }


            try
            {
                // Filter out jobs that were marked as Interrupted by the BS check
                var jobsToRun = BackupJobs.Where(vm => vm.JobModel.Name != null && // Ensure job has a name
                                              _backupManagerService.GetAllJobs().Any(j => j.Name == vm.JobModel.Name) && // Ensure job is known to service
                                              vm.StatusMessage != LanguageManager.GetString("StatusInterrupted")) // Check StatusMessage for Interrupted state
                                     .Select(vm => vm.JobModel)
                                     .ToList();

                _backupManagerService.StartAllJobsInParallel(jobsToRun); // Modified to take a list of jobs to run
                await _backupManagerService.WaitForAllJobsAsync(); // Wait for all these started tasks to complete
                _backupManagerService.ClearFinishedJobs(); // Clean up tracking of finished jobs in the manager

                // Final status determination after all parallel jobs are done
                bool anyJobHadIssues = BackupJobs.Any(vm =>
                    vm.StatusMessage == LanguageManager.GetString("StatusError") ||
                    vm.StatusMessage == LanguageManager.GetString("StatusInterrupted")); // Check final states

                if (anyJobHadIssues)
                {
                    GlobalStatusMessage = LanguageManager.GetString("AllJobsCompletedWithIssues");
                }
                // Check if all jobs that were meant to run are completed, or if some were not run (e.g. filtered out by BS check and stayed "Ready")
                else if (BackupJobs.All(vm => vm.StatusMessage == LanguageManager.GetString("StatusCompleted") ||
                                             (!vm.IsExecuting && vm.StatusMessage == LanguageManager.GetString("StatusReady"))))
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
            _socketService?.Stop(); // Clean up socket service when application closes
        }


        private async Task SaveJobsConfigurationAsync()
        {
            if (_backupManagerService == null)
            {
                // Log this, as it's an issue if we try to save without the service
                System.Diagnostics.Debug.WriteLine("SaveJobsConfigurationAsync: _backupManagerService is null. Configuration not saved.");
                return;
            }
            // Ensure we are saving the list of jobs known by the BackupManagerService
            await ConfigManager.SaveJobsAsync(_backupManagerService.GetAllJobs().ToList());
        }
    }
}