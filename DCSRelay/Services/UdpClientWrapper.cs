using System.Net;
using System.Net.Sockets;

namespace DCSRelay.Services
{
    public class UdpClientWrapper : IUdpClientWrapper
    {
        private readonly UdpClient _client;

        public UdpClientWrapper()
        {
            _client = new UdpClient();
        }

        public UdpClientWrapper(UdpClient client)
        {
            _client = client;
        }

        public void JoinMulticastGroup(IPAddress multicastAddress, IPAddress interfaceAddress) 
            => _client.JoinMulticastGroup(multicastAddress, interfaceAddress);

        public async Task<UdpReceiveResult> ReceiveAsync(CancellationToken cancellationToken)
        {
            // UdpClient.ReceiveAsync doesn't take a CancellationToken in older .NET, 
            // but in .NET 10 we can use the Task.WaitAsync or the overload if available.
            // For compatibility with the existing logic in MulticastUdpRelayService, 
            // we can wrap the call.
            return await _client.ReceiveAsync(cancellationToken);
        }

        public async Task SendAsync(byte[] bytes, int bytesPerPacket, IPEndPoint endPoint)
            => await _client.SendAsync(bytes, bytesPerPacket, endPoint);

        public void SetReuseAddress(bool reuse)
        {
            _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, reuse);
        }

        public void Bind(IPEndPoint localEP)
        {
            _client.Client.Bind(localEP);
        }

        public void Close() => _client.Close();
        public void Dispose() => _client.Dispose();
    }
}
