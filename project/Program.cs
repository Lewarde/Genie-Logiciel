using System;
using System.Threading.Tasks;
using EasySave.Core;    // Référence au cœur de l'application (logique principale)
using EasySave.Utils;   // Référence aux utilitaires (ex : gestion des langues)

namespace EasySave
{
    class Program
    {
        // Point d'entrée principal du programme (fonction asynchrone)
        static async Task Main(string[] args)
        {
            try
            {
                // Initialiser la gestion des langues (chargement des traductions disponibles)
                LanguageManager.Initialize();

                // Demander à l'utilisateur de choisir une langue
                Console.WriteLine("Choose language / Choisissez la langue :");
                Console.WriteLine("1. English");
                Console.WriteLine("2. Français");
                Console.Write(">> ");
                string languageChoice = Console.ReadLine();

                // Appliquer la langue choisie par l'utilisateur
                if (languageChoice == "2")
                {
                    LanguageManager.SetLanguage("fr"); // Choix du français
                }
                else
                {
                    LanguageManager.SetLanguage("en"); // Par défaut, anglais
                }

                // Afficher un message de bienvenue dans la langue choisie
                Console.WriteLine(LanguageManager.GetString("WelcomeMessage"));

                // Initialiser la logique principale de l'application
                var appManager = new ApplicationManager();
                await appManager.InitializeAsync(); // Chargement initial (paramètres, état, etc.)

                // Si des arguments sont fournis en ligne de commande
                if (args.Length > 0)
                {
                    // Traiter les arguments automatiquement (exécution en mode script)
                    await appManager.ProcessCommandLineArgs(args);
                }
                else
                {
                    // Sinon, démarrer un menu interactif (mode utilisateur)
                    await appManager.StartInteractiveMenuAsync();
                }
            }
            catch (Exception ex)
            {
                // En cas d'erreur, afficher un message en rouge
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(LanguageManager.GetString("ErrorOccurred") + ex.Message);
                Console.ResetColor(); // Réinitialiser la couleur d'origine de la console
            }

            // Fin du programme : attendre que l'utilisateur appuie sur une touche
            Console.WriteLine(LanguageManager.GetString("PressAnyKeyToExit"));
            Console.ReadKey();
        }
    }
}
