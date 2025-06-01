using System;
using System.Linq; // Added for .Cast<>() and .ToList()
using System.Windows;
using System.Windows.Controls;
using EasySave.Models; // For BackupType enum, and now PriorityFileExtension, EncryptionFileExtension
using EasySave.ViewModels;
using EasySave.Utils;

namespace EasySave.Wpf.Views
{
    public partial class EditBackupJobWindow : Window
    {
        private BackupJobViewModel _viewModel; // Holds the data and logic for this window

        public EditBackupJobWindow(BackupJobViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            this.DataContext = _viewModel; // Connects the XAML (view) to this ViewModel
            LocalizeUI(); // Set text for UI elements based on current language
            InitializeComboBoxes(); // Setup the dropdown lists (ComboBoxes)
        }

        private void LocalizeUI()
        {
            // Set the window title: "Create Backup Job" if it's a new job, "Modify Backup Job" if editing
            this.Title = LanguageManager.GetString(string.IsNullOrEmpty(_viewModel.Name) ? "CreateBackupJob" : "ModifyBackupJob");
            labelName.Text = LanguageManager.GetString("EnterJobName");
            labelSource.Text = LanguageManager.GetString("EnterSourceDir");
            labelTarget.Text = LanguageManager.GetString("EnterTargetDir");
            labelTypePrio.Text = LanguageManager.GetString("SelectTypePrio"); // This label seems to cover both Type and Priority now
            buttonOK.Content = LanguageManager.GetString("OK");
            buttonCancel.Content = LanguageManager.GetString("Cancel");
        }

        private void InitializeComboBoxes()
        {
            // ComboBox for priority file extensions
            comboBoxTypeFilePrio.Items.Clear(); // Clear any existing items first
            comboBoxTypeFile.Items.Clear();     // Clear items for encryption type ComboBox

            // Add "No Priority" as the first option for priority ComboBox
            comboBoxTypeFilePrio.Items.Add(new ComboBoxItemPriorityViewModel { DisplayName = LanguageManager.GetString("NoPriority"), Value = PriorityFileExtension.Null });

            // Add all priority file extensions from the enum (except Null, which is already added)
            foreach (PriorityFileExtension prio in Enum.GetValues(typeof(PriorityFileExtension)))
            {
                if (prio != PriorityFileExtension.Null) // Don't add the 'Null' value again
                {
                    comboBoxTypeFilePrio.Items.Add(new ComboBoxItemPriorityViewModel { DisplayName = "." + prio.ToString().ToLower(), Value = prio });
                }
            }
            comboBoxTypeFilePrio.DisplayMemberPath = "DisplayName"; // Tell ComboBox which property to show in the list

            // Select the current priority from the ViewModel in the ComboBox
            var currentPrioItem = comboBoxTypeFilePrio.Items.Cast<ComboBoxItemPriorityViewModel>()
                .FirstOrDefault(item => item.Value == _viewModel.Priority);
            if (currentPrioItem != null)
            {
                comboBoxTypeFilePrio.SelectedItem = currentPrioItem;
            }
            else if (comboBoxTypeFilePrio.Items.Count > 0) // If no match, select the first item (e.g., "No Priority")
            {
                comboBoxTypeFilePrio.SelectedIndex = 0;
                _viewModel.Priority = ((ComboBoxItemPriorityViewModel)comboBoxTypeFilePrio.SelectedItem).Value; // Update ViewModel
            }

            // ComboBox for encryption file extensions
            // Add "No Encryption" as the first option
            comboBoxTypeFile.Items.Add(new ComboBoxItemViewModel { DisplayName = LanguageManager.GetString("NoEncryption"), Value = EncryptionFileExtension.Null });

            // Add all encryption file extensions from the enum (except Null)
            foreach (EncryptionFileExtension ext in Enum.GetValues(typeof(EncryptionFileExtension)))
            {
                if (ext != EncryptionFileExtension.Null)
                {
                    comboBoxTypeFile.Items.Add(new ComboBoxItemViewModel { DisplayName = "." + ext.ToString().ToLower(), Value = ext });
                }
            }
            comboBoxTypeFile.DisplayMemberPath = "DisplayName"; // Tell ComboBox which property to show

            // Select the current encryption extension from the ViewModel in the ComboBox
            var currentExtensionItem = comboBoxTypeFile.Items.Cast<ComboBoxItemViewModel>()
                .FirstOrDefault(item => item.Value == _viewModel.FileExtension);
            if (currentExtensionItem != null)
            {
                comboBoxTypeFile.SelectedItem = currentExtensionItem;
            }
            else if (comboBoxTypeFile.Items.Count > 0) // If no match, select the first item (e.g., "No Encryption")
            {
                comboBoxTypeFile.SelectedIndex = 0;
                _viewModel.FileExtension = ((ComboBoxItemViewModel)comboBoxTypeFile.SelectedItem).Value; // Update ViewModel
            }
        }

        // Event handler for when priority selection changes
        private void comboBoxTypeFilePrio_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxTypeFilePrio.SelectedItem is ComboBoxItemPriorityViewModel selectedItem)
            {
                _viewModel.Priority = selectedItem.Value; // Update ViewModel with the new priority
            }
        }

        // Event handler for when encryption type selection changes
        private void comboBoxTypeFile_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxTypeFile.SelectedItem is ComboBoxItemViewModel selectedItem)
            {
                _viewModel.FileExtension = selectedItem.Value; // Update ViewModel with the new encryption extension
            }
        }

        // Event handler for OK button click
        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            // Basic validation: check if essential fields are filled
            if (string.IsNullOrWhiteSpace(_viewModel.Name) ||
                string.IsNullOrWhiteSpace(_viewModel.SourceDirectory) ||
                string.IsNullOrWhiteSpace(_viewModel.TargetDirectory))
            {
                System.Windows.MessageBox.Show(
                    LanguageManager.GetString("FieldsCannotBeEmpty"), // "Fields cannot be empty."
                    LanguageManager.GetString("ValidationError"),     // "Validation Error"
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return; // Stop and don't close the window
            }
            this.DialogResult = true; // Signal that the user confirmed the dialog
            this.Close(); // Close the window
        }

        // Event handler for Cancel button click
        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false; // Signal that the user cancelled the dialog
            this.Close(); // Close the window
        }

        // Event handler for Browse Source Directory button
        private void buttonBrowseSource_Click(object sender, RoutedEventArgs e)
        {
            // Use Windows Forms FolderBrowserDialog (WPF doesn't have a built-in one)
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                var result = dialog.ShowDialog(); // Show the folder selection dialog
                if (result == System.Windows.Forms.DialogResult.OK) // If user selected a folder and clicked OK
                {
                    _viewModel.SourceDirectory = dialog.SelectedPath; // Update ViewModel with selected path
                }
            }
        }

        // Event handler for Browse Target Directory button
        private void buttonBrowseTarget_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    _viewModel.TargetDirectory = dialog.SelectedPath; // Update ViewModel with selected path
                }
            }
        }
    }

    // Helper class to display encryption options in a ComboBox
    public class ComboBoxItemViewModel
    {
        public string DisplayName { get; set; } // Text shown in the ComboBox
        public EncryptionFileExtension Value { get; set; } // The actual enum value
    }

    // Helper class to display priority options in a ComboBox
    public class ComboBoxItemPriorityViewModel
    {
        public string DisplayName { get; set; } // Text shown in the ComboBox
        public PriorityFileExtension Value { get; set; } // The actual enum value
    }
}