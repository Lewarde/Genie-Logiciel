// Dans EasySave.Utils/ProcessUtils.cs
using System;
using System.Diagnostics;
using System.IO;
using System.ComponentModel; // Pour Win32Exception

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
                normalizedPathToFind = Path.GetFullPath(fullExecutablePath);
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
                processes = Process.GetProcesses();
                foreach (Process process in processes)
                {
                    try
                    {
                        if (process.Id == 0 || process.Id == 4) continue; // Skip Idle and System process early

                        if (process.MainModule != null && !string.IsNullOrEmpty(process.MainModule.FileName))
                        {
                            string runningProcessPath = Path.GetFullPath(process.MainModule.FileName);
                            if (string.Equals(runningProcessPath, normalizedPathToFind, StringComparison.OrdinalIgnoreCase))
                            {
                                found = true;
                                break;
                            }
                        }
                    }
                    catch (Win32Exception ex)
                    {
                        // Debug.WriteLine($"[ProcessUtils] Win32Exception for {process.ProcessName} (ID: {process.Id}): {ex.Message}");
                    }
                    catch (InvalidOperationException ex)
                    {
                        // Debug.WriteLine($"[ProcessUtils] InvalidOperationException for {process.ProcessName} (ID: {process.Id}): {ex.Message}");
                    }
                    catch (NotSupportedException ex) // Can happen for some processes like Secure System
                    {
                        // Debug.WriteLine($"[ProcessUtils] NotSupportedException for {process.ProcessName} (ID: {process.Id}): {ex.Message}");
                    }
                    catch (Exception ex)
                    {
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