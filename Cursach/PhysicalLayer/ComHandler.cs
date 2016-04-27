﻿using System;
using System.IO.Ports;
using System.Threading;

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
        public byte[] ReadBuffer { get; private set; }
        public byte[] WriteBuffer { get; private set; }
        public SerialPort ComPort { get; private set; }

        public CanalLayer.SFile sFile;

        public ComHandler(CanalLayer.SFile sFile)
        {
            ComPort = new SerialPort();
            ComPort.DataReceived += new SerialDataReceivedEventHandler(ComPort_DataReceived);
            ComPort.PinChanged += new SerialPinChangedEventHandler(ComPort_PinChanged);
            this.sFile = sFile;
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
            this.sFile.ReceiveNewBlock(ReadBuffer);
            this.sFile.isReceived = true;
        }
   
        public void WriteToCom(byte[] buffer)
        {
            ComPort.Write(buffer, 0, buffer.Length);
        }
    }
}
