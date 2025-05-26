using System;
using System.Diagnostics;
using System.IO; // Ajout pour Path

namespace EasySave.Services.CryptoSoft
{
    public class EncryptionService
    {
        // Le chemin vers CryptoSoft.exe doit être configurable ou relatif
        private readonly string _encryptionToolPath = @"C:\Users\ajese\OneDrive - Association Cesi Viacesi mail\Documents\Génie Logiciel\Livrable3\CryptoSoft\bin\Debug\net9.0-windows\CryptoSoft.exe"; // Utilisez @ pour les chemins

        public long EncryptFile(string sourceFilePath, string targetDirectoryPath)
        {
            if (!File.Exists(_encryptionToolPath))
            {
                Console.WriteLine($"Erreur : L'outil de chiffrement CryptoSoft.exe n'a pas été trouvé à l'emplacement : {_encryptionToolPath}");
                return -10;
            }
            if (!File.Exists(sourceFilePath))
            {
                Console.WriteLine($"Erreur : Le fichier source à chiffrer n'existe pas : {sourceFilePath}");
                return -11;
            }
            if (!Directory.Exists(targetDirectoryPath))
            {
                Console.WriteLine($"Erreur : Le répertoire cible pour le chiffrement n'existe pas : {targetDirectoryPath}");
                return -12; // Code d'erreur spécifique : répertoire cible non trouvé
            }


            // Le nom du fichier chiffré sera sourceFileName.extension.crypt
            string encryptedFileName = Path.GetFileName(sourceFilePath) + ".crypt";
            string encryptedTargetFilePath = Path.Combine(targetDirectoryPath, encryptedFileName);

            string arguments = $"\"{sourceFilePath}\" \"{encryptedTargetFilePath}\"";
            var stopwatch = new Stopwatch();

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = _encryptionToolPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;

                    stopwatch.Start();
                    process.Start();

                    // Lire les sorties pour éviter le blocage du buffer
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    process.WaitForExit(); // Attendre la fin du processus
                    stopwatch.Stop();

                    // Vous pouvez logger output et error ici si vous le souhaitez,
                    // mais ne pas utiliser Console.WriteLine directement dans une lib/service.
                    // Exemple: _logger.LogInfo("CryptoSoft Output: " + output);
                    // Exemple: _logger.LogError("CryptoSoft Error: " + error);

                    if (process.ExitCode == 0)
                    {
                        return stopwatch.ElapsedMilliseconds; // Succès
                    }
                    else
                    {
                        // Retourner un code d'erreur négatif basé sur ExitCode
                        // ou un code d'erreur générique.
                        Console.WriteLine($"Erreur de CryptoSoft (ExitCode {process.ExitCode}): {error}");
                        return process.ExitCode != 0 ? -process.ExitCode : -1; // Évite de retourner 0 si ExitCode est 0 mais qu'il y a eu une autre erreur
                    }
                }
            }
            catch (Exception ex)
            {
                // Logger l'exception
                Console.WriteLine($"Exception lors du chiffrement de {sourceFilePath}: {ex.Message}");
                return -99; // Code d'erreur générique pour exception
            }
        }
    }
}