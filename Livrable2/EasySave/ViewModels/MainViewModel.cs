// MainViewModel.cs
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using EasySave.Config;
using EasySave.Models;
using EasySave.Utils; // For LanguageManager and ProcessUtils
using Logger;
using EasySave.BackupManager;
using EasySave.BackupExecutor; // For ProgressUpdated event args
using EasySave.Wpf.Views;
using System.IO; // For Path validation in CheckAndLogBusinessSoftwareAsync (optional)

namespace EasySave.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private EasySave.BackupManager.BackupManager _backupManagerService;
        private ObservableCollection<BackupJobViewModel> _backupJobs;
        private BackupJobViewModel _selectedBackupJob;
        private AppSettingsData _appSettings;

        private string _currentLanguage;
        private string _currentLogFormat;
        private string _statusMessage;
        private int _currentProgressPercentage;
        private bool _isExecutingBackup;

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
#pragma warning disable CS4014
                        ConfigManager.SaveAppSettingsAsync(_appSettings);
#pragma warning restore CS4014
                    }
                    OnPropertyChanged(nameof(AddButtonText));
                }
            }
        }

        public string CurrentLogFormat
        {
            get => _currentLogFormat;
            set
            {
                if (SetProperty(ref _currentLogFormat, value))
                {
                    LogManager.Initialize(value);
                    if (_appSettings != null)
                    {
                        _appSettings.LogFormat = value;
#pragma warning disable CS4014
                        ConfigManager.SaveAppSettingsAsync(_appSettings);
#pragma warning restore CS4014
                    }
                }
            }
        }

        public string BusinessSoftwareNameSetting // Renamed to BusinessSoftwarePathSetting for clarity
        {
            get => _appSettings?.BusinessSoftwareProcessName ?? string.Empty; // Property in AppSettingsData is still BusinessSoftwareProcessName
            set
            {
                if (_appSettings != null)
                {
                    if (EqualityComparer<string>.Default.Equals(_appSettings.BusinessSoftwareProcessName, value))
                        return;

                    _appSettings.BusinessSoftwareProcessName = value; // Storing the path here
                    OnPropertyChanged(nameof(BusinessSoftwareNameSetting));

#pragma warning disable CS4014
                    ConfigManager.SaveAppSettingsAsync(_appSettings);
#pragma warning restore CS4014
                }
            }
        }


        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public int CurrentProgressPercentage
        {
            get => _currentProgressPercentage;
            set => SetProperty(ref _currentProgressPercentage, value);
        }

        public bool IsExecutingBackup
        {
            get => _isExecutingBackup;
            set
            {
                if (SetProperty(ref _isExecutingBackup, value))
                {
                    OnPropertyChanged(nameof(AreControlsEnabled));
                }
            }
        }

        public bool AreControlsEnabled => !_isExecutingBackup;

        public string AddButtonText => LanguageManager.GetString("CreateBackupJob");

        public MainViewModel()
        {
            BackupJobs = new ObservableCollection<BackupJobViewModel>();
        }

        public async Task InitializeAsync()
        {
            LanguageManager.Initialize();
            _appSettings = await ConfigManager.LoadAppSettingsAsync();
            if (_appSettings == null) _appSettings = new AppSettingsData();

            LanguageManager.SetLanguage(_appSettings.Language);
            _currentLanguage = _appSettings.Language;
            OnPropertyChanged(nameof(CurrentLanguage));

            LogManager.Initialize(_appSettings.LogFormat);
            _currentLogFormat = _appSettings.LogFormat;
            OnPropertyChanged(nameof(CurrentLogFormat));
            OnPropertyChanged(nameof(BusinessSoftwareNameSetting));

            _backupManagerService = new EasySave.BackupManager.BackupManager(_appSettings);
            if (_backupManagerService.GetBackupExecutor() != null)
            {
                _backupManagerService.GetBackupExecutor().ProgressUpdated += OnBackupProgressUpdated;
            }

            var jobModelsFromConfig = await ConfigManager.LoadJobsAsync();
            BackupJobs.Clear();
            foreach (var jobModel in jobModelsFromConfig)
            {
                _backupManagerService.AddBackupJob(jobModel);
                BackupJobs.Add(new BackupJobViewModel(jobModel));
            }
            StatusMessage = LanguageManager.GetString("WelcomeMessage");
        }

        private void OnBackupProgressUpdated(BackupProgress progress)
        {
            if (System.Windows.Application.Current != null && !System.Windows.Application.Current.Dispatcher.CheckAccess())
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => UpdateProgressUI(progress));
            }
            else if (System.Windows.Application.Current != null)
            {
                UpdateProgressUI(progress);
            }
        }

        private void UpdateProgressUI(BackupProgress progress)
        {
            if (progress.State == BackupState.Interrupted)
            {
                StatusMessage = $"{LanguageManager.GetString("JobInterrupted")} {progress.JobName}: {LanguageManager.GetString("BusinessSoftwareDetected")}";
            }
            else if (progress.State == BackupState.Error)
            {
                StatusMessage = $"{progress.JobName}: {LanguageManager.GetString("BackupError")}";
            }
            else if (progress.State == BackupState.Completed)
            {
                StatusMessage = $"{progress.JobName}: {LanguageManager.GetString("BackupCompleted")}";
            }
            else if (progress.State == BackupState.Active)
            {
                StatusMessage = $"{LanguageManager.GetString("ExecutingJob")} {progress.JobName}: {progress.Progress}% - {progress.CurrentSourceFile ?? "Initializing..."}";
            }
            else if (progress.State == BackupState.Inactive && IsExecutingBackup)
            {
                StatusMessage = $"{progress.JobName}: {LanguageManager.GetString("BackupCompleted")}";
            }
            CurrentProgressPercentage = progress.Progress;
        }

        private async Task<bool> CheckAndLogBusinessSoftwareAsync(string jobName)
        {
            // The BusinessSoftwareProcessName in _appSettings now holds the full path
            string businessSoftwarePath = _appSettings?.BusinessSoftwareProcessName;

            if (string.IsNullOrWhiteSpace(businessSoftwarePath))
            {
                return false; // No business software path configured
            }


            if (ProcessUtils.IsProcessRunning(businessSoftwarePath))
            {
                // Extract file name for a slightly friendlier message, or use the full path.
                string softwareDisplayName = Path.GetFileName(businessSoftwarePath);
                string message = string.Format(LanguageManager.GetString("BusinessSoftwarePreventingJob"), jobName, softwareDisplayName);
                StatusMessage = message;
                System.Windows.MessageBox.Show(message, LanguageManager.GetString("OperationAborted"), MessageBoxButton.OK, MessageBoxImage.Warning);
                await LogManager.Instance.LogFileOperationAsync(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    JobName = jobName,
                    Message = $"{message} (Path: {businessSoftwarePath})" // Log with path for clarity
                });
                return true;
            }
            return false;
        }


        public async Task AddBackupJobAsync(Window owner)
        {
            var newJobModel = new BackupJob();
            var newJobVm = new BackupJobViewModel(newJobModel);

            var editWindow = new EditBackupJobWindow(newJobVm)
            {
                Owner = owner
            };

            bool? dialogResult = editWindow.ShowDialog();

            if (dialogResult == true)
            {
                _backupManagerService.AddBackupJob(newJobModel);
                BackupJobs.Add(newJobVm);
                await SaveJobsConfigurationAsync();
                StatusMessage = LanguageManager.GetString("JobCreatedSuccessfully");
            }
        }

        public async Task EditBackupJobAsync(Window owner)
        {
            if (SelectedBackupJob == null)
            {
                System.Windows.MessageBox.Show(
                    LanguageManager.GetString("InvalidJobIndex"),
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            var originalJobModel = SelectedBackupJob.JobModel;
            var jobModelCloneForEditing = new BackupJob
            {
                Name = originalJobModel.Name,
                SourceDirectory = originalJobModel.SourceDirectory,
                TargetDirectory = originalJobModel.TargetDirectory,
                Type = originalJobModel.Type,
                FileExtension = originalJobModel.FileExtension
            };
            var tempViewModelForEditing = new BackupJobViewModel(jobModelCloneForEditing);


            var editWindow = new EditBackupJobWindow(tempViewModelForEditing)
            {
                Owner = owner
            };
            bool? dialogResult = editWindow.ShowDialog();

            if (dialogResult == true)
            {
                originalJobModel.Name = tempViewModelForEditing.Name;
                originalJobModel.SourceDirectory = tempViewModelForEditing.SourceDirectory;
                originalJobModel.TargetDirectory = tempViewModelForEditing.TargetDirectory;
                originalJobModel.Type = tempViewModelForEditing.Type;
                originalJobModel.FileExtension = tempViewModelForEditing.FileExtension;

                SelectedBackupJob.UpdateModel(originalJobModel);

                await SaveJobsConfigurationAsync();
                StatusMessage = LanguageManager.GetString("JobModifiedSuccessfully");
            }
        }

        public async Task DeleteBackupJobAsync()
        {
            if (SelectedBackupJob == null)
            {
                System.Windows.MessageBox.Show(
                    LanguageManager.GetString("InvalidJobIndex"),
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            var result = System.Windows.MessageBox.Show(
                string.Format(LanguageManager.GetString("ConfirmDeleteJob"), SelectedBackupJob.Name),
                LanguageManager.GetString("Confirmation"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _backupManagerService.RemoveBackupJob(SelectedBackupJob.JobModel);
                BackupJobs.Remove(SelectedBackupJob);
                SelectedBackupJob = null;
                await SaveJobsConfigurationAsync();
                StatusMessage = LanguageManager.GetString("JobDeletedSuccessfully");
            }
        }

        public async Task ExecuteSelectedJobAsync()
        {
            if (SelectedBackupJob == null)
            {
                System.Windows.MessageBox.Show(LanguageManager.GetString("InvalidJobIndex"), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (await CheckAndLogBusinessSoftwareAsync(SelectedBackupJob.Name))
            {
                return;
            }

            IsExecutingBackup = true;
            CurrentProgressPercentage = 0;
            try
            {
                StatusMessage = $"{LanguageManager.GetString("ExecutingJob")} {SelectedBackupJob.Name}...";
                await _backupManagerService.ExecuteBackupJobAsync(SelectedBackupJob.JobModel);
            }
            catch (BusinessSoftwareInterruptionException bsie)
            {
                StatusMessage = $"{LanguageManager.GetString("JobInterrupted")} {SelectedBackupJob.Name}: {bsie.Message}";
                System.Windows.MessageBox.Show(StatusMessage, LanguageManager.GetString("OperationAborted"), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                StatusMessage = $"{LanguageManager.GetString("BackupError")} on job {SelectedBackupJob.Name}: {ex.Message}";
                System.Windows.MessageBox.Show(StatusMessage, "Execution Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsExecutingBackup = false;
                if (CurrentProgressPercentage == 100 && StatusMessage.Contains(LanguageManager.GetString("ExecutingJob")))
                    StatusMessage = $"{SelectedBackupJob.Name}: {LanguageManager.GetString("BackupCompleted")}";
                else if (!StatusMessage.Contains(LanguageManager.GetString("JobInterrupted")) && !StatusMessage.Contains(LanguageManager.GetString("BackupError")))
                {
                }
            }
        }

        public async Task ExecuteAllJobsAsync()
        {
            IsExecutingBackup = true;
            CurrentProgressPercentage = 0;
            bool anErrorOccurredOverall = false;
            string overallErrorMessage = string.Empty;
            bool cancelledByUser = false;

            try
            {
                foreach (var jobVm in BackupJobs.ToList())
                {
                    if (await CheckAndLogBusinessSoftwareAsync(jobVm.Name))
                    {
                        // Using Path.GetFileName for a cleaner display in the message if BusinessSoftwareProcessName is a path
                        string softwareDisplayName = Path.GetFileName(_appSettings?.BusinessSoftwareProcessName);
                        overallErrorMessage = $"{LanguageManager.GetString("JobSkipped")} {jobVm.Name}. {LanguageManager.GetString("BusinessSoftwareDetected")} ({softwareDisplayName})";
                        anErrorOccurredOverall = true;

                        var userChoice = System.Windows.MessageBox.Show(
                            $"{overallErrorMessage}\n\n{LanguageManager.GetString("ContinueWithOtherJobs")}",
                            LanguageManager.GetString("BusinessSoftwareDetected"),
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);
                        if (userChoice == MessageBoxResult.No)
                        {
                            StatusMessage = LanguageManager.GetString("AllJobsExecutionCancelled");
                            cancelledByUser = true;
                            break;
                        }
                        continue;
                    }

                    CurrentProgressPercentage = 0;
                    StatusMessage = $"{LanguageManager.GetString("ExecutingJob")} {jobVm.Name}...";
                    try
                    {
                        await _backupManagerService.ExecuteBackupJobAsync(jobVm.JobModel);
                    }
                    catch (BusinessSoftwareInterruptionException bsie)
                    {
                        anErrorOccurredOverall = true;
                        overallErrorMessage = $"{LanguageManager.GetString("JobInterrupted")} {jobVm.Name}: {bsie.Message}";
                        StatusMessage = overallErrorMessage;
                        var result = System.Windows.MessageBox.Show(
                            $"{overallErrorMessage}\n\n{LanguageManager.GetString("ContinueWithOtherJobs")}",
                            LanguageManager.GetString("OperationAborted"),
                            MessageBoxButton.YesNo, MessageBoxImage.Error);
                        if (result == MessageBoxResult.No)
                        {
                            cancelledByUser = true;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        anErrorOccurredOverall = true;
                        overallErrorMessage = $"{LanguageManager.GetString("BackupError")} on job {jobVm.Name}: {ex.Message}";
                        StatusMessage = overallErrorMessage;
                        var result = System.Windows.MessageBox.Show(
                            $"{overallErrorMessage}\n\n{LanguageManager.GetString("ContinueWithOtherJobs")}",
                            "Execution Error",
                            MessageBoxButton.YesNo, MessageBoxImage.Error);
                        if (result == MessageBoxResult.No)
                        {
                            cancelledByUser = true;
                            break;
                        }
                    }
                }

                if (cancelledByUser)
                {
                }
                else if (anErrorOccurredOverall)
                {
                    StatusMessage = $"{LanguageManager.GetString("AllJobsCompletedWithIssues")}: {overallErrorMessage}";
                }
                else
                {
                    StatusMessage = LanguageManager.GetString("AllJobsCompletedSuccessfully");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"{LanguageManager.GetString("GenericErrorDuringAllJobs")} {ex.Message}";
                System.Windows.MessageBox.Show(StatusMessage, "Execution Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsExecutingBackup = false;
            }
        }

        private async Task SaveJobsConfigurationAsync()
        {
            await ConfigManager.SaveJobsAsync(_backupManagerService.GetAllJobs().ToList());
        }
    }
}