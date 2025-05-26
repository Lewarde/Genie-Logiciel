
namespace EasySave.Config
{
    public class AppSettingsData
    {
        public string Language { get; set; } = "en";
        public string LogFormat { get; set; } = "JSON";
        public string BusinessSoftwareProcessName { get; set; } // e.g., "calc.exe" or "notepad.exe"


        public AppSettingsData()
        {
            BusinessSoftwareProcessName = string.Empty;
        }
    }
}