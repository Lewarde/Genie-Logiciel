// BackupJobViewModel.cs
using EasySave.Models;
using EasySave.Commands; // Assuming RelayCommand is here
using System.IO; // For Path.GetFileName
using System.Windows.Input; // For ICommand
using EasySave.BackupManager; // Namespace for BackupManager service
using EasySave.Utils; // For LanguageManager

namespace EasySave.ViewModels
{
    public class BackupJobViewModel : BaseViewModel
    {
        private BackupJob _job;
        private EasySave.BackupManager.BackupManager _backupManagerService; // To call Pause/Resume/Stop

        public BackupJob JobModel => _job;

        private int _currentProgressPercentage;
        public int CurrentProgressPercentage
        {
            get => _currentProgressPercentage;
            set => SetProperty(ref _currentProgressPercentage, value); // Notify UI if value changes
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private bool _isExecuting; // True if job is Active or Paused
        public bool IsExecuting
        {
            get => _isExecuting;
            set
            {
                if (SetProperty(ref _isExecuting, value))
                {
                    UpdateCommandStates(); // Update button states when execution state changes
                }
            }
        }

        private bool _isPaused; // True if job is Paused
        public bool IsPaused
        {
            get => _isPaused;
            set
            {
                if (SetProperty(ref _isPaused, value))
                {
                    UpdateCommandStates(); // Update button states when pause state changes
                }
            }
        }

        // Commands bound to the UI
        public ICommand PauseCommand { get; }
        public ICommand ResumeCommand { get; }
        public ICommand StopCommand { get; }

        // Used to control whether each command is enabled
        private bool _canPause;
        public bool CanPause
        {
            get => _canPause;
            set => SetProperty(ref _canPause, value);
        }

        private bool _canResume;
        public bool CanResume
        {
            get => _canResume;
            set => SetProperty(ref _canResume, value);
        }

        private bool _canStop;
        public bool CanStop
        {
            get => _canStop;
            set => SetProperty(ref _canStop, value);
        }

        // Constructor
        public BackupJobViewModel(BackupJob job, EasySave.BackupManager.BackupManager backupManagerService)
        {
            _job = job;
            _backupManagerService = backupManagerService;
            Name = job.Name;

            // RelayCommands are initialized with actions and conditions
            PauseCommand = new RelayCommand(async () => await _backupManagerService.PauseJobAsync(_job.Name), () => CanPause);
            ResumeCommand = new RelayCommand(async () => await _backupManagerService.ResumeJobAsync(_job.Name), () => CanResume);
            StopCommand = new RelayCommand(async () => await _backupManagerService.StopJobAsync(_job.Name), () => CanStop);

            ResetState(); // Set default state
        }

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
                {
                    if (_job != null) _job.Name = value; // Update model too
                    OnPropertyChanged(nameof(DisplayMember)); // Refresh display string
                }
            }
        }

        // Properties linked to BackupJob model
        public string SourceDirectory
        {
            get => _job.SourceDirectory;
            set
            {
                if (_job.SourceDirectory != value)
                {
                    _job.SourceDirectory = value;
                    OnPropertyChanged();
                }
            }
        }

        public string TargetDirectory
        {
            get => _job.TargetDirectory;
            set
            {
                if (_job.TargetDirectory != value)
                {
                    _job.TargetDirectory = value;
                    OnPropertyChanged();
                }
            }
        }

        public EncryptionFileExtension FileExtension
        {
            get => _job.FileExtension;
            set
            {
                if (_job.FileExtension != value)
                {
                    _job.FileExtension = value;
                    OnPropertyChanged();
                }
            }
        }

        public PriorityFileExtension Priority
        {
            get => _job.Priority;
            set
            {
                if (_job.Priority != value)
                {
                    _job.Priority = value;
                    OnPropertyChanged();
                }
            }
        }

        public string DisplayMember => $"{Name}"; // Used for UI display

        // Updates internal model and notifies UI
        public void UpdateModel(BackupJob job)
        {
            _job = job;
            Name = job.Name;
            OnPropertyChanged(nameof(SourceDirectory));
            OnPropertyChanged(nameof(TargetDirectory));
            OnPropertyChanged(nameof(FileExtension));
            OnPropertyChanged(nameof(DisplayMember));
            OnPropertyChanged(nameof(Priority));
        }

        // Called to update progress and states from the backup process
        public void UpdateProgress(BackupProgress progress)
        {
            CurrentProgressPercentage = progress.Progress;
            IsExecuting = progress.State == BackupState.Active || progress.State == BackupState.Paused;
            IsPaused = progress.State == BackupState.Paused;

            switch (progress.State)
            {
                case BackupState.Inactive: // After stop, error, or finish
                    StatusMessage = LanguageManager.GetString("StatusReady");
                    IsExecuting = false;
                    IsPaused = false;
                    break;
                case BackupState.Active:
                    // Show progress and current file
                    StatusMessage = string.Format(
                        LanguageManager.GetString("StatusActive"),
                        progress.Progress,
                        Path.GetFileName(progress.CurrentSourceFile ?? "Initializing..."));
                    break;
                case BackupState.Paused:
                    StatusMessage = LanguageManager.GetString("StatusPaused");
                    break;
                case BackupState.Stopped:
                    StatusMessage = LanguageManager.GetString("StatusStopped");
                    IsExecuting = false;
                    IsPaused = false;
                    break;
                case BackupState.Completed:
                    StatusMessage = LanguageManager.GetString("StatusCompleted");
                    CurrentProgressPercentage = 100;
                    IsExecuting = false;
                    IsPaused = false;
                    break;
                case BackupState.Error:
                    StatusMessage = LanguageManager.GetString("StatusError");
                    IsExecuting = false;
                    IsPaused = false;
                    break;
                case BackupState.Interrupted:
                    StatusMessage = LanguageManager.GetString("StatusInterrupted");
                    IsExecuting = false;
                    IsPaused = false;
                    break;
                default:
                    StatusMessage = progress.State.ToString(); // Fallback
                    break;
            }

            UpdateCommandStates();
        }

        // Updates the command availability based on state
        private void UpdateCommandStates()
        {
            CanPause = IsExecuting && !IsPaused;
            CanResume = IsExecuting && IsPaused;
            CanStop = IsExecuting;

            // Notify UI that CanExecute values may have changed
            (PauseCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ResumeCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (StopCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        // Reset state when job is loaded or finished
        public void ResetState()
        {
            CurrentProgressPercentage = 0;
            StatusMessage = LanguageManager.GetString("StatusReady");
            IsExecuting = false;
            IsPaused = false;
            UpdateCommandStates();
        }
    }
}
