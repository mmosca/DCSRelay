using System;
using DCSRelay.Services;
using Xunit;

namespace DCSRelay.Tests.Services
{
    public class CommunicationFactoryTests
    {
        [Fact]
        public void CreateUdpClient_ReturnsUdpClientWrapper()
        {
            // Arrange
            var factory = new CommunicationFactory();

            // Act
            var client = factory.CreateUdpClient();

            // Assert
            Assert.NotNull(client);
            Assert.IsType<UdpClientWrapper>(client);
        }

        [Fact]
        public void CreateSerialPort_ReturnsSerialPortWrapper()
        {
            // Arrange
            var factory = new CommunicationFactory();
            string portName = "COM1";
            int baudRate = 115200;

            // Act
            var port = factory.CreateSerialPort(portName, baudRate);

            // Assert
            Assert.NotNull(port);
            Assert.IsType<SerialPortWrapper>(port);
            Assert.Equal(portName, port.PortName);
            Assert.Equal(baudRate, port.BaudRate);
        }
    }
}
