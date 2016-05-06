using System;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Windows;

namespace Cursach.PhysicalLayer
{
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
        private byte[] rFrame;
        private int count = 0;
        private int size = 0;

        private SerialPort serialPort;
        private bool isConnected = false;

        public Settings FormsManager { get; set; }
        public DatalinkLayer.CanalHandler CanalManager { get; set; }

        public ComHandler()
        {
            serialPort = new SerialPort();
            serialPort.DataReceived += new SerialDataReceivedEventHandler(ComPort_DataReceived);
            serialPort.PinChanged += new SerialPinChangedEventHandler(ComPort_PinChanged);
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
                serialPort.DtrEnable = true;
                return PortState.Opened;
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
                    MessageBox.Show("Обрыв соединения");
                    isConnected = false;
                    break;
                case SerialPinChange.DsrChanged:
                    if (serialPort.DsrHolding && !isConnected)
                    {
                        serialPort.RtsEnable = true;
                        isConnected = true;
                        MessageBox.Show("Соединение установлено");
                    }
                    else if (!serialPort.DsrHolding && isConnected)
                    {
                        isConnected = false;
                        MessageBox.Show("Соединение с другим компьютером прервано");
                    }
                    break;
                case SerialPinChange.CtsChanged:
                    if (serialPort.CtsHolding && !isConnected)
                    {
                        isConnected = true;
                        MessageBox.Show("Соединение установлено");
                    }
                    else if (!serialPort.CtsHolding && isConnected)
                    {
                        isConnected = false;
                        MessageBox.Show("Соединение с другим компьютером прервано");
                    }
                    break;
            }

        }

        /// <summary>
        /// Обработчик события, возникающего при получении данных с COM-порта
        /// </summary>
        private void ComPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            while (serialPort.BytesToRead > 0)
            {
                try
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
                catch(TimeoutException)
                {
                }
            }
        }

        public void WriteToCom(byte[] frame)
        {
            byte[] sBuffer = new byte[frame.Length + 1];
            sBuffer[0] = Convert.ToByte(frame.Length);
            for (int i = 1; i < sBuffer.Length; i++)
            {
                sBuffer[i] = frame[i - 1];
            }
            try
            {
                serialPort.Write(sBuffer, 0, sBuffer.Length);
            }
            catch (TimeoutException)
            {
            }
        }
    }
}