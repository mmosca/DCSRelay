using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DCSRelay.Models;

namespace DCSRelay.Services
{
    /// <summary>
    /// Interface for multicast UDP relay service.
    /// </summary>
    public interface IMulticastUdpRelayService : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the relay service is actively listening.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Event fired when a UDP packet is received and relayed to serial ports.
        /// </summary>
        event EventHandler<RelayEventArgs>? DataRelayed;

        /// <summary>
        /// Event fired when an error occurs during relay operations.
        /// </summary>
        event EventHandler<System.IO.ErrorEventArgs>? ErrorOccurred;

        /// <summary>
        /// Starts listening for multicast UDP traffic and relaying to serial ports.
        /// </summary>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops listening for multicast UDP traffic.
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Updates the list of active serial ports that will receive relayed data.
        /// </summary>
        void UpdateActivePorts(List<HardwareDeviceInfo> ports);

        /// <summary>
        /// Adds a single port to the relay target list.
        /// </summary>
        void AddPort(HardwareDeviceInfo port);

        /// <summary>
        /// Removes a single port from the relay target list.
        /// </summary>
        void RemovePort(HardwareDeviceInfo port);

        /// <summary>
        /// Gets the current count of active relay ports.
        /// </summary>
        int GetActivePortCount();
    }
}