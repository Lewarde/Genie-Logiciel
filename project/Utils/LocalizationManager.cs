using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace EasySave.Utils
{
    public static class LocalizationManager
    {
        private static Dictionary<string, Dictionary<string, string>> _translations;
        private static string _currentLanguage = "en"; // Default language
        
        public static void Initialize()
        {
            _translations = new Dictionary<string, Dictionary<string, string>>();
            
            // English translations
            var englishTranslations = new Dictionary<string, string>
            {
                { "WelcomeMessage", "Welcome to EasySave 1.0!" },
                { "ManageBackupJobs", "Manage Backup Jobs" },
                { "ExecuteBackupJobs", "Execute Backup Jobs" },
                { "Exit", "Exit" },
                { "EnterChoice", "Enter your choice:" },
                { "InvalidOption", "Invalid option. Please try again." },
                { "CreateBackupJob", "Create Backup Job" },
                { "ModifyBackupJob", "Modify Backup Job" },
                { "DeleteBackupJob", "Delete Backup Job" },
                { "DisplayBackupJobs", "Display Backup Jobs" },
                { "Back", "Back" },
                { "EnterJobName", "Enter job name:" },
                { "EnterSourceDir", "Enter source directory:" },
                { "EnterTargetDir", "Enter target directory:" },
                { "SelectBackupType", "Select backup type:" },
                { "FullBackup", "Full Backup" },
                { "DifferentialBackup", "Differential Backup" },
                { "MaxJobsReached", "Maximum number of backup jobs (5) reached." },
                { "JobCreatedSuccessfully", "Backup job created successfully." },
                { "EnterJobIndexToModify", "Enter job index to modify:" },
                { "EnterJobIndexToDelete", "Enter job index to delete:" },
                { "InvalidJobIndex", "Invalid job index." },
                { "JobModifiedSuccessfully", "Backup job modified successfully." },
                { "JobDeletedSuccessfully", "Backup job deleted successfully." },
                { "BackupJobs", "Backup Jobs" },
                { "NoJobsCreated", "No backup jobs created yet." },
                { "Source", "Source" },
                { "Target", "Target" },
                { "ExecutionOptions", "Execution Options" },
                { "ExecuteSingleJob", "Execute Single Job" },
                { "ExecuteAllJobs", "Execute All Jobs" },
                { "EnterJobIndexToExecute", "Enter job index to execute:" },
                { "ExecutingJob", "Executing backup job: " },
                { "SourceDirNotFound", "Source directory not found." },
                { "BackupCompleted", "Backup completed successfully." },
                { "BackupError", "Error during backup: " },
                { "Progress", "Progress" },
                { "ErrorCopyingFile", "Error copying file " },
                { "NoFilesToCopy", "No files needed to be copied (already up to date)." },
                { "ErrorOccurred", "An error occurred: " },
                { "PressAnyKeyToExit", "Press any key to exit..." },
                { "ErrorLoadingConfig", "Error loading configuration: " },
                { "ErrorSavingConfig", "Error saving configuration: " }
            };
            
            // French translations
            var frenchTranslations = new Dictionary<string, string>
            {
                { "WelcomeMessage", "Bienvenue sur EasySave 1.0 !" },
                { "ManageBackupJobs", "Gérer les Tâches de Sauvegarde" },
                { "ExecuteBackupJobs", "Exécuter les Tâches de Sauvegarde" },
                { "Exit", "Quitter" },
                { "EnterChoice", "Entrez votre choix :" },
                { "InvalidOption", "Option invalide. Veuillez réessayer." },
                { "CreateBackupJob", "Créer une Tâche de Sauvegarde" },
                { "ModifyBackupJob", "Modifier une Tâche de Sauvegarde" },
                { "DeleteBackupJob", "Supprimer une Tâche de Sauvegarde" },
                { "DisplayBackupJobs", "Afficher les Tâches de Sauvegarde" },
                { "Back", "Retour" },
                { "EnterJobName", "Entrez le nom de la tâche :" },
                { "EnterSourceDir", "Entrez le répertoire source :" },
                { "EnterTargetDir", "Entrez le répertoire cible :" },
                { "SelectBackupType", "Sélectionnez le type de sauvegarde :" },
                { "FullBackup", "Sauvegarde Complète" },
                { "DifferentialBackup", "Sauvegarde Différentielle" },
                { "MaxJobsReached", "Nombre maximum de tâches de sauvegarde (5) atteint." },
                { "JobCreatedSuccessfully", "Tâche de sauvegarde créée avec succès." },
                { "EnterJobIndexToModify", "Entrez l'index de la tâche à modifier :" },
                { "EnterJobIndexToDelete", "Entrez l'index de la tâche à supprimer :" },
                { "InvalidJobIndex", "Index de tâche invalide." },
                { "JobModifiedSuccessfully", "Tâche de sauvegarde modifiée avec succès." },
                { "JobDeletedSuccessfully", "Tâche de sauvegarde supprimée avec succès." },
                { "BackupJobs", "Tâches de Sauvegarde" },
                { "NoJobsCreated", "Aucune tâche de sauvegarde créée pour le moment." },
                { "Source", "Source" },
                { "Target", "Cible" },
                { "ExecutionOptions", "Options d'Exécution" },
                { "ExecuteSingleJob", "Exécuter une Seule Tâche" },
                { "ExecuteAllJobs", "Exécuter Toutes les Tâches" },
                { "EnterJobIndexToExecute", "Entrez l'index de la tâche à exécuter :" },
                { "ExecutingJob", "Exécution de la tâche de sauvegarde : " },
                { "SourceDirNotFound", "Répertoire source introuvable." },
                { "BackupCompleted", "Sauvegarde terminée avec succès." },
                { "BackupError", "Erreur pendant la sauvegarde : " },
                { "Progress", "Progression" },
                { "ErrorCopyingFile", "Erreur lors de la copie du fichier " },
                { "NoFilesToCopy", "Aucun fichier n'a besoin d'être copié (déjà à jour)." },
                { "ErrorOccurred", "Une erreur s'est produite : " },
                { "PressAnyKeyToExit", "Appuyez sur une touche pour quitter..." },
                { "ErrorLoadingConfig", "Erreur lors du chargement de la configuration : " },
                { "ErrorSavingConfig", "Erreur lors de l'enregistrement de la configuration : " }
            };
            
            _translations["en"] = englishTranslations;
            _translations["fr"] = frenchTranslations;
            
            // Detect system language
            try
            {
                string systemLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLower();
                if (_translations.ContainsKey(systemLanguage))
                {
                    _currentLanguage = systemLanguage;
                }
            }
            catch
            {
                // If detection fails, use English as default
                _currentLanguage = "en";
            }
        }
        
        public static string GetString(string key)
        {
            if (_translations.TryGetValue(_currentLanguage, out var translations) && 
                translations.TryGetValue(key, out string translation))
            {
                return translation;
            }
            
            // Fallback to English if the key is not found in the current language
            if (_translations.TryGetValue("en", out var englishTranslations) && 
                englishTranslations.TryGetValue(key, out string englishTranslation))
            {
                return englishTranslation;
            }
            
            // Return the key itself as a last resort
            return key;
        }
        
        public static void SetLanguage(string language)
        {
            if (_translations.ContainsKey(language))
            {
                _currentLanguage = language;
            }
        }
        
        public static string GetCurrentLanguage()
        {
            return _currentLanguage;
        }
    }
}