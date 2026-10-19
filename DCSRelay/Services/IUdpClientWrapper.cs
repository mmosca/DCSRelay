using System.Net;
using System.Net.Sockets;

namespace DCSRelay.Services
{
    /// <summary>
    /// Abstract wrapper for UdpClient to enable unit testing of network logic.
    /// </summary>
    public interface IUdpClientWrapper : IDisposable
    {
        void JoinMulticastGroup(IPAddress multicastAddress, IPAddress interfaceAddress);
        Task<UdpReceiveResult> ReceiveAsync(CancellationToken cancellationToken);
        Task SendAsync(byte[] bytes, int bytesPerPacket, IPEndPoint endPoint);
        void SetReuseAddress(bool reuse);
        void Bind(IPEndPoint localEP);
        void Close();
    }
}
