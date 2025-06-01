using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;

namespace EasySave.Services
{
    // Manages TCP socket communication for sending progress updates to connected clients.
    public class SocketService
    {
        private TcpListener _listener; // Listener for incoming client connections.
        private readonly int _port; // Port number to listen on.
        private CancellationTokenSource _cts; // Cancellation token source for managing asynchronous operations.
        private readonly ConcurrentBag<TcpClient> _clients = new ConcurrentBag<TcpClient>(); // Thread-safe collection of connected clients.
        private readonly object _clientsLock = new object(); // Lock object for thread-safe operations on the clients collection.

        // JSON serialization options for compact network transfer.
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = false
        };

        // Constructor initializes the port.
        public SocketService(int port)
        {
            _port = port;
        }

        // Starts the TCP listener and begins accepting clients.
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
            }
        }

        // Stops the TCP listener and disconnects all clients.
        public void Stop()
        {
            _cts?.Cancel();
            _listener?.Stop();
            lock (_clientsLock)
            {
                foreach (var client in _clients)
                {
                    try { client.Close(); } catch { /* Ignore errors */ }
                }
                _clients.Clear();
                while (_clients.TryTake(out _)) { }
            }
            Debug.WriteLine("[SocketService] Server stopped.");
        }

        // Asynchronously accepts incoming client connections.
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

        // Sends progress data to all connected clients.
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
                        clientsToRemove ??= new List<TcpClient>();
                        clientsToRemove.Add(client);
                    }
                    catch (ObjectDisposedException ex)
                    {
                        Debug.WriteLine($"[SocketService] ObjectDisposedException sending to client {client.Client.RemoteEndPoint}: {ex.Message}. Marking for removal.");
                        clientsToRemove ??= new List<TcpClient>();
                        clientsToRemove.Add(client);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[SocketService] Generic error sending to client {client.Client.RemoteEndPoint}: {ex.Message}");
                    }
                }
                else
                {
                    Debug.WriteLine($"[SocketService] Client {client.Client.RemoteEndPoint} found disconnected. Marking for removal.");
                    clientsToRemove ??= new List<TcpClient>();
                    clientsToRemove.Add(client);
                }
            }

            if (clientsToRemove != null && clientsToRemove.Any())
            {
                lock (_clientsLock)
                {
                    foreach (var clientToRemove in clientsToRemove)
                    {
                        try { clientToRemove.Close(); } catch { /* Ignore errors */ }
                    }
                    Debug.WriteLine($"[SocketService] Attempted to clean up {clientsToRemove.Count} disconnected clients.");
                }
            }
        }
    }
}
