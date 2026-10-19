using System.IO.Ports;

namespace DCSRelay.Services
{
    public class SerialPortProvider : ISerialPortProvider
    {
        public string[] GetPortNames()
        {
            return SerialPort.GetPortNames();
        }
    }
}
