using System;
using System.Windows;
using System.Windows.Controls;
using EasySave.ViewModels;
using EasySave.Utils; 
using System.ComponentModel; 

namespace EasySave.Wpf.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            this.DataContext = _viewModel; // Set DataContext for XAML Bindings
            this.Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitializeAsync(); // Loads data, languages, etc.

            // ViewModel PropertyChanged subscription for dynamic UI updates (e.g., language)
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            LocalizeStaticUI(); // Initial localization

            // Button enabled states are now primarily handled by AreControlsEnabled binding in XAML.
            // If ProgressBar visibility needs to be explicitly managed or other complex scenarios,
            // ViewModel_PropertyChanged can handle them.
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.CurrentLanguage))
            {
                LocalizeStaticUI();
            }
            // IsExecutingBackup changes are handled by AreControlsEnabled for button states
            // and direct binding for ProgressBar visibility (using a BooleanToVisibilityConverter).
        }

        // In MainWindow.xaml.cs
        private void LocalizeStaticUI()
        {
            this.Title = LanguageManager.GetString("WelcomeMessage"); // Or "EasySave Application"
            labelJobsHeader.Text = LanguageManager.GetString("BackupJobs");
            buttonAddJob.Content = LanguageManager.GetString("CreateBackupJob");
            buttonEditJob.Content = LanguageManager.GetString("ModifyBackupJob");
            buttonDeleteJob.Content = LanguageManager.GetString("DeleteBackupJob");
            buttonExecuteSelected.Content = LanguageManager.GetString("ExecuteSingleJob");
            buttonExecuteAll.Content = LanguageManager.GetString("ExecuteAllJobs");
            labelLanguage.Text = LanguageManager.GetString("Language") + ":";
            labelLogFormat.Text = LanguageManager.GetString("LogFormat") + ":";
            // Add localization for the new label
            labelBusinessSoftware.Text = LanguageManager.GetString("BusinessSoftware") + ":"; // Assuming "BusinessSoftware" is the key
        }
        private void listBoxBackupJobs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listBoxBackupJobs.SelectedItem is BackupJobViewModel selectedVm)
            {
                _viewModel.SelectedBackupJob = selectedVm; // This should already happen via binding
            }
        }

        private async void buttonAddJob_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.AddBackupJobAsync(this); // Pass owner window for dialogs
        }

        private async void buttonEditJob_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedBackupJob != null)
            {
                await _viewModel.EditBackupJobAsync(this); // Pass owner window
            }
            else
            {
                System.Windows.MessageBox.Show(
                    LanguageManager.GetString("InvalidJobIndex"),
                    "Selection Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private async void buttonDeleteJob_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedBackupJob != null)
            {
                // Consider adding a confirmation dialog here
                // var result = System.Windows.MessageBox.Show(
                //    string.Format(LanguageManager.GetString("ConfirmDeleteJob"), _viewModel.SelectedBackupJob.Name),
                //    LanguageManager.GetString("Confirmation"),
                //    MessageBoxButton.YesNo, MessageBoxImage.Warning);
                // if (result == MessageBoxResult.Yes)
                // {
                await _viewModel.DeleteBackupJobAsync();
                // }
            }
            else
            {
                System.Windows.MessageBox.Show(
                   LanguageManager.GetString("InvalidJobIndex"),
                   "Selection Error",
                   MessageBoxButton.OK,
                   MessageBoxImage.Warning);
            }
        }

        private async void buttonExecuteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedBackupJob != null)
            {
                await _viewModel.ExecuteSelectedJobAsync();
            }
            else
            {
                System.Windows.MessageBox.Show(
                    LanguageManager.GetString("InvalidJobIndex"),
                    "Selection Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private async void buttonExecuteAll_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.ExecuteAllJobsAsync();
        }
    }
}