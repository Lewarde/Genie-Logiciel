using System.Windows.Forms;
using System;

namespace CryptoSoft
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {

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
                Application.Run(new Form1(fichierSource, fichierCible));
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Erreur critique lors du démarrage de l'application : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
//