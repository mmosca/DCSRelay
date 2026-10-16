using System.Collections.Generic;
using System.Threading.Tasks;
using DCSRelay.Models;

namespace DCSRelay.Services
{
    /// <summary>
    /// Interface for port configuration persistence service.
    /// </summary>
    public interface IPortConfigurationService
    {
        /// <summary>
        /// Loads the previously saved active ports from the configuration file.
        /// </summary>
        /// <returns>A list of previously active device identifiers, or an empty list if no configuration exists.</returns>
        Task<List<string>> LoadActivePortsAsync();

        /// <summary>
        /// Saves the current list of active ports to the configuration file.
        /// </summary>
        Task SaveActivePortsAsync(List<HardwareDeviceInfo> activePorts);
    }
}
