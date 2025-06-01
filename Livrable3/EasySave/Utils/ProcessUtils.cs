// Dans EasySave.Utils/ProcessUtils.cs
using System;
using System.Diagnostics;
using System.IO;
using System.ComponentModel; // For Win32Exception

namespace EasySave.Utils
{
    public static class ProcessUtils
    {
        public static bool IsProcessRunning(string fullExecutablePath)
        {
            if (string.IsNullOrWhiteSpace(fullExecutablePath))
            {
                return false;
            }

            string normalizedPathToFind;
            try
            {
                normalizedPathToFind = Path.GetFullPath(fullExecutablePath); // Normalize the input path
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ProcessUtils] Invalid path provided: {fullExecutablePath}. Error: {ex.Message}");
                return false;
            }

            Process[] processes = null;
            bool found = false;
            try
            {
                processes = Process.GetProcesses(); // Get all running processes
                foreach (Process process in processes)
                {
                    try
                    {
                        if (process.Id == 0 || process.Id == 4) continue; // Skip Idle and System process

                        // Check if process has a valid MainModule and file path
                        if (process.MainModule != null && !string.IsNullOrEmpty(process.MainModule.FileName))
                        {
                            string runningProcessPath = Path.GetFullPath(process.MainModule.FileName); // Normalize process path
                            // Compare paths ignoring case
                            if (string.Equals(runningProcessPath, normalizedPathToFind, StringComparison.OrdinalIgnoreCase))
                            {
                                found = true;
                                break;
                            }
                        }
                    }
                    catch (Win32Exception ex)
                    {
                        // Happens when access is denied to process information
                        Debug.WriteLine($"[ProcessUtils] Win32Exception for {process.ProcessName} (ID: {process.Id}): {ex.Message}");
                    }
                    catch (InvalidOperationException ex)
                    {
                        // Happens when the process has exited
                        Debug.WriteLine($"[ProcessUtils] InvalidOperationException for {process.ProcessName} (ID: {process.Id}): {ex.Message}");
                    }
                    catch (NotSupportedException ex)
                    {
                        // Happens when MainModule is not supported
                        Debug.WriteLine($"[ProcessUtils] NotSupportedException for {process.ProcessName} (ID: {process.Id}): {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        // Catch any other unexpected error
                        Debug.WriteLine($"[ProcessUtils] Generic exception for {process.ProcessName} (ID: {process.Id}): {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ProcessUtils] Error getting process list: {ex.Message}");
                return false;
            }
            finally
            {
                if (processes != null)
                {
                    // Always dispose processes to free system resources
                    foreach (Process process in processes)
                    {
                        process.Dispose();
                    }
                }
            }
            return found;
        }
    }
}
