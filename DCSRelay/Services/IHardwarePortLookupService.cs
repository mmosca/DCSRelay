namespace DCSRelay.Services
{
    using System.Threading.Tasks;
    using DCSRelay.Models;

    /// <summary>
    /// Defines the contract for querying detailed hardware information from a COM port.
    /// This interface allows for mocking in unit tests and dependency injection.
    /// </summary>
    public interface IHardwarePortLookupService
    {
        /// <summary>
        /// Finds and returns the hardware details for a specified COM port.
        /// </summary>
        /// <param name="portName">The name of the COM port (e.g., "COM3").</param>
        /// <returns>A Task resolving to the HardwareDeviceInfo if found, otherwise null.</returns>
        Task<HardwareDeviceInfo> GetHardwareDetailsAsync(string portName);
    }
}