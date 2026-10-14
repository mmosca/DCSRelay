namespace DCSRelay.Models
{
    /// <summary>
    /// Holds the detailed hardware information for a specific serial port.
    /// </summary>
    public class HardwareDeviceInfo
    {
        /// <summary>
        /// The logical name of the port (e.g., "COM3").
        /// </summary>
        public string PortName { get; set; }

        /// <summary>
        /// The name of the device.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The descriptive name or caption of the device.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Vendor ID (VID) reported by the hardware.
        /// </summary>
        public string VendorID { get; set; }

        /// <summary>
        /// Product ID (PID) reported by the hardware.
        /// </summary>
        public string ProductID { get; set; }

        /// <summary>
        /// The Plug and Play device ID.
        /// </summary>
        public string PnpDeviceID { get; set; }

        /// <summary>
        /// The Plug and Play device ID.
        /// </summary>
        public string DeviceID { get; set; }

        public override string ToString()
        {
            return $"{PortName}: {Name} ({DeviceID})";
        }

        override public bool Equals(object? obj)
        {
            if (obj is HardwareDeviceInfo other)
            {
                return this.PortName == other.PortName &&
                       this.Name == other.Name &&
                       this.Description == other.Description &&
                       this.VendorID == other.VendorID &&
                       this.ProductID == other.ProductID &&
                       this.PnpDeviceID == other.PnpDeviceID &&
                       this.DeviceID == other.DeviceID;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PortName, Name, Description, VendorID, ProductID, PnpDeviceID, DeviceID);
        }
    }
}