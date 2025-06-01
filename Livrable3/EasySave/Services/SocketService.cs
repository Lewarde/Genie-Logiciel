using System;
using System.Collections.Generic; // Pour List
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json; // Pour la sérialisation JSON
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent; // Pour ConcurrentBag
using System.Diagnostics;
using System.Linq;

namespace EasySave.Services
{
    public class SocketService
    {
        private TcpListener _listener;
        private readonly int _port;
        private CancellationTokenSource _cts;
        private readonly ConcurrentBag<TcpClient> _clients = new ConcurrentBag<TcpClient>();
        private readonly object _clientsLock = new object(); 

        // Option pour la sérialisation JSON
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = false // Compact JSON for network transfer
        };

        public SocketService(int port)
        {
            _port = port;
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Any, _port);
            try
            {
                _listener.Start();
                Debug.WriteLine($"[SocketService] Server started. Listening on port {_port}...");
                Task.Run(() => AcceptClientsAsync(_cts.Token), _cts.Token);
            }
            catch (SocketException ex)
            {
                Debug.WriteLine($"[SocketService] Error starting listener on port {_port}: {ex.Message}. Port might be in use.");
                // Gérer l'erreur, par exemple, informer l'utilisateur.
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            _listener?.Stop();
            lock (_clientsLock)
            {
                foreach (var client in _clients)
                {
                    try { client.Close(); } catch { /* Ignore */ }
                }
                _clients.Clear(); 
                while (_clients.TryTake(out _)) { }
            }
            Debug.WriteLine("[SocketService] Server stopped.");
        }

        private async Task AcceptClientsAsync(CancellationToken token)
        {
            Debug.WriteLine("[SocketService] Waiting for client connections...");
            while (!token.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();



                    Debug.WriteLine($"[SocketService] Client connected: {client.Client.RemoteEndPoint}");
                    lock (_clientsLock)
                    {
                        _clients.Add(client);
                    }

                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine("[SocketService] Client accept operation cancelled.");
                    break;
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
                {

                    Debug.WriteLine("[SocketService] Listener stopped, client accept interrupted.");
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SocketService] Error accepting client: {ex.Message}");

                    await Task.Delay(1000, token);
                }
            }
            Debug.WriteLine("[SocketService] Client acceptance loop stopped.");
        }

        public async Task SendProgressToClientsAsync(object progressData)
        {
            if (_clients.IsEmpty) return;

            string jsonPayload;
            try
            {
                jsonPayload = JsonSerializer.Serialize(progressData, _jsonSerializerOptions);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SocketService] Error serializing progress data: {ex.Message}");
                return;
            }

            byte[] data = Encoding.UTF8.GetBytes(jsonPayload + Environment.NewLine); 

            List<TcpClient> clientsToRemove = null;

            foreach (var client in _clients)
            {
                if (client.Connected)
                {
                    try
                    {
                        NetworkStream stream = client.GetStream();
                        if (stream.CanWrite)
                        {
                            await stream.WriteAsync(data, 0, data.Length);

                        }
                    }
                    catch (IOException ex) 
                    {
                        Debug.WriteLine($"[SocketService] IOException sending to client {client.Client.RemoteEndPoint}: {ex.Message}. Marking for removal.");
                        if (clientsToRemove == null) clientsToRemove = new List<TcpClient>();
                        clientsToRemove.Add(client);
                    }
                    catch (ObjectDisposedException ex) 
                    {
                        Debug.WriteLine($"[SocketService] ObjectDisposedException sending to client {client.Client.RemoteEndPoint}: {ex.Message}. Marking for removal.");
                        if (clientsToRemove == null) clientsToRemove = new List<TcpClient>();
                        clientsToRemove.Add(client);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[SocketService] Generic error sending to client {client.Client.RemoteEndPoint}: {ex.Message}");
                    }
                }
                else if (client.Client == null)
                {
                    Debug.WriteLine($"[SocketService] Client {client.Client.RemoteEndPoint} found disconnected. Marking for removal.");
                    if (clientsToRemove == null) clientsToRemove = new List<TcpClient>();
                    clientsToRemove.Add(client);
                }
            }

            if (clientsToRemove != null && clientsToRemove.Any())
            {
                lock (_clientsLock)
                {
                    foreach (var clientToRemove in clientsToRemove)
                    {
                        try { clientToRemove.Close(); } catch { /* Ignore */ }

                    }
                    Debug.WriteLine($"[SocketService] Attempted to clean up {clientsToRemove.Count} disconnected clients.");
                }
            }
        }
    }
}