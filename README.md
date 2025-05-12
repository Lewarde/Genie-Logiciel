# 💾 EasySave – Livrable 1

## 🎓 Contexte pédagogique

Projet réalisé dans le cadre du **module Génie Logiciel**  
Groupe : **Hamza, Adam, Thomas, Ethan**  
Encadré par : **DSI de ProSoft**

---

## 🏢 Présentation de ProSoft et du projet

Nous avons intégré l’équipe de développement de **ProSoft**, éditeur de logiciels professionnels, pour concevoir une première version fonctionnelle d’un logiciel de sauvegarde : **EasySave**.

Le projet est encadré par le DSI de ProSoft, avec des exigences fortes en termes de **qualité logicielle**, **documentation**, **maintenabilité du code** et **gestion de versions**.  
EasySave s’inscrit dans la suite logicielle de l’entreprise et doit être **commercialisable**, avec un **prix unitaire de 200 € HT** et un **contrat de maintenance annuel** basé sur l’indice SYNTEC.

---

## 🎯 Objectif du livrable 1

Ce livrable correspond à la **version 1.0** d’EasySave, développée en **C#** avec **.NET 8**, sous forme d’une **application console**.

Fonctionnalités attendues :
- Création jusqu’à **5 travaux de sauvegarde**
- Sauvegardes de type **complète** ou **différentielle**
- Exécution possible via **ligne de commande**
- Sauvegarde depuis/vers :
  - Disques **locaux**
  - Disques **externes**
  - Lecteurs **réseaux**
- Journalisation des actions dans un **fichier log JSON**
- Suivi en temps réel via un **fichier d’état JSON**
- Compatibilité avec une future version graphique (MVVM)

---

## ⚙️ Contraintes techniques

- 🔧 **Langage** : C#  
- 🏗️ **Framework** : .NET 8.0  
- 💻 **IDE** : Visual Studio 2022  
- 📂 **Versioning** : GitHub  
- 📐 **UML** : lucidshart  
- 🌐 **Langues** : Interface bilingue (Français / Anglais)

Le code doit respecter les **bonnes pratiques de développement** :
- Aucune redondance
- Fonctions de taille raisonnable
- Commentaires et noms en anglais
- Architecture claire et maintenable

---

## 📊 Modélisation UML

Quatre diagrammes UML ont été produits pour guider la conception :
- **Diagramme de cas d’utilisation** : interactions entre l’utilisateur et le système
- **Diagramme de classes** : structure interne du logiciel
- **Diagramme d’activités** : parcours logique de l’utilisateur
- **Diagramme de séquence** : exécution d’un scénario métier

Chaque diagramme est expliqué dans la suite du livrable.

---

## 📁 Structure du projet

Le projet EasySave est organisé en plusieurs composants :
- `Program` : point d’entrée
- `ApplicationManager` : gestion centrale
- `BackupManager`, `BackupJob` : gestion des sauvegardes
- `LogManager`, `StateManager` : journalisation et suivi
- `LanguageManager` : gestion multilingue

Les fichiers de log et d’état sont générés au format **JSON** dans des emplacements compatibles avec les serveurs clients (hors `C:\temp\`).

---

## 📘 Remarques

- Un **manuel utilisateur (1 page)** et une **documentation technique** sont prévus.
- Le système de log est implémenté dans une **DLL réutilisable**.
- Le projet respecte les consignes de **modularité, maintenabilité et évolutivité** imposées par ProSoft.
- Une **version 2.0 avec interface graphique** (architecture MVVM) est envisagée si le prototype console donne satisfaction.

---

