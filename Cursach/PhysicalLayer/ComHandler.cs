using System;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace Cursach.PhysicalLayer
{
    public enum PortState
    {
        Opened,
        Occupied,
        InvalidArgs,
        Error,
        Closed,
        Connected
    }

    public class ComHandler
    {
        private byte[] rFrame;
        private int count = 0;
        private int size = 0;

        private SerialPort serialPort;

        public bool IsConnected { get; private set; }

        public Settings FormsManager { get; set; }
        public DatalinkLayer.CanalHandler CanalManager { get; set; }

        public ComHandler()
        {
            serialPort = new SerialPort();
            serialPort.DataReceived += new SerialDataReceivedEventHandler(ComPort_DataReceived);
            serialPort.PinChanged += new SerialPinChangedEventHandler(ComPort_PinChanged);
        }

        ~ComHandler()
        {
            CloseCom();
        }

        /// <summary>
        ///Статический метод, возвращающий сортированный список COM-портов
        /// </summary>
        public static string[] GetSortedPortNames()
        {
            string[] portNames = SerialPort.GetPortNames();
            Array.Sort(portNames);
            return portNames;
        }

        /// <summary>
        /// Открывает COM-порт с заданными параметрами
        /// </summary>
        public PortState OpenCom(string portName, string baudRate, string parity, string dataBits, string stopBits)
        {
            CloseCom();
            serialPort.DtrEnable = false;
            try
            {
                serialPort.PortName = portName.Trim();
                serialPort.BaudRate = int.Parse(baudRate.Trim());
                serialPort.Parity = (Parity)Enum.Parse(typeof(Parity), parity.Trim());
                serialPort.DataBits = int.Parse(dataBits.Trim());
                serialPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), stopBits.Trim());
                serialPort.Encoding = Encoding.Unicode;
                serialPort.Handshake = Handshake.None;
                serialPort.ReadTimeout = 500;
                serialPort.WriteTimeout = 500;

                serialPort.Open();
                //serialPort.DiscardOutBuffer();
                //serialPort.DiscardInBuffer();
                serialPort.DtrEnable = true;
                if (serialPort.DsrHolding)
                {
                    IsConnected = true;
                    return PortState.Connected;
                }
                else
                {
                    return PortState.Opened;
                }
            }
            catch (UnauthorizedAccessException)
            {
                return PortState.Occupied;
            }
            catch (ArgumentOutOfRangeException)
            {
                return PortState.InvalidArgs;
            }
            catch (Exception)
            {
                return PortState.Error;
            }
        }

        public void CloseCom()
        {
            if (serialPort.IsOpen)
            {
                serialPort.Close();
                IsConnected = false;
                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// Обработчик события, возникающего при обрыве соединения
        /// </summary>
        private void ComPort_PinChanged(object sender, SerialPinChangedEventArgs e)
        {
            switch (e.EventType)
            {
                case SerialPinChange.Break:
                    IsConnected = false;
                    FormsManager.ConnectBroke();
                    break;
                case SerialPinChange.DsrChanged:
                    if (serialPort.DsrHolding && !IsConnected)
                    {
                        IsConnected = true;
                        FormsManager.ConnectSuccess();
                    }
                    else if (!serialPort.DsrHolding && IsConnected)
                    {
                        IsConnected = false;
                        FormsManager.ConnectFail();
                    }
                    break;
            }
        }

        /// <summary>
        /// Обработчик события, возникающего при получении данных с COM-порта
        /// </summary>
        private void ComPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //if (serialPort.IsOpen)
            //{
                while (serialPort.BytesToRead > 0)
                {
                    if (size == 0)
                    {
                        size = serialPort.ReadByte();
                        rFrame = new byte[size];
                    }
                    int bytesRead = serialPort.Read(rFrame, count, size - count);
                    count += bytesRead;
                    if (count == size)
                    {
                        count = 0;
                        size = 0;
                        CanalManager.RecieveFrame(rFrame);
                    }
                }
            //}
        }

        public void WriteToCom(byte[] frame)
        {
            byte[] sBuffer = new byte[frame.Length + 1];
            sBuffer[0] = Convert.ToByte(frame.Length);
            for (int i = 1; i < sBuffer.Length; i++)
            {
                sBuffer[i] = frame[i - 1];
            }
            serialPort.Write(sBuffer, 0, sBuffer.Length);
        }
    }
}