namespace DCSRelay.Services
{
    public class CommunicationFactory : ICommunicationFactory
    {
        public IUdpClientWrapper CreateUdpClient() => new UdpClientWrapper();
        public ISerialPortWrapper CreateSerialPort(string portName, int baudRate) => new SerialPortWrapper(portName, baudRate);
    }
}
