using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DCSRelay.Models;
using DCSRelay.Services;
using Xunit;

namespace DCSRelay.Tests.Services
{
    public class MulticastUdpRelayServiceTests
    {
        private class FakeUdpClientWrapper : IUdpClientWrapper
        {
            public bool JoinMulticastCalled { get; private set; }
            public bool BindCalled { get; private set; }
            public bool ReuseAddressSet { get; private set; }
            public int SendCount { get; private set; }
            public List<(byte[] Data, IPEndPoint Endpoint)> SentPackets { get; } = new();

            private TaskCompletionSource<UdpReceiveResult> _receiveTcs = new();

            public void JoinMulticastGroup(IPAddress multicastAddress, IPAddress interfaceAddress) => JoinMulticastCalled = true;
            public void SetReuseAddress(bool reuse) => ReuseAddressSet = reuse;
            public void Bind(IPEndPoint localEP) => BindCalled = true;
            public void Close() { }
            public void Dispose() { }

            public async Task<UdpReceiveResult> ReceiveAsync(CancellationToken cancellationToken)
            {
                return await _receiveTcs.Task;
            }

            public void SimulateReceive(byte[] data)
            {
                _receiveTcs.TrySetResult(new UdpReceiveResult(data, new IPEndPoint(IPAddress.Any, 0)));
                _receiveTcs = new TaskCompletionSource<UdpReceiveResult>();
            }

            public Task SendAsync(byte[] bytes, int bytesPerPacket, IPEndPoint endPoint)
            {
                SendCount++;
                SentPackets.Add((bytes, endPoint));
                return Task.CompletedTask;
            }
        }

        private class FakeSerialPortWrapper : ISerialPortWrapper
        {
            public string PortName { get; set; }
            public bool IsOpen { get; set; } = true;
            public int BaudRate { get; set; }
            public event DCSRelay.Services.SerialDataReceivedEventHandler DataReceived;
            public List<byte[]> WrittenData { get; } = new();
            public string ReadExistingValue { get; set; } = "";

            public void Open() => IsOpen = true;
            public void Close() => IsOpen = false;
            public void Write(byte[] buffer, int offset, int count)
            {
                var data = new byte[count];
                Array.Copy(buffer, offset, data, 0, count);
                WrittenData.Add(data);
            }
            public string ReadExisting() => ReadExistingValue;
            public void Dispose() => IsOpen = false;

            public void SimulateDataReceived()
            {
                DataReceived?.Invoke(this, EventArgs.Empty);
            }
        }

        private class FakeCommunicationFactory : ICommunicationFactory
        {
            public FakeUdpClientWrapper UdpClient { get; } = new();
            public Dictionary<string, FakeSerialPortWrapper> SerialPorts { get; } = new();

            public IUdpClientWrapper CreateUdpClient() => UdpClient;
            public ISerialPortWrapper CreateSerialPort(string portName, int baudRate)
            {
                var port = new FakeSerialPortWrapper { PortName = portName };
                SerialPorts[portName] = port;
                return port;
            }
        }

        private class FakeAppConfigService : IAppConfigurationService
        {
            public bool StartMinimized { get; set; }
            public bool StartWithWindows { get; set; }
            public string DcsBiosCommandAddress { get; set; } = "127.0.0.1:7778";
            public Task LoadAsync() => Task.CompletedTask;
            public Task SaveAsync() => Task.CompletedTask;
        }

        [Fact]
        public async Task StartAsync_CorrectlyConfiguresNetwork()
        {
            var factory = new FakeCommunicationFactory();
            var config = new FakeAppConfigService();
            var service = new MulticastUdpRelayService(config, factory);

            await service.StartAsync();

            Assert.True(service.IsRunning);
            Assert.True(factory.UdpClient.BindCalled);
            Assert.True(factory.UdpClient.JoinMulticastCalled);
            Assert.True(factory.UdpClient.ReuseAddressSet);
        }

        [Fact]
        public async Task DataReceived_RelaysToActivePorts()
        {
            var factory = new FakeCommunicationFactory();
            var config = new FakeAppConfigService();
            var service = new MulticastUdpRelayService(config, factory);

            await service.StartAsync();

            var port = new HardwareDeviceInfo { PortName = "COM1" };
            service.UpdateActivePorts(new List<HardwareDeviceInfo> { port });

            var data = new byte[] { 0x01, 0x02, 0x03 };
            factory.UdpClient.SimulateReceive(data);

            // Wait for relay
            await Task.Delay(100);

            var serialPort = factory.SerialPorts["COM1"];
            Assert.Single(serialPort.WrittenData);
            Assert.Equal(data, serialPort.WrittenData[0]);
        }

        [Fact]
        public async Task SerialData_SendsToDcsBios()
        {
            var factory = new FakeCommunicationFactory();
            var config = new FakeAppConfigService { DcsBiosCommandAddress = "1.2.3.4:1234" };
            var service = new MulticastUdpRelayService(config, factory);

            await service.StartAsync();

            var port = new HardwareDeviceInfo { PortName = "COM1" };
            service.UpdateActivePorts(new List<HardwareDeviceInfo> { port });

            var serialPort = factory.SerialPorts["COM1"];
            serialPort.ReadExistingValue = "MASTER_ARM 1\n";
            serialPort.SimulateDataReceived();

            // Wait for UDP send
            await Task.Delay(100);

            Assert.Single(factory.UdpClient.SentPackets);
            Assert.Equal("MASTER_ARM 1\n", System.Text.Encoding.ASCII.GetString(factory.UdpClient.SentPackets[0].Data));
            Assert.Equal(new IPEndPoint(IPAddress.Parse("1.2.3.4"), 1234), factory.UdpClient.SentPackets[0].Endpoint);
        }

        [Fact]
        public async Task UpdateActivePorts_UpdatesSessions()
        {
            var factory = new FakeCommunicationFactory();
            var config = new FakeAppConfigService();
            var service = new MulticastUdpRelayService(config, factory);

            await service.StartAsync();

            service.UpdateActivePorts(new List<HardwareDeviceInfo> { new HardwareDeviceInfo { PortName = "COM1" } });
            Assert.True(factory.SerialPorts.ContainsKey("COM1"));

            service.UpdateActivePorts(new List<HardwareDeviceInfo>());
            // Session should be closed (disposing the fake port)
            Assert.False(factory.SerialPorts["COM1"].IsOpen);
        }

        [Fact]
        public async Task StopAsync_CleansUpResources()
        {
            var factory = new FakeCommunicationFactory();
            var config = new FakeAppConfigService();
            var service = new MulticastUdpRelayService(config, factory);

            await service.StartAsync();
            Assert.True(service.IsRunning);

            await service.StopAsync();
            Assert.False(service.IsRunning);
        }
    }
}
