// Client/Program.cs
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows; // Pour MessageBox, bien que ce soit mieux de le gérer dans la partie UI

namespace Client
{

    public static class NetworkClient
    {
        public static Socket ConnectToServer(string ipAddressStr, int port)
        {
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            if (!IPAddress.TryParse(ipAddressStr, out IPAddress ipAddress))
            {
                MessageBox.Show($"Adresse IP invalide : {ipAddressStr}");
                return null;
            }

            try
            {
                clientSocket.Connect(new IPEndPoint(ipAddress, port));
                return clientSocket;
            }
            catch (SocketException se)
            {
                // Il est préférable de ne pas afficher de MessageBox ici,
                // mais de laisser la méthode appelante (MainWindow) gérer l'affichage de l'erreur.
                // On peut logger l'erreur ou la remonter.
                Console.WriteLine($"Erreur de connexion : {se.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur : {ex.Message}");
                return null;
            }
        }



        public static void Disconnect(Socket socket)
        {
            if (socket != null && socket.Connected)
            {
                try
                {
                    socket.Shutdown(SocketShutdown.Both);
                }
                catch (SocketException) { /* Ignorer si déjà fermé */ }
                catch (ObjectDisposedException) { /* Ignorer si déjà disposé */ }
                finally
                {
                    socket.Close();
                }
            }
        }
    }
}