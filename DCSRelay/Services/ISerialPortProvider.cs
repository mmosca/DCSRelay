using System.Collections.Generic;

namespace DCSRelay.Services
{
    public interface ISerialPortProvider
    {
        string[] GetPortNames();
    }
}
