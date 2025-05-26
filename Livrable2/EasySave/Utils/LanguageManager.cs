using System;
using System.Collections.Generic;
using System.Globalization;

namespace EasySave.Utils
{
    // Made class static as all its members were static or effectively static
    public static class LanguageManager
    {
        private static Dictionary<string, Dictionary<string, string>> _translations;
        private static string _currentLanguage = "en"; // Default language
        // Removed unused singleton instance fields and properties

        public static void Initialize()
        {
            _translations = new Dictionary<string, Dictionary<string, string>>();

            var englishTranslations = new Dictionary<string, string>
            {
                { "WelcomeMessage", "Welcome to EasySave!" },
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
                { "EnterJobName", "Job Name:" },
                { "EnterSourceDir", "Source Directory:" },
                { "EnterTargetDir", "Target Directory:" },
                { "SelectBackupType", "Backup Type:" },
                { "FullBackup", "Full Backup" },
                { "DifferentialBackup", "Differential Backup" },
                { "MaxJobsReached", "Maximum number of backup jobs (5) reached." },
                { "JobCreatedSuccessfully", "Backup job created successfully." },
                { "EnterJobIndexToModify", "Enter job index to modify:" }, // Less relevant for GUI
                { "EnterJobIndexToDelete", "Enter job index to delete:" }, // Less relevant for GUI
                { "InvalidJobIndex", "Invalid job selection or no job selected." }, // Adapted for GUI
                { "JobModifiedSuccessfully", "Backup job modified successfully." },
                { "JobDeletedSuccessfully", "Backup job deleted successfully." },
                { "BackupJobs", "Backup Jobs" },
                { "NoJobsCreated", "No backup jobs created yet." },
                { "Source", "Source" },
                { "Target", "Target" },
                { "ExecutionOptions", "Execution Options" },
                { "ExecuteSingleJob", "Execute Selected Job" },
                { "ExecuteAllJobs", "Execute All Jobs" },
                { "EnterJobIndexToExecute", "Enter job index to execute:" }, // Less relevant for GUI
                { "ExecutingJob", "Executing backup job: " },
                { "SourceDirNotFound", "Source directory not found." },
                { "BackupCompleted", "Backup completed successfully." },
                { "AllJobsCompleted", "All backup jobs completed." }, // New
                { "BackupError", "Error during backup: " },
                { "Progress", "Progress" },
                { "ErrorCopyingFile", "Error copying file " },
                { "NoFilesToCopy", "No files needed to be copied (already up to date)." },
                { "ErrorOccurred", "An error occurred: " },
                { "PressAnyKeyToExit", "Press any key to exit..." }, // Less relevant for GUI
                { "ErrorLoadingConfig", "Error loading configuration: " },
                { "ErrorSavingConfig", "Error saving configuration: " },
                { "OK", "OK" }, // New for dialogs
                { "Cancel", "Cancel" }, // New for dialogs
                { "FieldsCannotBeEmpty", "All fields must be filled." }, // New for validation
                { "ValidationError", "Validation Error" }, // New for validation
                { "Language", "Language:"}, // New for UI
                { "LogFormat", "Log Format:"} // New for UI
            };

            var frenchTranslations = new Dictionary<string, string>
            {
                { "WelcomeMessage", "Bienvenue sur EasySave !" },
                { "ManageBackupJobs", "Gérer les Tâches de Sauvegarde" },
                { "ExecuteBackupJobs", "Exécuter les Tâches de Sauvegarde" },
                { "Exit", "Quitter" },
                { "EnterChoice", "Entrez votre choix :" },
                { "InvalidOption", "Option invalide. Veuillez réessayer." },
                { "CreateBackupJob", "Créer une Tâche" },
                { "ModifyBackupJob", "Modifier la Tâche" },
                { "DeleteBackupJob", "Supprimer la Tâche" },
                { "DisplayBackupJobs", "Afficher les Tâches" },
                { "Back", "Retour" },
                { "EnterJobName", "Nom de la tâche :" },
                { "EnterSourceDir", "Répertoire Source :" },
                { "EnterTargetDir", "Répertoire Cible :" },
                { "SelectBackupType", "Type de Sauvegarde :" },
                { "FullBackup", "Sauvegarde Complète" },
                { "DifferentialBackup", "Sauvegarde Différentielle" },
                { "MaxJobsReached", "Nombre maximum de tâches de sauvegarde (5) atteint." },
                { "JobCreatedSuccessfully", "Tâche de sauvegarde créée avec succès." },
                { "EnterJobIndexToModify", "Entrez l'index de la tâche à modifier :" },
                { "EnterJobIndexToDelete", "Entrez l'index de la tâche à supprimer :" },
                { "InvalidJobIndex", "Sélection de tâche invalide ou aucune tâche sélectionnée." },
                { "JobModifiedSuccessfully", "Tâche de sauvegarde modifiée avec succès." },
                { "JobDeletedSuccessfully", "Tâche de sauvegarde supprimée avec succès." },
                { "BackupJobs", "Tâches de Sauvegarde" },
                { "NoJobsCreated", "Aucune tâche de sauvegarde créée pour le moment." },
                { "Source", "Source" },
                { "Target", "Cible" },
                { "ExecutionOptions", "Options d'Exécution" },
                { "ExecuteSingleJob", "Exécuter la Tâche Sélectionnée" },
                { "ExecuteAllJobs", "Exécuter Toutes les Tâches" },
                { "EnterJobIndexToExecute", "Entrez l'index de la tâche à exécuter :" },
                { "ExecutingJob", "Exécution de la tâche : " },
                { "SourceDirNotFound", "Répertoire source introuvable." },
                { "BackupCompleted", "Sauvegarde terminée avec succès." },
                { "AllJobsCompleted", "Toutes les sauvegardes sont terminées." }, // New
                { "BackupError", "Erreur pendant la sauvegarde : " },
                { "Progress", "Progression" },
                { "ErrorCopyingFile", "Erreur lors de la copie du fichier " },
                { "NoFilesToCopy", "Aucun fichier à copier (déjà à jour)." },
                { "ErrorOccurred", "Une erreur s'est produite : " },
                { "PressAnyKeyToExit", "Appuyez sur une touche pour quitter..." },
                { "ErrorLoadingConfig", "Erreur chargement configuration : " },
                { "ErrorSavingConfig", "Erreur sauvegarde configuration : " },
                { "OK", "OK" },
                { "Cancel", "Annuler" },
                { "FieldsCannotBeEmpty", "Tous les champs doivent être remplis." },
                { "ValidationError", "Erreur de Validation" },
                { "Language", "Langue :"},
                { "LogFormat", "Format des Logs :"}
            };

            _translations["en"] = englishTranslations;
            _translations["fr"] = frenchTranslations;

            // System language detection (can be overridden by saved settings later)
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
                // Fallback to English if system language detection fails
                _currentLanguage = "en";
            }
        }

        public static string GetString(string key)
        {
            if (_translations == null) Initialize(); // Ensure initialized

            if (_translations.TryGetValue(_currentLanguage, out var translations) &&
                translations.TryGetValue(key, out string translation))
            {
                return translation;
            }

            // Fallback to English if current language or key not found
            if (_currentLanguage != "en" && _translations.TryGetValue("en", out var englishTranslations) &&
                englishTranslations.TryGetValue(key, out string englishTranslation))
            {
                return englishTranslation; // Return English translation
            }

            return key; // Return the key itself if not found anywhere
        }

        public static void SetLanguage(string language)
        {
            if (_translations == null) Initialize(); // Ensure initialized

            if (!string.IsNullOrEmpty(language) && _translations.ContainsKey(language))
            {
                _currentLanguage = language;
            }
            // Optionally, else set to default or log an error
        }

        public static string GetCurrentLanguage()
        {
            return _currentLanguage;
        }
    }
}