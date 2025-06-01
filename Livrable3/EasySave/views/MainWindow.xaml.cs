using System;
using System.Windows;
using System.Windows.Controls; // Needed for ListBox, TextBlock, etc.
using EasySave.ViewModels;
using EasySave.Utils;
using System.ComponentModel; // Needed for PropertyChanged
using System.Windows.Media.Imaging; // Seems unused in this file, might be for XAML.

namespace EasySave.Wpf.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel; // This will hold the main logic and data for our window

        public MainWindow()
        {
            InitializeComponent(); // This is important, it loads the XAML part of the window
            _viewModel = new MainViewModel(); // Create an instance of our MainViewModel
            this.DataContext = _viewModel; // Connect the XAML to our ViewModel, so buttons and lists can use its data
            this.Loaded += MainWindow_Loaded; // Run MainWindow_Loaded method when the window is fully loaded
        }

        // This method runs after the window has finished loading
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitializeAsync(); // Do some initial setup in the ViewModel, like loading jobs
            _viewModel.PropertyChanged += ViewModel_PropertyChanged; // Listen for changes in ViewModel properties (like language)
            LocalizeStaticUI(); // Set the text for buttons and labels based on the current language
        }

        // This method is called when a property in the ViewModel changes
        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // If the CurrentLanguage property changed, we need to update the UI text
            if (e.PropertyName == nameof(MainViewModel.CurrentLanguage))
            {
                LocalizeStaticUI();
            }
        }

        // This method updates the text of UI elements (buttons, labels) to the current language
        private void LocalizeStaticUI()
        {
            this.Title = LanguageManager.GetString("WelcomeMessage"); // Set window title
            labelJobsHeader.Text = LanguageManager.GetString("BackupJobs");
            buttonAddJob.Content = LanguageManager.GetString("CreateBackupJob");
            buttonEditJob.Content = LanguageManager.GetString("ModifyBackupJob");
            buttonDeleteJob.Content = LanguageManager.GetString("DeleteBackupJob");
            buttonExecuteSelected.Content = LanguageManager.GetString("ExecuteSingleJob");
            buttonExecuteAll.Content = LanguageManager.GetString("ExecuteAllJobs");
            labelLanguage.Text = LanguageManager.GetString("Language") + ":";
            labelLogFormat.Text = LanguageManager.GetString("LogFormat") + ":";
            labelBusinessSoftware.Text = LanguageManager.GetString("BusinessSoftware") + ":";
        }

        // This method is called when the user selects an item in the backup jobs list
        private void listBoxBackupJobs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Check if the selected item is a BackupJobViewModel
            if (listBoxBackupJobs.SelectedItem is BackupJobViewModel selectedVm)
            {
                _viewModel.SelectedBackupJob = selectedVm; // Tell the ViewModel which job is selected
            }
            else // If nothing is selected (e.g., after deleting a job)
            {
                _viewModel.SelectedBackupJob = null; // Tell the ViewModel no job is selected
            }
        }

        // This method is called when the "Add Job" button is clicked
        private async void buttonAddJob_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.AddBackupJobAsync(this); // Ask the ViewModel to handle adding a new job
                                                      // 'this' passes the current window, so the ViewModel can open a dialog on top of it
        }

        // This method is called when the "Edit Job" button is clicked
        private async void buttonEditJob_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedBackupJob != null) // Make sure a job is actually selected
            {
                await _viewModel.EditBackupJobAsync(this); // Ask ViewModel to handle editing
            }
            else
            {
                // Show a message if no job is selected
                System.Windows.MessageBox.Show(
                    LanguageManager.GetString("InvalidJobIndex"),      // Message: "Please select a job."
                    LanguageManager.GetString("SelectionErrorTitle"),  // Title: "Selection Error"
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        // This method is called when the "Delete Job" button is clicked
        private async void buttonDeleteJob_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedBackupJob != null) // Make sure a job is selected
            {
                await _viewModel.DeleteBackupJobAsync(); // Ask ViewModel to handle deletion
            }
            else
            {
                // Show a message if no job is selected
                System.Windows.MessageBox.Show(
                   LanguageManager.GetString("InvalidJobIndex"),
                   LanguageManager.GetString("SelectionErrorTitle"),
                   MessageBoxButton.OK,
                   MessageBoxImage.Warning);
            }
        }

        // This method is called when the "Execute Selected Job" button is clicked
        private async void buttonExecuteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedBackupJob != null) // Make sure a job is selected
            {
                await _viewModel.ExecuteSelectedJobAsync(); // Ask ViewModel to run the selected job
            }
            else
            {
                // Show a message if no job is selected
                System.Windows.MessageBox.Show(
                    LanguageManager.GetString("InvalidJobIndex"),
                    LanguageManager.GetString("SelectionErrorTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        // This method is called when the "Execute All Jobs" button is clicked
        private async void buttonExecuteAll_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.ExecuteAllJobsAsync(); // Ask ViewModel to run all jobs
        }
    }
}