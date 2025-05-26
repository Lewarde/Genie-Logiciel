using System;

namespace EasySave.Models
{
    public class BackupJob
    {
        public string Name { get; set; }
        public string SourceDirectory { get; set; }
        public string TargetDirectory { get; set; }
        public BackupType Type { get; set; }

        public EncryptionFileExtension FileExtension { get; set; } 

        public BackupJob()
        {
            Name = string.Empty;
            SourceDirectory = string.Empty;
            TargetDirectory = string.Empty;
            Type = BackupType.Full;
            FileExtension = EncryptionFileExtension.Null; // Valeur par défaut
        }
    }

    public enum BackupType
    {
        Full,
        Differential
    }

    public enum EncryptionFileExtension
    {
        Null, 
        Txt,
        Docx,
        Xlsx,
        Jpg,
        Png,
        Mp4,
        Mp3,
        Avi,
        Mkv,
        Mov
        
    }
}