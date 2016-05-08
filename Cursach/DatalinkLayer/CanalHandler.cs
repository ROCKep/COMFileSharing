using System;
using System.Collections.Generic;
using System.Text;

namespace Cursach.DatalinkLayer
{
    public class CanalHandler
    {

        private byte[] sFrame; //информационный кадр, который в случае ошибки передается заново

        //ссылки на другие уровни
        public PhysicalLayer.ComHandler ComManager { get; set; }
        public Settings FormsManager { get; set; }

        /// <summary>
        /// Обрабатывает полученный кадр
        /// </summary>
        /// <param name="frame">Закодированный кадр</param>
        public void RecieveFrame(byte[] frame)
        {
                switch (frame[0])
                {
                    case 0x25: //25h - начало передачи файла (BEGIN)
                        string fileName = Encoding.Unicode.GetString(frame, 1, frame.Length - 1);
                        FormsManager.SavePrompt(fileName);
                        break;
                    case 0x18: //18h - отмена передачи файла (CANCEL)
                        FormsManager.TransmissionCancel();
                        break;
                    case 0x6: //06h - положительная квитанция (ACK)
                        SendData();
                        break;
                    case 0x14: //14h - отрицательная квитанция (NAK)
                        SendAgain();
                        break;
                    case 0x40: //40h - информационный кадр (DATA)
                        if (Check(frame))
                        {
                            byte[] rFrame = Decode(frame);
                            FormsManager.WriteToFile(rFrame);
                            Acknowledge();
                        }
                        else
                        {
                            NotAcknowledge();
                        }
                        break;
                    case 0x4: //04h - конец файла (EOF)
                        FormsManager.ReceiveSuccess();
                        break;
                }
        }

        /// <summary>
        /// Отправляет кадр начала передачи
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        public void SendFile(string fileName)
        {
            byte[] BEGIN = new byte[Encoding.Unicode.GetByteCount(fileName) + 1];
            BEGIN[0] = 0x25;
            Encoding.Unicode.GetBytes(fileName, 0, fileName.Length, BEGIN, 1);
            ComManager.WriteToCom(BEGIN);
        }

        /// <summary>
        /// Отправляет отрицательную квитанцию
        /// </summary>
        private void NotAcknowledge()
        {
            byte[] NAK = { 0x14 };
            ComManager.WriteToCom(NAK);
        }

        public PhysicalLayer.PortState Connect(string portName, string baudRate, string parity, string dataBits, string stopBits)
        {
            PhysicalLayer.PortState portState = ComManager.OpenCom(portName, baudRate, parity, dataBits, stopBits);
            ComManager.SendDtr();
            if (ComManager.IsConnected)
            {
                return PhysicalLayer.PortState.Connected;
            }
            return portState;
        }

        /// <summary>
        /// Отправляет заново информационный кадр
        /// </summary>
        private void SendAgain()
        {
            ComManager.WriteToCom(sFrame);
        }

        /// <summary>
        /// Отправляет информационный кадр или EOF
        /// </summary>
        private void SendData()
        {   
            byte[] DATA = new byte[64];
            int bytesRead = FormsManager.ReadFromFile(DATA);
            if (bytesRead > 0)
            {
                Array.Resize(ref DATA, bytesRead);
                sFrame = Encode(DATA);
                ComManager.WriteToCom(sFrame);
            }
            else
            {
                byte[] EOF = { 0x4 };
                ComManager.WriteToCom(EOF);
                FormsManager.SendSuccess();
            }
        }

        /// <summary>
        /// Отправляет положительную квитанцию
        /// </summary>
        public void Acknowledge()
        {
            byte[] ACK = { 0x6 };
            ComManager.WriteToCom(ACK);
        }

        /// <summary>
        /// Отправляет кадр отмены передачи
        /// </summary>
        public void Abort()
        {
            byte[] CANCEL = { 0x18 }; 
            ComManager.WriteToCom(CANCEL);
        }

        /// <summary>
        /// Проверяет полученный кадр на ошибки
        /// </summary>
        /// <param name="bytes">Закодированный кадр</param>
        /// <returns></returns>
        private bool Check(byte[] bytes)
        {
            bool check = true;
            int length = bytes.Length;
            byte[] mid = new byte[length];
            for (int i = 1; i < length; i++) //начинаем с единицы из-за 0 байта с кодом 
            {
                byte[] tmp = toarr(bytes[i]);
                byte[] ham = new byte[7];
                for (int a = 0; a < 7; a++)
                {
                    ham[a] = tmp[a + 1];
                }
                if (!HamCheck(ham))
                {
                    check = false;
                    break;
                }
            }
            return check;
        }

        public void NotConnected()
        {
            ComManager.CloseCom();
            FormsManager.ConnectFail();
        }

        public void Connected()
        {
            FormsManager.ConnectSuccess();
        }

        /// <summary>
        /// Декодирует полученный кадр
        /// </summary>
        /// <param name="bytes">Закодированный кадр</param>
        /// <returns></returns>
        private byte[] Decode(byte[] bytes)
        {
            int length = bytes.Length;
            List<byte[]> tmpBytes = new List<byte[]>();
            byte[] mid = new byte[length];
            for (int i = 1; i < length; i++) //начинаем с единицы из-за 0 байта с кодом 
            {
                byte[] tmp = toarr(bytes[i]);
                byte[] ham = new byte[7];
                for (int a = 0; a < 7; a++)
                {
                    ham[a] = tmp[a + 1];
                }
                byte[] halfInBits = UnHam(ham);
                tmpBytes.Add(halfInBits);
            }
            length--;
            //соединяем половинки в целые байты 
            byte[][] FullBytes = new byte[length / 2][];
            byte[] nul = { 0, 0, 0, 0, 0, 0, 0, 0 };
            for (int i = 0; i < length / 2; i++)
            {
                FullBytes[i] = ret(nul, 8);
            }
            byte[] rez;
            int add = 0;
            int x = 0;
            for (int c = 0; c < length; c++)
            {
                for (int a = 0; a < 4; a++)
                {
                    FullBytes[x][a + add] = tmpBytes[c][a];
                    if (a == 3 && add != 4)
                    {
                        c++;
                        add = 4;
                        a = -1;
                    }
                }
                x++;
                add = 0;
            }

            //преобразуем из псевдодвоичного в нормальный вид 

            rez = new byte[length / 2];

            for (int i = 0; i < length / 2; i++)
            {
                rez[i] = tobyte(FullBytes[i]);
            }
            return rez;
        }

        /// <summary>
        /// Кодирует пересылаемый кадр
        /// </summary>
        /// <param name="bytes">Исходный кадр</param>
        /// <returns></returns>
        private byte[] Encode(byte[] bytes)
        {
            int size = bytes.Length;

            byte[][] FullByteArray;
            byte[][] HalfByteArray;
            byte[][] CodeByteArray;
            byte[] exmp = new byte[4];
            byte[][] mid;
            byte[] result;
            byte[] nul = { 0, 0, 0, 0, 0, 0, 0, 0 };
            byte[] rez = new byte[2 * size];
            FullByteArray = new byte[size][]; //массив для записи считанных байтов в псевдодвоичном виде (ПДД) 
            HalfByteArray = new byte[size * 2][]; //массив для записи байтов в ПДД, разделенных на половинки 
            CodeByteArray = new byte[2 * size][]; //массив для записи байтов в ПДД, закодированных кодом хэмминга 
            mid = new byte[2 * size][]; //массив для хранения промежуточных данных 
            result = new byte[(2 * size) + 1]; //лишний байт для кода, массив для хранения байтов, готовых для отправки 
            for (int a = 0; a < size; a++)
            {
                FullByteArray[a] = toarr(bytes[a]);
            }
            //делим на блоки по 4 байта 
            int b = 0;
            int add = 0;
            for (int a = 0; a < size; a++)
            {
                for (int x = 0; x < 4; x++)
                {
                    exmp[x] = FullByteArray[a][x + add];
                    if (x == 3 && add != 4)
                    {
                        HalfByteArray[b] = ret(exmp, 4);
                        b++;
                        add = 4;
                        x = -1;
                    }
                }
                HalfByteArray[b] = ret(exmp, 4);
                add = 0;
                b++;
            }
            //кодируем кодом Хэмминга 
            for (int a = 0; a < 2 * size; a++)
            {
                CodeByteArray[a] = Ham(HalfByteArray[a]);
            }
            //преобразуем в байты для передачи 
            //суть преобразования - из массивов по 7 знаков (7битных чисел) сделать массивы по 8 знаков, дописав ноль в начале 
            for (int a = 0; a < 2 * size; a++)
            {
                mid[a] = ret(nul, 8);
            }
            for (int a = 0; a < 2 * size; a++)
            {
                for (int c = 0; c < 7; c++)
                {
                    mid[a][c + 1] = CodeByteArray[a][c];
                }
            }
            result[0] = 0x40;
            for (int a = 0; a < 2 * size; a++)
            {
                result[a + 1] = tobyte(mid[a]);
            }

            return result;
        }

        private bool HamCheck(byte[] b)
        {
            byte check;
            check = sum(b[2], sum(b[4], b[6]));
            if (check != b[0])
                return false;
            check = sum(b[2], sum(b[5], b[6]));
            if (check != b[1])
                return false;
            check = sum(b[4], sum(b[5], b[6]));
            if (check != b[3])
                return false;
            return true;
        }

        private byte[] UnHam(byte[] code)
        {
            byte[] rez = new byte[4];
            rez[0] = code[2];
            rez[1] = code[4];
            rez[2] = code[5];
            rez[3] = code[6];
            return rez;
        }


        private byte invert(byte b)
        {
            if (b == 0)
                return 1;
            else
                return 0;
        }

        private byte sum(byte a, byte b)
        {
            if (a == 0 && b == 0 || a == 1 && b == 1)
                return 0;
            else
                return 1;
        }

        private byte[] ret(byte[] b, int n)
        {
            byte[] x = new byte[n];
            for (int i = 0; i < 4; i++)
            {
                x[i] = b[i];
            }
            return x;
        }

        private byte[] toarr(int n)
        {
            byte[] rez = new byte[8];
            int m = 128;
            byte s;
            for (int i = 0; i < 8; i++)
            {
                s = Convert.ToByte(n / m);
                if (s != 0)
                {
                    n = n - m;
                    rez[i] = s;

                }
                m = Convert.ToByte(m / 2);
            }
            return rez;
        }

        private byte tobyte(byte[] b)
        {
            byte rez = 0;
            int m = 1;
            for (int i = 7; i > -1; i--)
            {
                rez = Convert.ToByte(rez + (m * b[i]));
                m = m * 2;
            }
            return rez;
        }

        private byte[] Ham(byte[] b)
        {
            byte[] code = new byte[7];
            code[0] = 0;
            code[1] = 0;
            code[3] = 0;
            code[2] = b[0];
            code[4] = b[1];
            code[5] = b[2];
            code[6] = b[3];
            code[0] = sum(sum(code[0], code[2]), sum(code[4], code[6]));
            code[1] = sum(sum(code[1], code[2]), sum(code[5], code[6]));
            code[3] = sum(sum(code[3], code[4]), sum(code[5], code[6]));

            return code;
        }

    }
}
