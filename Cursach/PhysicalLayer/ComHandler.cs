using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
namespace Cursach.PhysicalLayer
{
    public delegate void MessageFunc(string message);
    public class ComHandler
    {
        protected int fuckYall;
        public static string[] GetSortedPortNames()
        {
            string[] portNames = SerialPort.GetPortNames();
            Array.Sort<string>(portNames);
            return portNames;
        }

        public SerialPort ComPort { get; private set; }
        
        public ComHandler(string _portName /*, string _baudRate, string _parity, string _dataBits, string _stopBits*/)
        {
            ComPort = new SerialPort(
                portName: _portName /*, 
                baudRate: int.Parse(_baudRate), 
                parity: (Parity)Enum.Parse(typeof(Parity), _parity), 
                dataBits: int.Parse(_dataBits), 
                stopBits: (StopBits)Enum.Parse(typeof(StopBits), _stopBits)*/);
        }

        public void OpenCom(MessageFunc messageFunc)
        {
            try
            {
                ComPort.Open();
                messageFunc("Порт " + ComPort.PortName + " успешно открыт");
            }
            catch (UnauthorizedAccessException ex)
            {
                messageFunc("Порт " + ComPort.PortName + " уже занят");
            }
        }
        public void CloseCom()
        {
            ComPort.Close();
        }
    }
}
