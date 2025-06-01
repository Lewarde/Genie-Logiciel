using System.Collections.Generic; // For using List<T>.

namespace EasySave.Config
{
    // Class to store application settings.
    public class AppSettingsData
    {
        public string Language { get; set; } = "en"; // Default language is English.
        public string LogFormat { get; set; } = "JSON"; // Default log format is JSON.
        public string BusinessSoftwareProcessName { get; set; }

        // List of priority file extensions.
        public List<string> PriorityExtensions { get; set; } = new List<string> { ".pdf", ".docx", ".xlsx" };

        // Constructor initializes BusinessSoftwareProcessName.
        public AppSettingsData()
        {
            BusinessSoftwareProcessName = string.Empty;
        }
    }
}
