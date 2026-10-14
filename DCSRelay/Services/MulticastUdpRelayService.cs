using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DCSRelay.Models;

namespace DCSRelay.Services
{
    /// <summary>
    /// Service that listens for multicast UDP traffic on 239.255.50.10:5010
    /// and redistributes it to active serial ports.
    /// </summary>
    public class MulticastUdpRelayService : IMulticastUdpRelayService
    {
        private const string MulticastAddress = "239.255.50.10";
        private const int MulticastPort = 5010;
        private const int BufferSize = 4096;

        private readonly List<HardwareDeviceInfo> _activePorts;
        private readonly object _portsLock = new object();

        private UdpClient? _udpClient;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _listenerTask;
        private bool _isRunning;

        /// <summary>
        /// Gets a value indicating whether the relay service is actively listening.
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// Event fired when a UDP packet is received and relayed to serial ports.
        /// </summary>
        public event EventHandler<RelayEventArgs>? DataRelayed;

        /// <summary>
        /// Event fired when an error occurs during relay operations.
        /// </summary>
        public event EventHandler<System.IO.ErrorEventArgs>? ErrorOccurred;

        public MulticastUdpRelayService()
        {
            _activePorts = new List<HardwareDeviceInfo>();
            _isRunning = false;
        }

        /// <summary>
        /// Starts listening for multicast UDP traffic and relaying to serial ports.
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_isRunning)
            {
                throw new InvalidOperationException("Relay service is already running.");
            }

            try
            {
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _udpClient = new UdpClient();
                _udpClient.ExclusiveAddressUse = false;

                // Bind to the multicast port on any interface
                var localEndpoint = new IPEndPoint(IPAddress.Any, MulticastPort);
                _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _udpClient.Client.Bind(localEndpoint);

                // Join the multicast group
                var multicastAddress = IPAddress.Parse(MulticastAddress);
                _udpClient.JoinMulticastGroup(multicastAddress, IPAddress.Any);

                _isRunning = true;
                _listenerTask = ListenForMulticastAsync(_cancellationTokenSource.Token);

                System.Diagnostics.Debug.WriteLine($"Multicast UDP relay started on {MulticastAddress}:{MulticastPort}");
            }
            catch (Exception ex)
            {
                _isRunning = false;
                ErrorOccurred?.Invoke(this, new ErrorEventArgs(ex));
                throw;
            }
        }

        /// <summary>
        /// Stops listening for multicast UDP traffic.
        /// </summary>
        public async Task StopAsync()
        {
            if (!_isRunning)
            {
                return;
            }

            _isRunning = false;
            _cancellationTokenSource?.Cancel();

            // Closing the socket guarantees the pending ReceiveAsync is aborted
            //_udpClient?.Dispose();

            if (_listenerTask != null)
            {
                try
                {
                    await _listenerTask.WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException)
                {
                    // Expected when cancellation is requested (or listener is slow to finish)
                }
            }

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _udpClient = null;
            _listenerTask = null;

            System.Diagnostics.Debug.WriteLine("Multicast UDP relay stopped.");
        }

        /// <summary>
        /// Updates the list of active serial ports that will receive relayed data.
        /// </summary>
        public void UpdateActivePorts(List<HardwareDeviceInfo> ports)
        {
            lock (_portsLock)
            {
                _activePorts.Clear();
                _activePorts.AddRange(ports ?? new List<HardwareDeviceInfo>());
            }

            System.Diagnostics.Debug.WriteLine($"Relay service now targeting {_activePorts.Count} ports.");
        }

        /// <summary>
        /// Adds a single port to the relay target list.
        /// </summary>
        public void AddPort(HardwareDeviceInfo port)
        {
            lock (_portsLock)
            {
                if (!_activePorts.Contains(port))
                {
                    _activePorts.Add(port);
                }
            }
        }

        /// <summary>
        /// Removes a single port from the relay target list.
        /// </summary>
        public void RemovePort(HardwareDeviceInfo port)
        {
            lock (_portsLock)
            {
                _activePorts.Remove(port);
            }
        }

        /// <summary>
        /// Gets the current count of active relay ports.
        /// </summary>
        public int GetActivePortCount()
        {
            lock (_portsLock)
            {
                return _activePorts.Count;
            }
        }

        private async Task ListenForMulticastAsync(CancellationToken cancellationToken)
        {
            try
            {
                var client = _udpClient;

                while (!cancellationToken.IsCancellationRequested && _udpClient != null)
                {
                    try
                    {
                        List<Socket> clients = new List<Socket> { client.Client };
                        // Receive UDP packet asynchronously
                        Socket.Select(clients, null, clients, 1000000); // 1 second timeout
                        if(clients.Count > 0)
                        {
                            if (client.Client.Available > 0)
                            {
                                var result = await client.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                                var data = result.Buffer.AsSpan(0, result.Buffer.Length).ToArray();

                                // Relay to all active serial ports
                                await RelayToSerialPortsAsync(data);

                                // Fire event for logging/monitoring
                                DataRelayed?.Invoke(this, new RelayEventArgs(result.RemoteEndPoint, data.Length));
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected during shutdown
                        break;
                    }
                }
            }
            catch (Exception ex) when (cancellationToken.IsCancellationRequested && (ex is ObjectDisposedException || ex is SocketException))
            {
                // Socket was closed during shutdown
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, new ErrorEventArgs(ex));
                System.Diagnostics.Debug.WriteLine($"Error in multicast listener: {ex.Message}");
            }
        }

        private async Task RelayToSerialPortsAsync(byte[] data)
        {
            List<HardwareDeviceInfo> portsCopy;

            lock (_portsLock)
            {
                portsCopy = new List<HardwareDeviceInfo>(_activePorts);
            }

            var tasks = new List<Task>();

            foreach (var portInfo in portsCopy)
            {
                tasks.Add(Task.Run(() => RelayToSerialPort(portInfo, data)));
            }

            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error relaying to serial ports: {ex.Message}");
            }
        }

        private void RelayToSerialPort(HardwareDeviceInfo portInfo, byte[] data)
        {
            SerialPort? port = null;

            try
            {
                port = new SerialPort(portInfo.PortName)
                {
                    BaudRate = 115200,
                    Parity = Parity.None,
                    DataBits = 8,
                    StopBits = StopBits.One,
                    Handshake = Handshake.None,
                    ReadTimeout = 1000,
                    WriteTimeout = 1000
                };

                if (!port.IsOpen)
                {
                    port.Open();
                }

                port.Write(data, 0, data.Length);

                System.Diagnostics.Debug.WriteLine($"Relayed {data.Length} bytes to {portInfo.PortName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error writing to {portInfo.PortName}: {ex.Message}");
            }
            finally
            {
                port?.Dispose();
            }
        }

        public void Dispose()
        {
            // Run on the thread pool so a UI synchronization context cannot deadlock the wait
            Task.Run(StopAsync).Wait(TimeSpan.FromSeconds(5));
        }
    }

    /// <summary>
    /// Event arguments for relay events.
    /// </summary>
    public class RelayEventArgs : EventArgs
    {
        public IPEndPoint RemoteEndPoint { get; }
        public int BytesRelayed { get; }

        public RelayEventArgs(IPEndPoint remoteEndPoint, int bytesRelayed)
        {
            RemoteEndPoint = remoteEndPoint;
            BytesRelayed = bytesRelayed;
        }
    }
}