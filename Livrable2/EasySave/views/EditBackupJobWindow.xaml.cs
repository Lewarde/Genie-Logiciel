using System;
using System.Linq; // Ajout pour .Cast<>() et .ToList()
using System.Windows;
using System.Windows.Controls;
using EasySave.Models; // For BackupType enum
using EasySave.ViewModels;
using EasySave.Utils;

namespace EasySave.Wpf.Views
{
    public partial class EditBackupJobWindow : Window
    {
        private BackupJobViewModel _viewModel;

        public EditBackupJobWindow(BackupJobViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            this.DataContext = _viewModel; // Set DataContext for XAML bindings
            LocalizeUI();
            InitializeComboBoxes(); // Renommé pour plus de clarté
        }

        private void LocalizeUI()
        {
            this.Title = LanguageManager.GetString(string.IsNullOrEmpty(_viewModel.Name) ? "CreateBackupJob" : "ModifyBackupJob");
            labelName.Text = LanguageManager.GetString("EnterJobName");
            labelSource.Text = LanguageManager.GetString("EnterSourceDir");
            labelTarget.Text = LanguageManager.GetString("EnterTargetDir");
            labelType.Text = LanguageManager.GetString("SelectBackupType");
            // Assurez-vous d'avoir un label pour comboBoxTypeFile dans votre XAML, par exemple labelFileType.Text = "Type de fichier à chiffrer:"
            buttonOK.Content = LanguageManager.GetString("OK");
            buttonCancel.Content = LanguageManager.GetString("Cancel");
        }

        private void InitializeComboBoxes()
        {
            // ComboBox pour le type de sauvegarde
            comboBoxBackupType.Items.Clear();
            comboBoxBackupType.Items.Add(LanguageManager.GetString("FullBackup"));
            comboBoxBackupType.Items.Add(LanguageManager.GetString("DifferentialBackup"));

            if ((int)_viewModel.Type >= 0 && (int)_viewModel.Type < comboBoxBackupType.Items.Count)
            {
                comboBoxBackupType.SelectedIndex = (int)_viewModel.Type;
            }
            else if (comboBoxBackupType.Items.Count > 0)
            {
                comboBoxBackupType.SelectedIndex = 0;
                _viewModel.Type = (BackupType)0;
            }

            // ComboBox pour l'extension de fichier à chiffrer
            comboBoxTypeFile.Items.Clear();
            // Ajouter "Aucun" ou "Pas de chiffrement" comme première option
            comboBoxTypeFile.Items.Add(new ComboBoxItemViewModel { DisplayName = LanguageManager.GetString("NoEncryption"), Value = EncryptionFileExtension.Null });

            foreach (EncryptionFileExtension ext in Enum.GetValues(typeof(EncryptionFileExtension)))
            {
                if (ext != EncryptionFileExtension.Null) // Ne pas ajouter Null deux fois
                {
                    // Ajoute l'extension avec un point devant pour l'affichage
                    comboBoxTypeFile.Items.Add(new ComboBoxItemViewModel { DisplayName = "." + ext.ToString().ToLower(), Value = ext });
                }
            }
            comboBoxTypeFile.DisplayMemberPath = "DisplayName"; // Afficher le nom convivial

            // Sélectionner l'élément actuel du ViewModel
            var currentExtensionItem = comboBoxTypeFile.Items.Cast<ComboBoxItemViewModel>()
                                       .FirstOrDefault(item => item.Value == _viewModel.FileExtension);
            if (currentExtensionItem != null)
            {
                comboBoxTypeFile.SelectedItem = currentExtensionItem;
            }
            else if (comboBoxTypeFile.Items.Count > 0)
            {
                comboBoxTypeFile.SelectedIndex = 0; // Sélectionner "Aucun" par défaut
                _viewModel.FileExtension = ((ComboBoxItemViewModel)comboBoxTypeFile.SelectedItem).Value;
            }
        }


        private void comboBoxBackupType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxBackupType.SelectedIndex != -1)
            {
                _viewModel.Type = (BackupType)comboBoxBackupType.SelectedIndex;
            }
        }

        // Gestionnaire pour le ComboBox de type de fichier
        private void comboBoxTypeFile_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxTypeFile.SelectedItem is ComboBoxItemViewModel selectedItem)
            {
                _viewModel.FileExtension = selectedItem.Value;
            }
        }


        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_viewModel.Name) ||
                string.IsNullOrWhiteSpace(_viewModel.SourceDirectory) ||
                string.IsNullOrWhiteSpace(_viewModel.TargetDirectory))
            {
                System.Windows.MessageBox.Show(
                    LanguageManager.GetString("FieldsCannotBeEmpty"),
                    LanguageManager.GetString("ValidationError"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            this.DialogResult = true;
            this.Close();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void buttonBrowseSource_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    _viewModel.SourceDirectory = dialog.SelectedPath;
                }
            }
        }

        private void buttonBrowseTarget_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    _viewModel.TargetDirectory = dialog.SelectedPath;
                }
            }
        }
    }

    // Classe d'aide pour afficher des noms conviviaux dans le ComboBox
    // tout en conservant la valeur de l'énumération.
    public class ComboBoxItemViewModel
    {
        public string DisplayName { get; set; }
        public EncryptionFileExtension Value { get; set; }
    }
}