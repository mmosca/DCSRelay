using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Ports;

namespace DCSRelay.Services
{

    public class SerialPortWrapper : ISerialPortWrapper
    {
        private readonly SerialPort _port;

        public SerialPortWrapper(string portName, int baudRate)
        {
            _port = new SerialPort(portName)
            {
                BaudRate = baudRate,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                ReadTimeout = 1000,
                WriteTimeout = 1000,
                RtsEnable = true,
                DtrEnable = false,
                Encoding = System.Text.Encoding.ASCII
            };
            _port.DataReceived += HandleDataReceived;
        }

        public string PortName => _port.PortName;
        public bool IsOpen => _port.IsOpen;
        public int BaudRate { get => _port.BaudRate; set => _port.BaudRate = value; }

        private SerialDataReceivedEventHandler _dataReceivedHandler;
        public event SerialDataReceivedEventHandler DataReceived
        {
            add => _dataReceivedHandler += value;
            remove => _dataReceivedHandler -= value;
        }

        private void HandleDataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            _dataReceivedHandler?.Invoke(sender, EventArgs.Empty);
        }

        public void Open() => _port.Open();
        public void Close() => _port.Close();
        public void Write(byte[] buffer, int offset, int count) => _port.Write(buffer, offset, count);
        public string ReadExisting() => _port.ReadExisting();
        public void Dispose() => _port.Dispose();
    }
}
