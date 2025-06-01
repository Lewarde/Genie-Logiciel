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
            labelTypePrio.Text = LanguageManager.GetString("SelectTypePrio");
            buttonOK.Content = LanguageManager.GetString("OK");
            buttonCancel.Content = LanguageManager.GetString("Cancel");
        }

        private void InitializeComboBoxes()
        {
            // ComboBox pour le type de sauvegarde

            comboBoxTypeFilePrio.Items.Clear();
            comboBoxTypeFile.Items.Clear();

            // Ajouter "Aucune priorité" comme première option
            comboBoxTypeFilePrio.Items.Add(new ComboBoxItemPriorityViewModel { DisplayName = LanguageManager.GetString("NoPriority"), Value = PriorityFileExtension.Null });

            foreach (PriorityFileExtension prio in Enum.GetValues(typeof(PriorityFileExtension)))
            {
                if (prio != PriorityFileExtension.Null)
                {
                    comboBoxTypeFilePrio.Items.Add(new ComboBoxItemPriorityViewModel { DisplayName = "." + prio.ToString().ToLower(), Value = prio });
                }
            }
            comboBoxTypeFilePrio.DisplayMemberPath = "DisplayName";

            // Sélectionner l'élément actuel du ViewModel pour la priorité
            var currentPrioItem = comboBoxTypeFilePrio.Items.Cast<ComboBoxItemPriorityViewModel>()
                .FirstOrDefault(item => item.Value == _viewModel.Priority);
            if (currentPrioItem != null)
            {
                comboBoxTypeFilePrio.SelectedItem = currentPrioItem;
            }
            else if (comboBoxTypeFilePrio.Items.Count > 0)
            {
                comboBoxTypeFilePrio.SelectedIndex = 0;
                _viewModel.Priority = ((ComboBoxItemPriorityViewModel)comboBoxTypeFilePrio.SelectedItem).Value;
            }

            // Ajouter "Aucun" ou "Pas de chiffrement" comme première option
            comboBoxTypeFile.Items.Add(new ComboBoxItemViewModel { DisplayName = LanguageManager.GetString("NoEncryption"), Value = EncryptionFileExtension.Null });

            foreach (EncryptionFileExtension ext in Enum.GetValues(typeof(EncryptionFileExtension)))
            {
                if (ext != EncryptionFileExtension.Null)
                {
                    comboBoxTypeFile.Items.Add(new ComboBoxItemViewModel { DisplayName = "." + ext.ToString().ToLower(), Value = ext });
                }
            }
            comboBoxTypeFile.DisplayMemberPath = "DisplayName";

            // Sélectionner l'élément actuel du ViewModel pour le chiffrement
            var currentExtensionItem = comboBoxTypeFile.Items.Cast<ComboBoxItemViewModel>()
                .FirstOrDefault(item => item.Value == _viewModel.FileExtension);
            if (currentExtensionItem != null)
            {
                comboBoxTypeFile.SelectedItem = currentExtensionItem;
            }
            else if (comboBoxTypeFile.Items.Count > 0)
            {
                comboBoxTypeFile.SelectedIndex = 0;
                _viewModel.FileExtension = ((ComboBoxItemViewModel)comboBoxTypeFile.SelectedItem).Value;
            }
        }


        private void comboBoxTypeFilePrio_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxTypeFilePrio.SelectedItem is ComboBoxItemPriorityViewModel selectedItem)
            {
                _viewModel.Priority = selectedItem.Value;
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

    public class ComboBoxItemViewModel
    {
        public string DisplayName { get; set; }
        public EncryptionFileExtension Value { get; set; }
    }

    public class ComboBoxItemPriorityViewModel
    {
        public string DisplayName { get; set; }
        public PriorityFileExtension Value { get; set; }
    }

}