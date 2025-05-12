EasySave – Outil de sauvegarde de fichiers (v1.0)
EasySave est une application console développée en .NET Core, conçue pour automatiser la sauvegarde de fichiers et de dossiers, avec gestion de logs, suivi en temps réel, et support multilingue (français / anglais).
Cette première version respecte un cahier des charges rigoureux, en vue d'une future évolution vers une version graphique (v2.0 - MVVM).

🛠 Fonctionnalités principales
Gestion de jusqu’à 5 travaux de sauvegarde.

Chaque travail est défini par :

Un nom

Un répertoire source

Un répertoire cible

Un type de sauvegarde :

Complète

Différentielle

Exécution manuelle ou automatique de sauvegardes via ligne de commande :

1-3 : exécute les sauvegardes 1 à 3 séquentiellement

1;3 : exécute les sauvegardes 1 et 3

Support de toutes sources de stockage : disque local, externe ou réseau

Multilingue : messages disponibles en français 🇫🇷 et en anglais 🇬🇧

Suivi d’état en temps réel des sauvegardes (format JSON)

Fichier de log journalier avec toutes les opérations effectuées (format JSON)

📁 Format des fichiers générés
🧾 Fichier log (journalier)
Chaque action réalisée durant la sauvegarde est enregistrée :

Format : JSON (avec retour à la ligne pour chaque élément)

Contenu :

Horodatage

Nom du travail de sauvegarde

Chemin complet source (UNC)

Chemin complet destination (UNC)

Taille du fichier

Durée de transfert (ms) — négatif si erreur

📌 Ex : 2025-05-12.json

json
Copier
Modifier
{
  "Timestamp": "2025-05-12T15:42:10",
  "JobName": "DailyBackup",
  "SourceFile": "\\\\PC\\Documents\\report.docx",
  "TargetFile": "\\\\Backup\\Reports\\report.docx",
  "FileSize": 1048576,
  "TransferTimeMs": 345
}
📊 Fichier d’état temps réel
Mis à jour en temps réel, ce fichier stocke l’état actuel de chaque sauvegarde :

Format : JSON

Contenu minimum :

Nom du travail

Horodatage de la dernière action

État (Actif, Terminé, Erreur…)

Nombre total de fichiers

Taille totale

Nombre et taille des fichiers restants

Fichier en cours (source et cible)

📌 Ex : state.json

json
Copier
Modifier
{
  "JobName": "DailyBackup",
  "Timestamp": "2025-05-12T15:42:11",
  "State": "Active",
  "TotalFilesCount": 10,
  "TotalFilesSize": 104857600,
  "RemainingFilesCount": 5,
  "RemainingFilesSize": 52428800,
  "CurrentSourceFile": "\\\\PC\\Documents\\image.jpg",
  "CurrentTargetFile": "\\\\Backup\\Images\\image.jpg"
}
⚙️ Architecture du projet
EasySave.Models : Définitions des modèles (BackupJob, LogEntry…)

EasySave.Services :

IBackupManager / BackupManager : gestion des sauvegardes

IMenuManager / MenuManager : interface utilisateur console

ICommandParser / CommandParser : interprétation des commandes utilisateur

Logger.dll : librairie dédiée à la journalisation, versionnable et réutilisable

EasySave.Utils : gestion multilingue, outils divers

🚀 Utilisation
Compilation
bash
Copier
Modifier
dotnet build
Lancement
bash
Copier
Modifier
dotnet run
Exemple en ligne de commande
bash
Copier
Modifier
dotnet run -- 1-3
dotnet run -- 1;3

📌 Contraintes spécifiques
Aucun fichier de log ou d’état ne doit être stocké dans C:\Temp\ : chemins dynamiques et compatibles avec un environnement serveur.

Tous les fichiers de configuration ou état doivent être en JSON avec retour à la ligne par élément.

La librairie Logger.dll doit être maintenable et compatible avec les évolutions du logiciel.

📈 Évolutions futures prévues
Version 2.0 avec une interface graphique (WPF) suivant le pattern MVVM

Ajout de filtrage de fichiers, planification des sauvegardes, notifications, etc.

🧑‍💻 Auteurs
Projet réalisé dans le cadre d’un exercice de conception logicielle.
Adam ADJEROUD
Hamza HANi
Thomas HALLIEZ
Ethan-maris KAMOGNE DOMGUIA
Développement en .NET Core, C#.
