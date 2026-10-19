using System.IO.Ports;
using System.Collections.Generic;
using System.Threading.Tasks;
using DCSRelay.Models;
using DCSRelay.Services;

namespace DCSRelay.Services
{
    /// <summary>
    /// Service responsible for discovering available serial ports on the system.
    /// </summary>
    public class PortDiscoveryService : IPortDiscoveryService
    {
        // Dependencies
        private readonly IHardwarePortLookupService _hardwareLookupService;
        private readonly ISerialPortProvider _portProvider;

        public PortDiscoveryService(IHardwarePortLookupService hardwareLookupService, ISerialPortProvider portProvider)
        {
            _hardwareLookupService = hardwareLookupService;
            _portProvider = portProvider;
        }

        /// <summary>
        /// Asynchronously discovers and returns a list of all available COM port names.
        /// </summary>
        /// <returns>A Task containing a list of port names (e.g., "COM1", "COM3").</returns>
        public async Task<List<HardwareDeviceInfo>> DiscoverPortsAsync()
        {
            try
            {
                // Use the provider to get port names to allow for mocking in unit tests
                var portNames = await Task.Run(() => _portProvider.GetPortNames().Distinct(System.StringComparer.OrdinalIgnoreCase).ToArray());

                List<HardwareDeviceInfo> ports = new List<HardwareDeviceInfo>();

                foreach (var port in portNames)
                {
                    var hwinfo = await this._hardwareLookupService.GetHardwareDetailsAsync(port);
                    if (hwinfo == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ignoring {port}: no currently connected device was found.");
                        continue;
                    }

                    System.Diagnostics.Debug.WriteLine($"Hardware Info for {port}: {hwinfo.VendorID} {hwinfo.ProductID} {hwinfo.Description} {hwinfo.PnpDeviceID}");
                    ports.Add(hwinfo);
                }

                ports.Sort((left, right) => PortNameComparer.Instance.Compare(left.PortName, right.PortName));
                return ports;
            }
            catch (System.Exception ex)
            {
                // Log or handle the exception appropriately for production code
                System.Diagnostics.Debug.WriteLine($"Error discovering serial ports: {ex.Message}");
                return new List<HardwareDeviceInfo>(); // Return empty list on failure
            }
        }

        /// <summary>
        /// Retrieves detailed hardware and driver information for a specific serial port.
        /// </summary>
        /// <param name="portName">The name of the COM port (e.g., "COM3").</param>
        /// <returns>A Task resolving to the HardwareDeviceInfo if found, otherwise null.</returns>
        public async Task<HardwareDeviceInfo> GetPortHardwareDetailsAsync(string portName)
        {
            return await _hardwareLookupService.GetHardwareDetailsAsync(portName);
        }
    }
}