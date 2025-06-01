// MainWindow.xaml.cs
using System;
using System.Windows;
using System.Windows.Controls; // Nécessaire pour ListBox, TextBlock, etc.
using EasySave.ViewModels;
using EasySave.Utils;
using System.ComponentModel;
using System.Windows.Media.Imaging;

namespace EasySave.Wpf.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            this.DataContext = _viewModel;
            this.Loaded += MainWindow_Loaded;

        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitializeAsync();
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            LocalizeStaticUI();
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.CurrentLanguage))
            {
                LocalizeStaticUI();
            }
        }

        private void LocalizeStaticUI()
        {
            this.Title = LanguageManager.GetString("WelcomeMessage");
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
        private void listBoxBackupJobs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listBoxBackupJobs.SelectedItem is BackupJobViewModel selectedVm)
            {
                _viewModel.SelectedBackupJob = selectedVm;
            }
            else // Si rien n'est sélectionné (par exemple, après une suppression)
            {
                _viewModel.SelectedBackupJob = null;
            }
        }

        private async void buttonAddJob_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.AddBackupJobAsync(this);
        }

        private async void buttonEditJob_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedBackupJob != null)
            {
                await _viewModel.EditBackupJobAsync(this);
            }
            else
            {
                System.Windows.MessageBox.Show(
                    LanguageManager.GetString("InvalidJobIndex"),
                    LanguageManager.GetString("SelectionErrorTitle"), // Ajout d'une clé pour le titre
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private async void buttonDeleteJob_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedBackupJob != null)
            {
                await _viewModel.DeleteBackupJobAsync();
            }
            else
            {
                System.Windows.MessageBox.Show(
                   LanguageManager.GetString("InvalidJobIndex"),
                   LanguageManager.GetString("SelectionErrorTitle"),
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
                    LanguageManager.GetString("SelectionErrorTitle"),
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