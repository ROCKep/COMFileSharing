using System;
using System.Collections.Generic;
using System.IO;

namespace Cursach.CanalLayer
{

    public class SFile
    {
        public byte startByte = 66;
        public byte endByte = 56;
        protected byte trueRecByte = 36;

        protected string sendPath;
        protected string receivePath;
        protected int sendBlockSize = 8; //количество байт для отправки и приема
        protected bool ReceivedRight = false; //переменная для проверки правильности отправки
        public bool isReceived = false;
        public byte ReceivedSignalByte;

        public PhysicalLayer.ComHandler comHandler;

        public SFile(PhysicalLayer.ComHandler comHandler)
        {
            this.comHandler = comHandler;
        }

        public string getSendPath()
        {
            return this.sendPath;
        }

        public void setSendPath(string s)
        {
            this.sendPath = s;
        }

        public string getReceiveFile()
        {
            return this.receivePath;
        }

        public void setReceivePath(string s)
        {
            this.receivePath = s;
        }

        protected void SendSignal(byte b)
        {
            byte[] send = new byte[1];
            send[0] = b;
            this.comHandler.WriteToCom(send);
        }

        protected void ReceiveSignal(byte[] b)
        {
            byte temp = b[0];
            if (temp == this.trueRecByte)
                this.ReceivedRight = true;
            else
                this.ReceivedSignalByte = temp;
        }

        //отправка

        

        public void SendFile()
        {
            ReceivedRight = false;
            byte[][] FullByteArray;
            byte[][] HalfByteArray;
            byte[][] CodeByteArray;
            byte[] exmp = new byte[4];
            byte[][] mid;
            byte[] result;
            byte[] nul = { 0, 0, 0, 0, 0, 0, 0, 0 };
            byte[] rez = new byte[2 * this.sendBlockSize];
            byte[] ReadArray;
            FileStream fstream = File.OpenRead(this.sendPath);
            long fileLength = fstream.Length;
            long readSize = 4;
            for (long i = 0; i < fileLength; i += readSize)
            {
                if ((fileLength - i) < 4)
                    readSize = fileLength - i;
                ReadArray = new byte[readSize]; //массив для считывания куска файла
                fstream.Read(ReadArray, 0, ReadArray.Length); 
                FullByteArray = new byte[readSize][]; //массив для записи считанных байтов в псевдодвоичном виде (ПДД)
                HalfByteArray = new byte[readSize * 2][]; //массив для записи байтов в ПДД, разделенных на половинки
                CodeByteArray = new byte[2 * readSize][]; //массив для записи байтов в ПДД, закодированных кодом хэмминга
                mid = new byte[2 * readSize][]; //массив для хранения промежуточных данных
                result = new byte[2 * readSize]; //массив для хранения байтов, готовых для отправки
                for (int a = 0; a < readSize; a++)
                {
                    FullByteArray[a] = toarr(ReadArray[a]);
                }
                //делим на блоки по 4 байта
                int b = 0;
                int add = 0;
                for (int a = 0; a < readSize; a++)
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
                for (int a = 0; a < 2 * readSize; a++)
                {
                    CodeByteArray[a] = Ham(HalfByteArray[a]);
                }
                //преобразуем в байты для передачи
                //суть преобразования - из массивов по 7 знаков (7битных чисел) сделать массивы по 8 знаков, дописав ноль в начале
                for (int a = 0; a < 2 * readSize; a++)
                {
                    mid[a] = ret(nul, 8);
                }
                for (int a = 0; a < 2 * readSize; a++)
                {
                    for (int c = 0; c < 7; c++)
                    {
                        mid[a][c + 1] = CodeByteArray[a][c];
                    }
                }
                for (int a = 0; a < 2 * readSize; a++)
                {
                    result[a] = tobyte(mid[a]);
                }
                //result - готовый для отправки блок
                this.comHandler.WriteToCom(result);
                while(!isReceived)
                {

                }
                if (!this.ReceivedRight)
                    i += readSize;
            }
            fstream.Close();
        }

        public void SendBlock(byte[] result)
        {
            this.comHandler.WriteToCom(result);
            //return this.ReceiveSignal();
        }

        //прием





        public void ReceiveBlock(byte[] b)
        {
            int length = b.Length;
            List<byte[]> tmpBytes = new List<byte[]>();
            byte[] mid = new byte[length];
            bool check = true;
            for (int i = 0; i < length; i++)
            {
                byte[] tmp = toarr(b[i]);
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
                if (HamCheck(ham))
                {
                    byte[] halfInBits = UnHam(ham);
                    tmpBytes.Add(halfInBits);
                }
            }
            if (check)
            {
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

                FileStream fstream = new FileStream(this.receivePath, FileMode.OpenOrCreate);

                // запись массива байтов в файл
                fstream.Seek(0, SeekOrigin.End);
                fstream.Write(rez, 0, rez.Length);
                Console.WriteLine("Блок записан!");
                fstream.Close();

                this.SendSignal(this.trueRecByte);
            }
            /*
            if (!check)
            {
                this.SendSignal(this.falseRecByte);
            }
            */
        }

        public void ReceiveNewBlock(byte[] b)
        {
            if (b.Length == 1)
                ReceiveSignal(b);
            if (b.Length > 1)
                ReceiveBlock(b);
        }



        //ВОТ ЭТУ ПОЕБЕНЬ КОПИРОВАТЬ


        public byte[] decode(byte[] bytes)
        {
            int length = bytes.Length;
            List<byte[]> tmpBytes = new List<byte[]>();
            byte[] mid = new byte[length];
            bool check = true;
            for (int i = 0; i < length; i++)
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
                if (HamCheck(ham))
                {
                    byte[] halfInBits = UnHam(ham);
                    tmpBytes.Add(halfInBits);
                }
            }
            if (check)
            {
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

            //что-то происходящее при ошибке, пока пусть так
            byte[] err = new byte[1];
            err[0] = 0x13;
            return err;
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


        protected byte invert(byte b)
        {
            if (b == 0)
                return 1;
            else
                return 0;
        }

        protected byte sum(byte a, byte b)
        {
            if (a == 0 && b == 0 || a == 1 && b == 1)
                return 0;
            else
                return 1;
        }

        protected byte[] ret(byte[] b, int n)
        {
            byte[] x = new byte[n];
            for (int i = 0; i < 4; i++)
            {
                x[i] = b[i];
            }
            return x;
        }

        protected byte[] toarr(int n)
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

        protected byte tobyte(byte[] b)
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

        public byte[] encode(byte[] bytes)
        {
            int readSize = 8;

            byte[][] FullByteArray;
            byte[][] HalfByteArray;
            byte[][] CodeByteArray;
            byte[] exmp = new byte[4];
            byte[][] mid;
            byte[] result;
            byte[] nul = { 0, 0, 0, 0, 0, 0, 0, 0 };
            byte[] rez = new byte[2 * this.sendBlockSize];
            FullByteArray = new byte[readSize][]; //массив для записи считанных байтов в псевдодвоичном виде (ПДД)
            HalfByteArray = new byte[readSize * 2][]; //массив для записи байтов в ПДД, разделенных на половинки
            CodeByteArray = new byte[2 * readSize][]; //массив для записи байтов в ПДД, закодированных кодом хэмминга
            mid = new byte[2 * readSize][]; //массив для хранения промежуточных данных
            result = new byte[2 * readSize]; //массив для хранения байтов, готовых для отправки
            for (int a = 0; a < readSize; a++)
            {
                FullByteArray[a] = toarr(bytes[a]);
            }
            //делим на блоки по 4 байта
            int b = 0;
            int add = 0;
            for (int a = 0; a < readSize; a++)
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
            for (int a = 0; a < 2 * readSize; a++)
            {
                CodeByteArray[a] = Ham(HalfByteArray[a]);
            }
            //преобразуем в байты для передачи
            //суть преобразования - из массивов по 7 знаков (7битных чисел) сделать массивы по 8 знаков, дописав ноль в начале
            for (int a = 0; a < 2 * readSize; a++)
            {
                mid[a] = ret(nul, 8);
            }
            for (int a = 0; a < 2 * readSize; a++)
            {
                for (int c = 0; c < 7; c++)
                {
                    mid[a][c + 1] = CodeByteArray[a][c];
                }
            }
            for (int a = 0; a < 2 * readSize; a++)
            {
                result[a] = tobyte(mid[a]);
            }

            return result;
        }

    }    
}
