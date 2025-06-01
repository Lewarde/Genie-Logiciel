using System.Collections.Generic;

namespace EasySave.Config
{
    public class AppSettingsData
    {
        public string Language { get; set; } = "en";
        public string LogFormat { get; set; } = "JSON";
        public string BusinessSoftwareProcessName { get; set; } // e.g., "calc.exe" or "notepad.exe"

        public List<string> PriorityExtensions { get; set; } = new List<string> { ".pdf", ".docx", ".xlsx" };

        public AppSettingsData()
        {
            BusinessSoftwareProcessName = string.Empty;
        }
    }
}