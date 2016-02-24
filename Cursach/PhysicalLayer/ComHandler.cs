using System;
using System.IO.Ports;
namespace Cursach.PhysicalLayer
{
    public delegate void MessageFunc(string message);

    public class ComHandler
    {
        protected int fuckYall;

        public byte[] Buffer { get; private set; }
        public SerialPort ComPort { get; private set; }

        public static string[] GetSortedPortNames()
        {
            string[] portNames = SerialPort.GetPortNames();
            Array.Sort<string>(portNames);
            return portNames;
        }

        public void OpenCom(string portName, string baudRate, string parity, string dataBits, string stopBits, MessageFunc messageFunc)
        {
            if (ComPort == null)
            {
                ComPort = new SerialPort();
                ComPort.DataReceived += new SerialDataReceivedEventHandler(ComPort_DataReceived);
            }

            ComPort.PortName = portName;
            ComPort.BaudRate = int.Parse(baudRate.Trim());
            ComPort.Parity = (Parity)Enum.Parse(typeof(Parity), parity.Trim());
            ComPort.DataBits = int.Parse(dataBits.Trim());
            ComPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), stopBits);

            try
            {
                ComPort.Open();
                messageFunc("Порт " + ComPort.PortName + " успешно открыт");
            }
            catch (UnauthorizedAccessException ex)
            {
                messageFunc("Порт " + ComPort.PortName + " уже занят");
            }
            catch (ArgumentOutOfRangeException ex)
            {
                messageFunc("Параметры COM-порта заданы некорректно");
            }
        }

        private void ComPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int bytes = ComPort.BytesToRead;
            Buffer = new byte[bytes];
            ComPort.Read(Buffer, 0, bytes);

        }

        public void CloseCom(MessageFunc messageFunc)
        {
            ComPort.Close();
            messageFunc("Порт " + ComPort.PortName + " успешно закрыт");
        }

        public void WriteToCom(byte[] buffer)
        {
            ComPort.Write(buffer, 0, buffer.Length);
        }
    }
}
