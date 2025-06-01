using System;

// Namespace for models used in the EasySave application.
namespace EasySave.Models
{
    // Represents a backup job with source, target, and file extension settings.
    public class BackupJob
    {
        public string Name { get; set; } // Name of the backup job.
        public string SourceDirectory { get; set; } // Directory to backup from.
        public string TargetDirectory { get; set; } // Directory to backup to.

        // File extension types for encryption and priority settings.
        public EncryptionFileExtension FileExtension { get; set; }
        public PriorityFileExtension Priority { get; set; }

        // Constructor initializes properties to default values.
        public BackupJob()
        {
            Name = string.Empty;
            SourceDirectory = string.Empty;
            TargetDirectory = string.Empty;
            FileExtension = EncryptionFileExtension.Null;
            Priority = PriorityFileExtension.Null;
        }
    }

    // Enum for file extensions that can be encrypted.
    public enum EncryptionFileExtension
    {
        Null, // No file extension.
        Txt,  // Text file.
        Docx, // Word document.
        Xlsx, // Excel spreadsheet.
        Jpg,  // JPEG image.
        Png,  // PNG image.
        Mp4,  // MP4 video.
        Mp3,  // MP3 audio.
        Avi,  // AVI video.
        Mkv,  // MKV video.
        Mov   // MOV video.
    }

    // Enum for file extensions with priority settings.
    public enum PriorityFileExtension
    {
        Null, // No file extension.
        Txt,  // Text file.
        Docx, // Word document.
        Xlsx, // Excel spreadsheet.
        Jpg,  // JPEG image.
        Png,  // PNG image.
        Mp4,  // MP4 video.
        Mp3,  // MP3 audio.
        Avi,  // AVI video.
        Mkv,  // MKV video.
        Mov   // MOV video.
    }
}
