using EasySave.Models;

namespace EasySave.ViewModels
{
    public class BackupJobViewModel : BaseViewModel
    {
        private BackupJob _job;
        public BackupJob JobModel => _job;

        public BackupJobViewModel(BackupJob job)
        {
            _job = job;
        }

        public string Name
        {
            get => _job.Name;
            set
            {
                if (_job.Name != value)
                {
                    _job.Name = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayMember));
                }
            }
        }

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

        public BackupType Type
        {
            get => _job.Type;
            set
            {
                if (_job.Type != value)
                {
                    _job.Type = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayMember));
                }
            }
        }

        public EncryptionFileExtension FileExtension // Assurez-vous que cette propriété existe
        {
            get => _job.FileExtension;
            set
            {
                if (_job.FileExtension != value)
                {
                    _job.FileExtension = value;
                    OnPropertyChanged();
                    // Si DisplayMember doit refléter l'extension, ajoutez OnPropertyChanged(nameof(DisplayMember));
                }
            }
        }
        public string DisplayMember => $"{Name} ({Type})"; // Vous pourriez ajouter l'extension ici si désiré

        public void UpdateModel(BackupJob job)
        {
            _job = job;
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(SourceDirectory));
            OnPropertyChanged(nameof(TargetDirectory));
            OnPropertyChanged(nameof(Type));
            OnPropertyChanged(nameof(FileExtension)); // Notifier le changement de FileExtension
            OnPropertyChanged(nameof(DisplayMember));
        }
    }
}