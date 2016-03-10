using System;
using System.IO.Ports;
using System.Threading;

namespace Cursach.PhysicalLayer
{
    public delegate void MessageFunc(string message);

    public enum PortState
    {
        Opened,
        Occupied,
        InvalidArgs,
        Error,
        Closed
    }

    public class ComHandler
    {
        public byte[] ReadBuffer { get; private set; }
        public byte[] WriteBuffer { get; private set; }
        public SerialPort ComPort { get; private set; }

        public ComHandler()
        {
            ComPort = new SerialPort();
            ComPort.DataReceived += new SerialDataReceivedEventHandler(ComPort_DataReceived);
            ComPort.PinChanged += new SerialPinChangedEventHandler(ComPort_PinChanged);
        }

        /// <summary>
        ///Статический метод, возвращающий сортированный список COM-портов
        /// </summary>
        public static string[] GetSortedPortNames()
        {
            string[] portNames = SerialPort.GetPortNames();
            Array.Sort<string>(portNames);
            return portNames;
        }


        public PortState SetupCom(string portName, string baudRate, string parity, string dataBits, string stopBits)
        {
            if (ComPort.IsOpen)
            {
                try
                {
                    ComPort.Close();
                    Thread.Sleep(500);
                }
                catch
                {
                    return PortState.Error;
                }
            }

            try
            {
                ComPort.PortName = portName.Trim();
                ComPort.BaudRate = int.Parse(baudRate.Trim());
                ComPort.Parity = (Parity)Enum.Parse(typeof(Parity), parity.Trim());
                ComPort.DataBits = int.Parse(dataBits.Trim());
                ComPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), stopBits.Trim());
                ComPort.Open();
                return PortState.Opened;
            }
            catch (InvalidOperationException ex)
            {
                return PortState.Occupied;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return PortState.InvalidArgs;
            }
            catch (Exception ex)
            {
                return PortState.Error;
            }           
        }



        /// <summary>
        /// Метод, открывающий порт с заданными параметрами
        /// </summary>
        public PortState OpenCom(string portName, string baudRate, string parity, string dataBits, string stopBits /*, MessageFunc messageFunc*/)
        {
            if (ComPort == null)
            {
                ComPort = new SerialPort();
                ComPort.DataReceived += new SerialDataReceivedEventHandler(ComPort_DataReceived);
                ComPort.PinChanged += new SerialPinChangedEventHandler(ComPort_PinChanged);
            }
            ComPort.PortName = portName;
            ComPort.BaudRate = int.Parse(baudRate.Trim());
            ComPort.Parity = (Parity)Enum.Parse(typeof(Parity), parity.Trim());
            ComPort.DataBits = int.Parse(dataBits.Trim());
            ComPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), stopBits);
            try
            {
                ComPort.Open();
                return PortState.Opened;
            }
            catch (UnauthorizedAccessException ex)
            {
                return PortState.Occupied;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return PortState.InvalidArgs;
            }
            catch (Exception ex)
            {
                return PortState.Error;
            }
        }

        /// <summary>
        /// Метод, закрывающий порт
        /// </summary>
        public PortState CloseCom(/*MessageFunc messageFunc*/)
        {
            try
            {
                ComPort.Close();
                return PortState.Closed;
            }
            catch (Exception ex)
            {
                return PortState.Error;
            }
        }

        /// <summary>
        /// Обработчик события, возникающего при обрыве соединения
        /// </summary>
        private void ComPort_PinChanged(object sender, SerialPinChangedEventArgs e)
        {
            if (e.EventType == SerialPinChange.Break)
            {
                Thread.Sleep(500);
                if (ComPort.BreakState == true /*!this.ComPort.CDHolding*/)
                {
                    ComPort.Close();    
                }
            }
        }

        /// <summary>
        /// Обработчик события, возникающего при получении данных с COM-порта
        /// </summary>
        private void ComPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int bytes = ComPort.BytesToRead;
            ReadBuffer = new byte[bytes];
            ComPort.Read(ReadBuffer, 0, bytes);
        }
        
        public void WriteToCom(byte[] buffer)
        {
            ComPort.Write(buffer, 0, buffer.Length);
        }
    }
}
