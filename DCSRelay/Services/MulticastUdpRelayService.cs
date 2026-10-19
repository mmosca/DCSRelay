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

        private const int DcsBiosCommandPort = 7778;
        private const int MaxPendingLineLength = 1024;

        private readonly Dictionary<string, SerialSession> _sessions = new Dictionary<string, SerialSession>(StringComparer.OrdinalIgnoreCase);
        private IUdpClientWrapper? _commandClient;
        private readonly Services.IAppConfigurationService _appConfigService;
        private readonly ICommunicationFactory _commFactory;

        private readonly List<HardwareDeviceInfo> _activePorts;
        private readonly object _portsLock = new object();

        private IUdpClientWrapper? _udpClient;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _listenerTask;
        private bool _isRunning;

        public bool IsRunning => _isRunning;

        public event EventHandler<RelayEventArgs>? DataRelayed;
        public event EventHandler<System.IO.ErrorEventArgs>? ErrorOccurred;

        public MulticastUdpRelayService(Services.IAppConfigurationService appConfigService, ICommunicationFactory commFactory)
        {
            _activePorts = new List<HardwareDeviceInfo>();
            _isRunning = false;
            _appConfigService = appConfigService ?? throw new ArgumentNullException(nameof(appConfigService));
            _commFactory = commFactory ?? throw new ArgumentNullException(nameof(commFactory));
        }

        private static IPEndPoint ParseCommandEndpoint(string? address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return new IPEndPoint(IPAddress.Parse("127.0.0.1"), DcsBiosCommandPort);
            }

            // Support formats like "127.0.0.1:7778" or just "127.0.0.1"
            var parts = address.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[1], out var port))
            {
                if (IPAddress.TryParse(parts[0], out var ip))
                {
                    return new IPEndPoint(ip, port);
                }
            }

            // Fallback to localhost with default port
            return new IPEndPoint(IPAddress.Parse("127.0.0.1"), DcsBiosCommandPort);
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
                _udpClient = _commFactory.CreateUdpClient();
                _udpClient.SetReuseAddress(true);

                var localEndpoint = new IPEndPoint(IPAddress.Any, MulticastPort);
                _udpClient.Bind(localEndpoint);

                var multicastAddress = IPAddress.Parse(MulticastAddress);
                _udpClient.JoinMulticastGroup(multicastAddress, IPAddress.Any);

                _commandClient = _commFactory.CreateUdpClient();

                _isRunning = true;
                lock (_portsLock)
                {
                    SyncSessions();
                }
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

            if (_listenerTask != null)
            {
                try
                {
                    await _listenerTask.WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException)
                {
                }
            }

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _udpClient?.Dispose();
            _udpClient = null;
            _listenerTask = null;

            lock (_portsLock)
            {
                SyncSessions();
            }
            _commandClient?.Dispose();
            _commandClient = null;

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
                SyncSessions();
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
                    SyncSessions();
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
                SyncSessions();
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
            byte[] buffer = new byte[BufferSize];
            if (_udpClient == null)
            {
                return;
            }

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = await _udpClient.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                        if (result.Buffer.Length > 0)
                        {
                            var data = result.Buffer;

                            await RelayToSerialPortsAsync(data).ConfigureAwait(false);

                            DataRelayed?.Invoke(this, new RelayEventArgs(data.Length));
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex) when (cancellationToken.IsCancellationRequested && (ex is ObjectDisposedException || ex is SocketException))
            {
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, new ErrorEventArgs(ex));
                System.Diagnostics.Debug.WriteLine($"Error in multicast listener: {ex.Message}");
            }
        }

        private async Task RelayToSerialPortsAsync(byte[] data)
        {
            List<SerialSession> sessionsCopy;

            lock (_portsLock)
            {
                sessionsCopy = new List<SerialSession>(_sessions.Values);
            }

            var tasks = new List<Task>();

            foreach (var session in sessionsCopy)
            {
                tasks.Add(Task.Run(() => WriteToSession(session, data)));
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

        private static void WriteToSession(SerialSession session, byte[] data)
        {
            try
            {
                lock (session.WriteLock)
                {
                    if (!session.Port.IsOpen)
                    {
                        if (!session.TryReconnect())
                        {
                            System.Diagnostics.Debug.WriteLine($"Skipping write to {session.Port.PortName}: port is closed and reconnect failed.");
                            return;
                        }
                    }

                    session.Port.Write(data, 0, data.Length);
                }

                System.Diagnostics.Debug.WriteLine($"Relayed {data.Length} bytes to {session.Port.PortName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error writing to {session.Port.PortName}: {ex.Message}");

                // Best-effort recovery for transient disconnects.
                session.TryReconnect(force: true);
            }
        }

        // Must be called with _portsLock held. Keeps open serial ports in line with the active list.
        private void SyncSessions()
        {
            var wanted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (_isRunning)
            {
                foreach (var info in _activePorts)
                {
                    wanted.Add(info.PortName);
                }
            }

            foreach (var name in new List<string>(_sessions.Keys))
            {
                if (!wanted.Contains(name))
                {
                    var session = _sessions[name];
                    _sessions.Remove(name);
                    session.Close();
                }
            }

            foreach (var name in wanted)
            {
                if (_sessions.ContainsKey(name))
                {
                    continue;
                }

                try
                {
                    _sessions[name] = new SerialSession(name, _commFactory, OnSerialDataReceived);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error opening {name}: {ex.Message}");
                    ErrorOccurred?.Invoke(this, new ErrorEventArgs(ex));
                }
            }
        }

        private void OnSerialDataReceived(SerialSession session)
        {
            List<string> commands;
            try
            {
                commands = session.ReadCommands(MaxPendingLineLength);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading from {session.Port.PortName}: {ex.Message}");
                return;
            }

            foreach (var command in commands)
            {
                _ = SendToDcsBiosAsync(command);
            }
        }

        private async Task SendToDcsBiosAsync(string command)
        {
            var client = _commandClient;
            if (client == null)
            {
                return;
            }

            try
            {
                var bytes = System.Text.Encoding.ASCII.GetBytes(command);
                var endpoint = ParseCommandEndpoint(_appConfigService.DcsBiosCommandAddress);
                await client.SendAsync(bytes, bytes.Length, endpoint).ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine($"Sent to DCS-BIOS: {command.TrimEnd()} -> {endpoint}");
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, new ErrorEventArgs(ex));
            }
        }

        private sealed class SerialSession
        {
            private readonly System.Text.StringBuilder _pending = new System.Text.StringBuilder();
            private readonly object _readLock = new object();
            private readonly object _reconnectLock = new object();
            private readonly string _portName;
            private readonly Action<SerialSession> _onData;
            private readonly ICommunicationFactory _commFactory;

            public ISerialPortWrapper Port { get; private set; }
            public object WriteLock { get; } = new object();

            public SerialSession(string portName, ICommunicationFactory commFactory, Action<SerialSession> onData)
            {
                _portName = portName;
                _commFactory = commFactory;
                _onData = onData;
                Port = OpenWithFallback(_portName, _onData);
            }

            public bool TryReconnect(bool force = false)
            {
                lock (_reconnectLock)
                {
                    try
                    {
                        if (!force && Port.IsOpen)
                        {
                            return true;
                        }

                        try
                        {
                            Port.Dispose();
                        }
                        catch
                        {
                        }

                        Port = OpenWithFallback(_portName, _onData);
                        System.Diagnostics.Debug.WriteLine($"Reconnected {_portName}.");
                        return Port.IsOpen;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Reconnect failed for {_portName}: {ex.Message}");
                        return false;
                    }
                }
            }

            private ISerialPortWrapper OpenWithFallback(string portName, Action<SerialSession> onData)
            {
                ISerialPortWrapper port = _commFactory.CreateSerialPort(portName, 250000);
                port.DataReceived += (_, _) => onData(this);

                try
                {
                    port.Open();
                    return port;
                }
                catch (Exception firstOpenException)
                {
                    System.Diagnostics.Debug.WriteLine($"Opening {portName} at 250000 failed ({firstOpenException.Message}), retrying at 115200.");

                    try
                    {
                        port.Dispose();
                    }
                    catch
                    {
                    }
                }

                port = _commFactory.CreateSerialPort(portName, 115200);
                port.DataReceived += (_, _) => onData(this);
                port.Open();
                return port;
            }

            public List<string> ReadCommands(int maxPending)
            {
                var result = new List<string>();
                lock (_readLock)
                {
                    if (!Port.IsOpen)
                    {
                        return result;
                    }

                    _pending.Append(Port.ReadExisting());

                    int newline;
                    while ((newline = IndexOfNewline()) >= 0)
                    {
                        var line = _pending.ToString(0, newline).Trim();
                        _pending.Remove(0, newline + 1);
                        if (line.Length > 0)
                        {
                            result.Add(line + "\n");
                        }
                    }

                    if (_pending.Length > maxPending)
                    {
                        _pending.Clear();
                    }
                }
                return result;
            }

            private int IndexOfNewline()
            {
                for (int i = 0; i < _pending.Length; i++)
                {
                    if (_pending[i] == '\n')
                    {
                        return i;
                    }
                }
                return -1;
            }

            public void Close()
            {
                try
                {
                    Port.Dispose();
                }
                catch (Exception)
                {
                }
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
        //public IPEndPoint RemoteEndPoint { get; }
        public int BytesRelayed { get; }

        public RelayEventArgs(int bytesRelayed)
        {
            //RemoteEndPoint = remoteEndPoint;
            BytesRelayed = bytesRelayed;
        }
    }
}