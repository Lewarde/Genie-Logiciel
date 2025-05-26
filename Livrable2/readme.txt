# EasySave V2 - Logiciel de Sauvegarde

EasySave V2 est une application de bureau conçue pour simplifier et automatiser la création de travaux de sauvegarde de fichiers et de dossiers. Elle offre une interface utilisateur intuitive pour gérer plusieurs travaux de sauvegarde, exécuter des sauvegardes complètes ou différentielles, et suivre la progression.

## Fonctionnalités Principales

*   **Gestion des Travaux de Sauvegarde :**
    *   Créer, modifier et supprimer des travaux de sauvegarde.
    *   Définir un nom, un répertoire source, un répertoire cible et un type de sauvegarde (Complète ou Différentielle) pour chaque travail.
*   **Exécution des Sauvegardes :**
    *   Exécuter un travail de sauvegarde sélectionné.
    *   Exécuter tous les travaux de sauvegarde configurés en séquence.
*   **Suivi et Journalisation :**
    *   Affichage de la progression des sauvegardes en temps réel.
    *   Journalisation des opérations de sauvegarde (fichiers copiés, temps de transfert, erreurs) dans des fichiers log au format JSON ou XML.
    *   Sauvegarde de l'état d'avancement des travaux pour une reprise ou un suivi.
*   **Internationalisation :**
    *   Interface utilisateur disponible en plusieurs langues (Anglais, Français).
*   **Chiffrement (Optionnel) :**
    *   Possibilité de chiffrer certains types de fichiers pendant la sauvegarde en utilisant l'outil externe CryptoSoft.
*   **Détection de Logiciel Métier :**
    *   Empêche le démarrage des sauvegardes si un logiciel métier spécifié (par son chemin d'accès) est en cours d'exécution.

## Prérequis

*   .NET Framework (la version exacte dépendra de votre projet, par exemple .NET Framework 4.7.2 ou .NET 6/7/8 si c'est du .NET Core/5+ pour WPF).
*   (Optionnel) CryptoSoft : Si vous souhaitez utiliser la fonctionnalité de chiffrement.

## Configuration Initiale

### 1. Chemin de CryptoSoft (Important !)

Si vous prévoyez d'utiliser la fonctionnalité de chiffrement des fichiers, vous **devez** configurer le chemin d'accès correct vers l'exécutable de `CryptoSoft.exe`.

*   Ouvrez le fichier : `EasySave.Services/CryptoSoft/EncryptionService.cs`
*   Localisez la ligne suivante dans le constructeur `EncryptionService()` ou comme une constante/variable de classe :
    ```csharp
    // Exemple de ce que vous pourriez avoir :
    private const string CryptoSoftPath = @"C:\Chemin\Vers\CryptoSoft\CryptoSoft.exe"; 
    // OU
    // _cryptoSoftPath = ConfigurationManager.AppSettings["CryptoSoftPath"] ?? @"C:\Default\Path\To\CryptoSoft.exe";
    ```
*   **Modifiez cette ligne** pour qu'elle pointe vers l'emplacement réel de `CryptoSoft.exe` sur votre système. Par exemple :
    ```csharp
    private const string CryptoSoftPath = @"D:\Outils\CryptoSoft\CryptoSoft.exe";
    ```
*   **Sauvegardez le fichier.**

**Note :** Si le chemin de CryptoSoft n'est pas correctement configuré, la fonctionnalité de chiffrement échouera.

### 2. (Optionnel) Configuration du Logiciel Métier

Dans l'interface principale d'EasySave, vous pouvez spécifier le chemin complet vers un exécutable de "Logiciel Métier". Si ce logiciel est détecté comme étant en cours d'exécution, EasySave empêchera le démarrage de nouveaux travaux de sauvegarde.

## Utilisation

1.  Lancez l'application `EasySaveV2corrigé.exe` (ou le nom de votre exécutable).
2.  **Langue et Format de Log :** Sélectionnez votre langue préférée et le format de log désiré (JSON ou XML) en utilisant les menus déroulants. Ces paramètres sont sauvegardés.
3.  **Logiciel Métier :** Entrez le chemin complet vers l'exécutable du logiciel métier à surveiller (ex: `C:\Windows\System32\calc.exe`).
4.  **Créer un Travail de Sauvegarde :**
    *   Cliquez sur "Créer un travail de sauvegarde".
    *   Remplissez le nom, les répertoires source et cible, et choisissez le type de sauvegarde (Complète/Différentielle).
    *   (Optionnel) Spécifiez une extension de fichier à chiffrer.
    *   Cliquez sur "Sauvegarder".
5.  **Modifier/Supprimer :** Sélectionnez un travail dans la liste et utilisez les boutons correspondants.
6.  **Exécuter :**
    *   Sélectionnez un travail et cliquez sur "Exécuter le travail sélectionné".
    *   Cliquez sur "Exécuter tous les travaux" pour lancer toutes les sauvegardes.
7.  La barre d'état en bas affichera la progression et les messages.

## Fichiers Générés

*   **Logs :** Stockés dans `%LocalAppData%\EasySave\Logs\` dans des fichiers quotidiens (format JSON ou XML).
*   **États :** Stockés dans `%LocalAppData%\EasySave\States\` pour suivre la progression des travaux.
*   **Configuration :** Les travaux de sauvegarde et les paramètres de l'application sont sauvegardés dans `%LocalAppData%\EasySave\Config\`.

## Contribuer

Les contributions sont les bienvenues ! Veuillez suivre les directives de contribution standard (fork, branche, pull request).

## Licence

Ce projet est sous licence [NOM DE VOTRE LICENCE - ex: MIT, GPLv3, etc.]. Voir le fichier `LICENSE` pour plus de détails (si vous en avez un).