using System;
using System.Threading;
using System.Windows.Forms;

namespace CryptoSoft
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Nom global pour Mutex. Préfixe "Global\\" = mutex visible pour tous les utilisateurs (multi-session, multi-compte)
            string mutexName = "Global\\CryptoSoftSingletonMutex";

            // Mutex : mode cross-user / global
            bool createdNew = false;
            using (Mutex mutex = new Mutex(false, mutexName, out createdNew))
            {
                bool hasHandle = false;
                try
                {
                    // Attendre max 2 secondes pour acquisition du Mutex (pour éviter blocage infini si Mutex laissé vérouillé par un processus planté)
                    hasHandle = mutex.WaitOne(TimeSpan.FromSeconds(15), false);
                    if (!hasHandle)
                    {
                        // Ajout d'un log pour l'équipe (trace technique)
                        Console.WriteLine($"[{DateTime.Now}] Une autre instance de CryptoSoft est déjà en cours. Lancement refusé.");

                        MessageBox.Show("Une autre instance de CryptoSoft est déjà en cours d'exécution.", "Instance existante", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }


                    // Vérifie les arguments
                    string? fichierSource = args.Length > 0 ? args[0] : null;
                    string? fichierCible = args.Length > 1 ? args[1] : null;

                    if (!string.IsNullOrEmpty(fichierSource) && !System.IO.File.Exists(fichierSource))
                    {
                        MessageBox.Show($"Le fichier source spécifié n'existe pas : {fichierSource}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    try
                    {
                        ApplicationConfiguration.Initialize();
                        Application.Run(new Form(fichierSource, fichierCible));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erreur critique lors du démarrage : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (AbandonedMutexException)
                {
                    // Mutex abandonné par un process crashé → toujours pris pour éviter blocage
                    hasHandle = true;

                    MessageBox.Show("Une autre instance a été brutalement arrêtée. Le programme va continuer.", "Alerte", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    ApplicationConfiguration.Initialize();
                    Application.Run(new Form());
                }
                finally
                {
                    // Libère le mutex si c’est nous qui l’avons pris
                    if (hasHandle)
                    {
                        mutex.ReleaseMutex();
                    }
                }
            }
        }
    }
}
