// Client/MainWindow.xaml.cs
using System;
using System.Collections.Generic; // Pour List
using System.Collections.ObjectModel; // Pour ObservableCollection
using System.ComponentModel; // Pour ClosingEventArgs
using System.Net.Sockets;
using System.Text;
using System.Text.Json; // Pour désérialiser
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Client
{
    public partial class MainWindow : Window
    {
        private Socket _clientSocket;
        private Thread _listenThread;
        private CancellationTokenSource _cancellationTokenSource;

        // Collection pour la ListView, qui se met à jour automatiquement dans l'UI
        public ObservableCollection<JobProgressInfo> JobProgressList { get; set; }

        // Options pour le désérialiseur JSON
        private static readonly JsonSerializerOptions _jsonDeserializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true // Utile si la casse du JSON ne correspond pas exactement
        };


        public MainWindow()
        {
            InitializeComponent();
            JobProgressList = new ObservableCollection<JobProgressInfo>();
            JobProgressListView.ItemsSource = JobProgressList; // Lier la ListView à la collection
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            ConnectButton.IsEnabled = false;
            StatusTextBlock.Text = "Connexion en cours...";

            // Exécuter la connexion sur un thread de tâche pour ne pas geler l'UI
            _clientSocket = await Task.Run(() => NetworkClient.ConnectToServer("127.0.0.1", 8080));

            if (_clientSocket != null && _clientSocket.Connected)
            {
                StatusTextBlock.Text = "Connecté au serveur !";
                DisconnectButton.IsEnabled = true;

                _cancellationTokenSource = new CancellationTokenSource();
                _listenThread = new Thread(() => ListenToServerLoop(_clientSocket, _cancellationTokenSource.Token));
                _listenThread.IsBackground = true; // Permet à l'application de se fermer même si le thread tourne
                _listenThread.Start();
            }
            else
            {
                StatusTextBlock.Text = "Échec de la connexion.";
                ConnectButton.IsEnabled = true;
            }
        }

        private void ListenToServerLoop(Socket client, CancellationToken token)
        {
            StringBuilder stringBuilder = new StringBuilder(); // Pour assembler les messages fragmentés

            try
            {
                while (client.Connected && !token.IsCancellationRequested)
                {
                    byte[] buffer = new byte[4096]; // Augmenter la taille du buffer si les messages JSON sont longs
                    if (client.Available > 0) // Vérifier s'il y a des données à lire
                    {
                        int bytesReceived = client.Receive(buffer);
                        if (bytesReceived > 0)
                        {
                            string receivedChunk = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                            stringBuilder.Append(receivedChunk);

                            // Traiter les messages JSON complets (séparés par un retour à la ligne)
                            string allData = stringBuilder.ToString();
                            int lastNewline;
                            while ((lastNewline = allData.IndexOf('\n')) >= 0)
                            {
                                string jsonMessage = allData.Substring(0, lastNewline).Trim();
                                allData = allData.Substring(lastNewline + 1);

                                if (!string.IsNullOrWhiteSpace(jsonMessage))
                                {
                                    try
                                    {
                                        // Tenter de désérialiser en une liste de JobProgressInfo
                                        var progressDataList = JsonSerializer.Deserialize<List<JobProgressInfo>>(jsonMessage, _jsonDeserializerOptions);

                                        // Mettre à jour l'UI sur le thread UI
                                        Application.Current.Dispatcher.Invoke(() =>
                                        {
                                            JobProgressList.Clear(); // Vider la liste actuelle
                                            if (progressDataList != null)
                                            {
                                                foreach (var jobInfo in progressDataList)
                                                {
                                                    JobProgressList.Add(jobInfo);
                                                }
                                            }
                                            // Pour l'affichage brut (si vous le gardez)
                                            // ProgressDataRawTextBlock.Text = jsonMessage;
                                        });
                                    }
                                    catch (JsonException jsonEx)
                                    {
                                        Console.WriteLine($"Erreur de désérialisation JSON : {jsonEx.Message} | Données reçues : {jsonMessage}");
                                        // Optionnel: Afficher l'erreur dans l'UI (sur le thread UI)
                                        // Application.Current.Dispatcher.Invoke(() => ProgressDataRawTextBlock.Text = $"Erreur JSON: {jsonEx.Message}\n{jsonMessage}");
                                    }
                                }
                            }
                            stringBuilder.Clear().Append(allData); // Garder le reste pour le prochain chunk
                        }
                        else // 0 byte reçu signifie que le serveur a fermé la connexion (gracieusement)
                        {
                            Console.WriteLine("Serveur a fermé la connexion.");
                            break;
                        }
                    }
                    else
                    {
                        // Petite pause pour ne pas surcharger le CPU si pas de données
                        Thread.Sleep(100); // 100ms
                    }
                }
            }
            catch (SocketException se) when (se.SocketErrorCode == SocketError.ConnectionReset || se.SocketErrorCode == SocketError.TimedOut || se.SocketErrorCode == SocketError.NotConnected)
            {
                Console.WriteLine($"Socket déconnecté : {se.Message}");
            }
            catch (ObjectDisposedException) // Socket a été fermé
            {
                Console.WriteLine("Socket a été fermé (ObjectDisposedException).");
            }
            catch (ThreadAbortException)
            {
                Console.WriteLine("Thread d'écoute interrompu.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans ListenToServerLoop : {ex.Message}");
            }
            finally
            {
                // S'assurer que l'UI est mise à jour pour refléter la déconnexion
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (StatusTextBlock.Text != "Déconnecté") 
                    {
                        StatusTextBlock.Text = "Déconnecté du serveur.";
                    }
                    ConnectButton.IsEnabled = true;
                    DisconnectButton.IsEnabled = false;
                    JobProgressList.Clear(); 
                });
            }
        }


        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            PerformDisconnect();
        }

        private void PerformDisconnect()
        {
            StatusTextBlock.Text = "Déconnexion...";
            _cancellationTokenSource?.Cancel(); // Signaler au thread d'écoute de s'arrêter

            if (_listenThread != null && _listenThread.IsAlive)
            {
                // Attendre un peu que le thread se termine proprement
                //_listenThread.Join(TimeSpan.FromSeconds(1)); // Peut bloquer l'UI
            }

            NetworkClient.Disconnect(_clientSocket);
            _clientSocket = null;

            StatusTextBlock.Text = "Déconnecté";
            ConnectButton.IsEnabled = true;
            DisconnectButton.IsEnabled = false;
            JobProgressList.Clear();
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            PerformDisconnect(); // S'assurer de la déconnexion à la fermeture
        }
    }
}