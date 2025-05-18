using System;
using System.IO;
using System.Threading.Tasks;
using EasySave.Core;    // Logique principale
using EasySave.Utils;   // Langues
using Logger;           // Pour LogManager

namespace EasySave
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                // Initialiser la gestion des langues
                LanguageManager.Initialize();

                // Choix langue
                Console.WriteLine("Choose language / Choisissez la langue :");
                Console.WriteLine("1. English");
                Console.WriteLine("2. Français");
                Console.Write(">> ");
                string languageChoice = Console.ReadLine();
                LanguageManager.SetLanguage(languageChoice == "2" ? "fr" : "en");

                // Choix format log
                Console.WriteLine(LanguageManager.GetString("ChooseLogFormat"));
                Console.WriteLine("1. JSON");
                Console.WriteLine("2. XML");
                Console.Write(">> ");
                string logChoice = Console.ReadLine();
                string logFormat = logChoice == "2" ? "XML" : "JSON";

                // Initialisation du LogManager avec le format choisi
                LogManager.Initialize(logFormat);

                Console.WriteLine(LanguageManager.GetString("WelcomeMessage"));

                var appManager = new ApplicationManager();
                await appManager.InitializeAsync();

                if (args.Length > 0)
                {
                    await appManager.ProcessCommandLineArgs(args);
                }
                else
                {
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
