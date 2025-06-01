using System;
using System.Diagnostics;
using System.IO; // Required for Path operations

namespace EasySave.Services.CryptoSoft
{
    // Service for encrypting files using an external tool.
    public class EncryptionService
    {
        // Path to the encryption tool executable.
        private readonly string _encryptionToolPath = @"C:\Users\Adam\source\repos\Genie-Logiciel\Livrable3\CryptoSoft\bin\Debug\net9.0-windows\CryptoSoft.exe";

        // Encrypts a file using the external encryption tool.
        public long EncryptFile(string sourceFilePath, string targetDirectoryPath)
        {
            // Check if the encryption tool exists.
            if (!File.Exists(_encryptionToolPath))
            {
                Console.WriteLine($"Error: The encryption tool CryptoSoft.exe was not found at: {_encryptionToolPath}");
                return -10; // Error code for missing tool
            }

            // Check if the source file exists.
            if (!File.Exists(sourceFilePath))
            {
                Console.WriteLine($"Error: The source file to encrypt does not exist: {sourceFilePath}");
                return -11; // Error code for missing source file
            }

            // Check if the target directory exists.
            if (!Directory.Exists(targetDirectoryPath))
            {
                Console.WriteLine($"Error: The target directory for encryption does not exist: {targetDirectoryPath}");
                return -12; // Error code for missing target directory
            }

            // Define the encrypted file name and path.
            string encryptedFileName = Path.GetFileName(sourceFilePath) + ".crypt";
            string encryptedTargetFilePath = Path.Combine(targetDirectoryPath, encryptedFileName);

            // Prepare arguments for the encryption tool.
            string arguments = $"\"{sourceFilePath}\" \"{encryptedTargetFilePath}\"";
            var stopwatch = new Stopwatch();

            try
            {
                // Configure the process start info for the encryption tool.
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

                    // Start the encryption process and measure the time taken.
                    stopwatch.Start();
                    process.Start();

                    // Read output and error streams.
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    process.WaitForExit(); // Wait for the process to finish.
                    stopwatch.Stop();

                    // Return the elapsed time if successful, otherwise return an error code.
                    if (process.ExitCode == 0)
                    {
                        return stopwatch.ElapsedMilliseconds; // Success
                    }
                    else
                    {
                        Console.WriteLine($"CryptoSoft Error (ExitCode {process.ExitCode}): {error}");
                        return process.ExitCode != 0 ? -process.ExitCode : -1;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception.
                Console.WriteLine($"Exception during encryption of {sourceFilePath}: {ex.Message}");
                return -99; // Generic error code for exceptions
            }
        }
    }
}
