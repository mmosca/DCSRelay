using DCSRelay.Models;
using Xunit;

namespace DCSRelay.Tests.Models
{
    public class HardwareDeviceInfoTests
    {
        [Fact]
        public void ToString_ReturnsExpectedFormat()
        {
            // Arrange
            var device = new HardwareDeviceInfo
            {
                PortName = "COM3",
                Name = "USB Serial Port",
                DeviceID = "USB\\VID_123&PID_456"
            };

            // Act
            var result = device.ToString();

            // Assert
            Assert.Equal("COM3: USB Serial Port (USB\\VID_123&PID_456)", result);
        }

        [Fact]
        public void Equals_WithSameProperties_ReturnsTrue()
        {
            // Arrange
            var device1 = new HardwareDeviceInfo
            {
                PortName = "COM3",
                Name = "Device A",
                Description = "Desc A",
                VendorID = "VID1",
                ProductID = "PID1",
                PnpDeviceID = "PNP1",
                DeviceID = "DEV1"
            };

            var device2 = new HardwareDeviceInfo
            {
                PortName = "COM3",
                Name = "Device A",
                Description = "Desc A",
                VendorID = "VID1",
                ProductID = "PID1",
                PnpDeviceID = "PNP1",
                DeviceID = "DEV1"
            };

            // Act & Assert
            Assert.True(device1.Equals(device2));
        }

        [Fact]
        public void Equals_WithDifferentProperties_ReturnsFalse()
        {
            // Arrange
            var device1 = new HardwareDeviceInfo { PortName = "COM3" };
            var device2 = new HardwareDeviceInfo { PortName = "COM4" };

            // Act & Assert
            Assert.False(device1.Equals(device2));
        }

        [Fact]
        public void Equals_WithNullOrDifferentType_ReturnsFalse()
        {
            // Arrange
            var device = new HardwareDeviceInfo { PortName = "COM3" };

            // Act & Assert
            Assert.False(device.Equals(null));
            Assert.False(device.Equals("not a HardwareDeviceInfo"));
        }

        [Fact]
        public void GetHashCode_WithSameProperties_ReturnsSameHash()
        {
            // Arrange
            var device1 = new HardwareDeviceInfo
            {
                PortName = "COM3",
                Name = "Device A",
                Description = "Desc A",
                VendorID = "VID1",
                ProductID = "PID1",
                PnpDeviceID = "PNP1",
                DeviceID = "DEV1"
            };

            var device2 = new HardwareDeviceInfo
            {
                PortName = "COM3",
                Name = "Device A",
                Description = "Desc A",
                VendorID = "VID1",
                ProductID = "PID1",
                PnpDeviceID = "PNP1",
                DeviceID = "DEV1"
            };

            // Act & Assert
            Assert.Equal(device1.GetHashCode(), device2.GetHashCode());
        }

        [Fact]
        public void GetHashCode_WithDifferentProperties_ReturnsDifferentHash()
        {
            // Arrange
            var device1 = new HardwareDeviceInfo { PortName = "COM3" };
            var device2 = new HardwareDeviceInfo { PortName = "COM4" };

            // Act & Assert
            Assert.NotEqual(device1.GetHashCode(), device2.GetHashCode());
        }
    }
}
