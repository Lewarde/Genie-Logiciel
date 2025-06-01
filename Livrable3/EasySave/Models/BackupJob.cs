using System;

namespace EasySave.Models
{
    public class BackupJob
    {
        public string Name { get; set; }
        public string SourceDirectory { get; set; }
        public string TargetDirectory { get; set; }

        public EncryptionFileExtension FileExtension { get; set; }
        public PriorityFileExtension Priority { get; set; } 


        public BackupJob()
        {
            Name = string.Empty;
            SourceDirectory = string.Empty;
            TargetDirectory = string.Empty;
            FileExtension = EncryptionFileExtension.Null;
            Priority = PriorityFileExtension.Null;
        }
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

    public enum PriorityFileExtension
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