using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;


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
                { "SelectTypePrio", "priority extension:" },
                { "MaxJobsReached", "Maximum number of backup jobs (5) reached." },
                { "JobCreatedSuccessfully", "Backup job created successfully." },
                { "EnterJobIndexToModify", "Enter job index to modify:" },
                { "EnterJobIndexToDelete", "Enter job index to delete:" },
                { "InvalidJobIndex", "Invalid job selection or no job selected." },
                { "JobModifiedSuccessfully", "Backup job modified successfully." },
                { "JobDeletedSuccessfully", "Backup job deleted successfully." },
                { "BackupJobs", "Backup Jobs" },
                { "NoJobsCreated", "No backup jobs created yet." },
                { "Source", "Source" },
                { "Target", "Target" },
                { "ExecutionOptions", "Execution Options" },
                { "ExecuteSingleJob", "Execute Selected Job" },
                { "ExecuteAllJobs", "Execute All Jobs (Sequential)" }, // Clarified
                { "ExecuteAllJobsParallel", "Execute All Jobs (Parallel)"},
                { "EnterJobIndexToExecute", "Enter job index to execute:" },
                { "ExecutingJob", "Executing backup job: " },
                { "SourceDirNotFound", "Source directory not found." },
                { "BackupCompleted", "Backup completed successfully." },
                { "AllJobsCompleted", "All backup jobs completed." },
                { "BackupError", "Error during backup: " },
                { "Progress", "Progress" },
                { "ErrorCopyingFile", "Error copying file " },
                { "NoFilesToCopy", "No files needed to be copied (already up to date)." },
                { "ErrorOccurred", "An error occurred: " },
                { "PressAnyKeyToExit", "Press any key to exit..." },
                { "ErrorLoadingConfig", "Error loading configuration: " },
                { "ErrorSavingConfig", "Error saving configuration: " },
                { "OK", "OK" },
                { "Cancel", "Cancel" },
                { "FieldsCannotBeEmpty", "All fields must be filled." },
                { "ValidationError", "Validation Error" },
                { "Language", "Language:"},
                { "LogFormat", "Log Format:"},
                { "ServiceNotInitialized", "Backup service is not initialized. Please restart the application or check logs." },
                { "ServiceErrorTitle", "Service Error" },
                { "InitializationError", "Application Initialization Error" },
                { "Error", "Error" },
                { "ConfirmDeleteJob", "Are you sure you want to delete the job '{0}'?" },
                { "Confirmation", "Confirmation" },
                { "JobInterruptedWithMessage", "Job '{0}' interrupted: {1}"},
                { "BusinessSoftwarePreventingJob", "Backup job '{0}' cannot start because business software '{1}' is running." },
                 { "BusinessSoftwareDetectedForSome", "Business software is running that may affect some jobs." },
                 { "ContinueWithNonBlockedJobs", "Do you want to continue with jobs that are not blocked?" },
                { "OperationAborted", "Operation Aborted" },
                { "JobSkippedBusinessSoftware", "Job '{0}' skipped. Business software '{1}' is running." },
                { "ContinueWithOtherJobs", "Do you want to continue with other jobs?" },
                { "AllJobsExecutionCancelled", "Execution of all jobs has been cancelled by the user." },
                { "AllJobsCompletedWithIssues", "All jobs processed, but some had issues." },
                { "AllJobsCompletedSuccessfully", "All jobs completed successfully." },
                { "AllOperationsFinished", "All operations finished." },
                { "StartingAllJobs", "Starting execution of all backup jobs..." },
                { "ErrorDuringAllJobsExecution", "An error occurred during the execution of all jobs" },
                { "ExecutionErrorTitle", "Execution Error" },
                { "GenericErrorDuringAllJobs", "A generic error occurred while processing all jobs: " },
                { "UnknownJob", "Unknown Job" },
                { "Initializing", "Initializing..." },
                { "BackupStateInactive", "Backup job is inactive." },
                { "StartAllBackupsOperation", "Cannot start all backups" },
                { "AddJobOperation", "Cannot add backup job" },
                { "EditJobOperation", "Cannot edit backup job" },
                { "DeleteJobOperation", "Cannot delete backup job" },
                { "ExecuteSingleJobOperation", "Cannot execute selected job" },
                { "ExecuteAllJobsOperation", "Cannot execute all jobs" },
                { "JobAlreadyRunning", "This job is already running or paused."},
                { "Info", "Information"},
                // Status messages for BackupJobViewModel
                { "StatusPaused", "Paused" },
                { "StatusStopped", "Stopped" },
                { "StatusReady", "Ready" },
                { "StatusCompleted", "Completed" },
                { "StatusError", "Error" },
                { "StatusInterrupted", "Interrupted (Business Software)" },
                { "StatusActive", "Active: {0}% - {1}" }, // {0} = percentage, {1} = current file
                { "StatusCancelledOrNotRun", "Cancelled / Not Run"},
                { "NoPriority", "No Priority"},
                { "NoEncryption", "No Encryption"},
                { "BusinessSoftwareDetected", "Business Software Detected"},

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
                { "SelectBackupType", "Type de Sauvegarde :" }, // Keep for consistency if used elsewhere
                { "SelectTypePrio", "Extension prioritaire :" },
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
                { "ExecuteAllJobs", "Exécuter Toutes les Tâches (Seq)" }, // Clarified
                { "ExecuteAllJobsParallel", "Exécuter Toutes les Tâches (Parallèle)"},
                { "EnterJobIndexToExecute", "Entrez l'index de la tâche à exécuter :" },
                { "ExecutingJob", "Exécution de la tâche : " },
                { "SourceDirNotFound", "Répertoire source introuvable." },
                { "BackupCompleted", "Sauvegarde terminée avec succès." },
                { "AllJobsCompleted", "Toutes les sauvegardes sont terminées." },
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
                { "LogFormat", "Format des Logs :"},
                { "ServiceNotInitialized", "Le service de sauvegarde n'est pas initialisé. Veuillez redémarrer l'application ou vérifier les journaux." },
                { "ServiceErrorTitle", "Erreur de Service" },
                { "InitializationError", "Erreur d'Initialisation de l'Application" },
                { "Error", "Erreur" },
                { "ConfirmDeleteJob", "Êtes-vous sûr de vouloir supprimer la tâche '{0}' ?" },
                { "Confirmation", "Confirmation" },
                { "JobInterruptedWithMessage", "Tâche '{0}' interrompue : {1}"},
                { "BusinessSoftwarePreventingJob", "La tâche de sauvegarde '{0}' ne peut pas démarrer car le logiciel métier '{1}' est en cours d'exécution." },
                { "BusinessSoftwareDetectedForSome", "Un logiciel métier est en cours d'exécution et pourrait affecter certaines tâches." },
                { "ContinueWithNonBlockedJobs", "Voulez-vous continuer avec les tâches non bloquées ?" },
                { "OperationAborted", "Opération Annulée" },
                { "JobSkippedBusinessSoftware", "Tâche '{0}' ignorée. Le logiciel métier '{1}' est en cours d'exécution." },
                { "ContinueWithOtherJobs", "Voulez-vous continuer avec les autres tâches ?" },
                { "AllJobsExecutionCancelled", "L'exécution de toutes les tâches a été annulée par l'utilisateur." },
                { "AllJobsCompletedWithIssues", "Toutes les tâches ont été traitées, mais certaines ont eu des problèmes." },
                { "AllJobsCompletedSuccessfully", "Toutes les tâches terminées avec succès." },
                { "AllOperationsFinished", "Toutes les opérations sont terminées." },
                { "StartingAllJobs", "Démarrage de l'exécution de toutes les tâches de sauvegarde..." },
                { "ErrorDuringAllJobsExecution", "Une erreur s'est produite lors de l'exécution de toutes les tâches" },
                { "ExecutionErrorTitle", "Erreur d'Exécution" },
                { "GenericErrorDuringAllJobs", "Une erreur générique s'est produite lors du traitement de toutes les tâches : " },
                { "UnknownJob", "Tâche Inconnue" },
                { "Initializing", "Initialisation..." },
                { "BackupStateInactive", "La tâche de sauvegarde est inactive." },
                { "StartAllBackupsOperation", "Impossible de démarrer toutes les sauvegardes" },
                { "AddJobOperation", "Impossible d'ajouter la tâche de sauvegarde" },
                { "EditJobOperation", "Impossible de modifier la tâche de sauvegarde" },
                { "DeleteJobOperation", "Impossible de supprimer la tâche de sauvegarde" },
                { "ExecuteSingleJobOperation", "Impossible d'exécuter la tâche sélectionnée" },
                { "ExecuteAllJobsOperation", "Impossible d'exécuter toutes les tâches" },
                { "JobAlreadyRunning", "Cette tâche est déjà en cours d'exécution ou en pause."},
                { "Info", "Information"},
                // Status messages for BackupJobViewModel
                { "StatusPaused", "En pause" },
                { "StatusStopped", "Arrêté" },
                { "StatusReady", "Prêt" },
                { "StatusCompleted", "Terminé" },
                { "StatusError", "Erreur" },
                { "StatusInterrupted", "Interrompu (Logiciel Métier)" },
                { "StatusActive", "Actif : {0}% - {1}" }, // {0} = percentage, {1} = current file
                { "StatusCancelledOrNotRun", "Annulé / Non exécuté"},
                { "NoPriority", "Aucune Priorité"},
                { "NoEncryption", "Aucun Chiffrement"},
                { "BusinessSoftwareDetected", "Logiciel Métier Détecté"},
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
            catch (Exception ex)
            {
                // Fallback to English if system language detection fails
                _currentLanguage = "en";
                Debug.WriteLine($"[LanguageManager] Error detecting system language: {ex.Message}. Defaulting to 'en'.");
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
                Debug.WriteLine($"[LanguageManager] Key '{key}' not found for language '{_currentLanguage}'. Fell back to English.");
                return englishTranslation; // Return English translation
            }

            Debug.WriteLine($"[LanguageManager] Key '{key}' not found for language '{_currentLanguage}' or English. Returning key itself.");
            return key; // Return the key itself if not found anywhere
        }

        // Overload for GetString with specific language, useful for initial checks or tests
        public static string GetString(string key, string languageCode)
        {
            if (_translations == null) Initialize();

            if (_translations.TryGetValue(languageCode, out var translations) &&
                translations.TryGetValue(key, out string translation))
            {
                return translation;
            }
            return key; // Fallback to key if not found for the specified language
        }


        public static void SetLanguage(string language)
        {
            if (_translations == null) Initialize(); // Ensure initialized

            if (!string.IsNullOrEmpty(language) && _translations.ContainsKey(language))
            {
                _currentLanguage = language;
            }
            else
            {
                Debug.WriteLine($"[LanguageManager] Attempted to set invalid or unsupported language: '{language}'. Current language remains '{_currentLanguage}'.");
            }
        }

        public static string GetCurrentLanguage()
        {
            return _currentLanguage;
        }
    }
}