using DCSRelay.Models;

namespace DCSRelay.Services
{
    public interface IPortDiscoveryService
    {
        Task<List<HardwareDeviceInfo>> DiscoverPortsAsync();
    }
}