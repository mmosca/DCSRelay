using System;
using System.Management;
using System.Threading.Tasks;
using DCSRelay.Models;

namespace DCSRelay.Services
{
    /// <summary>
    /// Service responsible for querying Windows Management Instrumentation (WMI)
    /// to retrieve deep hardware details for a given COM port.
    /// </summary>
    public class HardwarePortLookupService : IHardwarePortLookupService
    {
        /// <summary>
        /// Finds and returns the hardware details for a specified COM port.
        /// </summary>
        /// <param name="portName">The name of the COM port (e.g., "COM3").</param>
        /// <returns>A HardwareDeviceInfo object containing details, or null if not found or an error occurs.</returns>
        public async Task<HardwareDeviceInfo> GetHardwareDetailsAsync(string portName)
        {
            if (string.IsNullOrWhiteSpace(portName))
            {
                throw new ArgumentException("Port name cannot be null or empty.", nameof(portName));
            }

            // WMI queries are traditionally synchronous, but we wrap them in Task.Run
            // to prevent blocking the calling thread (important for WPF responsiveness).
            var deviceInfo = await Task.Run(() =>
            {
                try
                {
                    // Use a filter based on the port name or a related PnP class.
                    // Win32_PnPEntity is a good candidate for general device info.
                    // Specific COM port mapping requires knowing the exact device instance ID format,
                    // which might vary. Only match devices Windows currently reports as present.
                    string query = $"SELECT * FROM Win32_PnPEntity WHERE ClassGuid = '{{4d36e978-e325-11ce-bfc1-08002be10318}}' AND Caption LIKE '%({portName})%' AND Present = TRUE";

                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
                    {
                        ManagementObjectCollection collection = searcher.Get();

                        foreach (ManagementObject obj in collection)
                        {
                            ManagementBaseObject device = (ManagementBaseObject)obj;

                            // Attempt to extract details.
                            // Note: VID/PID are often found in a separate hardware ID property,
                            // which is more complex to parse from all instances. We prioritize known properties.

                            foreach(var prop in device.Properties)
                            {
                                System.Diagnostics.Debug.WriteLine($"{prop.Name}: {prop.Value}");
                            }
                            Console.Out.Flush();

                            var name = device["Name"]?.ToString() ?? "Unknown";
                            var deviceName = device["Caption"]?.ToString() ?? "Unknown Device";
                            var driverName = "N/A";//device.GetPropertyValue("DriverName")?.ToString() ?? "N/A";
                            var manufacturer = device["Manufacturer"]?.ToString() ?? "N/A";
                            var pnpId = device["PNPDeviceID"]?.ToString() ?? portName;
                            var deviceId = device["DeviceID"]?.ToString() ?? portName;

                            // Simple heuristic to extract VID/PID from PNPDeviceID string (common format)
                            string vid = "N/A";
                            string pid = "N/A";
                            if (pnpId.Contains("VID_"))
                            {
                                // This is a simplistic extraction; robust parsing is needed in production
                                var parts = pnpId.Split('_');
                                foreach (var part in parts)
                                {
                                    if (part.StartsWith("VID_")) vid = part.Substring(4);
                                    if (part.StartsWith("PID_")) pid = part.Substring(4);
                                }
                            }

                            // Return the first match found
                            return new HardwareDeviceInfo
                            {
                                Name = name,
                                PortName = portName,
                                Description = deviceName,
                                VendorID = vid,
                                ProductID = pid,
                                PnpDeviceID = pnpId,
                                DeviceID = deviceId
                            };
                        }
                    }
                    // If the loop finishes without returning, the port was not found via WMI match
                    return null;
                }
                catch (Exception ex)
                {
                    // Log the exception details here (e.g., to a logging service)
                    // For now, return null on any WMI failure.
                    Console.WriteLine($"Error during WMI lookup for {portName}: {ex.Message}");
                    return null;
                }
            });

            return deviceInfo;
        }
    }
}