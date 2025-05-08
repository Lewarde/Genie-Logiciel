using System;
using System.Threading.Tasks;
using EasySave.Core;
using EasySave.Utils;

namespace EasySave
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                // Initialize localization
                LanguageManager.Initialize();

                // Ask user for language choice
                Console.WriteLine("Choose language / Choisissez la langue :");
                Console.WriteLine("1. English");
                Console.WriteLine("2. Français");
                Console.Write(">> ");
                string languageChoice = Console.ReadLine();

                if (languageChoice == "2")
                {
                    LanguageManager.SetLanguage("fr");
                }
                else
                {
                    LanguageManager.SetLanguage("en");
                }

                // Display welcome message
                Console.WriteLine(LanguageManager.GetString("WelcomeMessage"));

                // Initialize the application
                var appManager = new ApplicationManager();
                await appManager.InitializeAsync();

                // Parse command line arguments if provided
                if (args.Length > 0)
                {
                    await appManager.ProcessCommandLineArgs(args);
                }
                else
                {
                    // Start interactive menu
                    await appManager.StartInteractiveMenuAsync();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(LanguageManager.GetString("ErrorOccurred") + ex.Message);
                Console.ResetColor();
            }

            Console.WriteLine(LanguageManager.GetString("PressAnyKeyToExit"));
            Console.ReadKey();
        }
    }
}