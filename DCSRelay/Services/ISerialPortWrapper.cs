using System;
using System.Threading;
using System.Threading.Tasks;

namespace DCSRelay.Services
{
    public delegate void SerialDataReceivedEventHandler(object sender, EventArgs e);

    /// <summary>
    /// Abstract wrapper for SerialPort to enable unit testing of serial communication.
    /// </summary>
    public interface ISerialPortWrapper : IDisposable
    {
        string PortName { get; }
        bool IsOpen { get; }
        int BaudRate { get; set; }
        event SerialDataReceivedEventHandler DataReceived;
        void Open();
        void Close();
        void Write(byte[] buffer, int offset, int count);
        string ReadExisting();
    }
}
