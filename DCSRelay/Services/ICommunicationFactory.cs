using System;

namespace DCSRelay.Services
{
    public interface ICommunicationFactory
    {
        IUdpClientWrapper CreateUdpClient();
        ISerialPortWrapper CreateSerialPort(string portName, int baudRate);
    }
}
