using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KursST
{
    class SFile
    {
        private int l;
        private int blockSize = 1024;
        private int blockNumber;
        private string path;
        private List<byte> convertedBytes = new List<byte>();

        public SFile(string path)
        {
            this.path = path;
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

        public byte[] ForSend()
        {
            //считываем файл

            byte[] ReadArray;
            FileStream fstream = File.OpenRead(this.path);
            ReadArray = new byte[fstream.Length];
            fstream.Read(ReadArray, 0, ReadArray.Length);
            fstream.Close();

            this.l = ReadArray.Length;
            this.blockNumber = this.l / this.blockSize;
            if (this.l % this.blockSize != 0)
                this.blockNumber++;

            byte[][] FullByteArray;
            byte[][] HalfByteArray;
            byte[][] CodeByteArray;
            byte[] exmp = new byte[4];
            byte[][] mid;
            byte[] nul = { 0, 0, 0, 0, 0, 0, 0, 0 };
            byte[] rez = new byte[2 * l];
            int shift = 0;
            int border = this.blockSize;
            int checkLength = this.l;

            for (int y = 0; y < this.blockNumber; y++)
            {
                if (checkLength < this.blockSize)
                    border = checkLength;

                FullByteArray = new byte[border][];
                HalfByteArray = new byte[border * 2][];
                CodeByteArray = new byte[2 * border][];
                mid = new byte[2 * border][];

                for (int i = 0; i < border; i++)
                {
                    FullByteArray[i] = toarr(ReadArray[i + shift]);
                }

                //делим на блоки по 4 байта

                int b = 0;
                int add = 0;

                for (int i = 0; i < border; i++)
                {
                    for (int x = 0; x < 4; x++)
                    {
                        exmp[x] = FullByteArray[i][x + add];
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

                for (int i = 0; i < 2 * border; i++)
                {
                    CodeByteArray[i] = Ham(HalfByteArray[i]);
                }

                //преобразуем в байты для передачи

                for (int i = 0; i < 2 * border; i++)
                {
                    mid[i] = ret(nul, 8);
                }

                for (int i = 0; i < 2 * border; i++)
                {
                    for (int a = 0; a < 7; a++)
                    {
                        mid[i][a + 1] = CodeByteArray[i][a];
                    }
                }

                for (int i = 0; i < 2 * border; i++)
                {
                    this.convertedBytes.Add(tobyte(mid[i]));
                }
                shift = shift + this.blockSize;
                checkLength = checkLength - this.blockSize;
            }

            for (int i = 0; i < 2 * this.l; i++)
            {
                rez[i] = this.convertedBytes[i];
            }

            return rez;
        }

    }


    class RFile
    {
        private int l;
        private string path;
        private List<byte[]> ReceivedHalfs = new List<byte[]>();
        private int blockSize = 1024;
        private int blockNumber;

        public RFile(string path)
        {
            this.path = path;
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

        private byte sum(byte a, byte b)
        {
            ;
            if (a == 0 && b == 0 || a == 1 && b == 1)
                return 0;
            else
                return 1;

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

        public void ReceiveHalfByte(byte b)
        {
            byte[] mid = toarr(b);
            byte[] ham = new byte[7];
            for (int i = 0; i < 7; i++)
            {
                ham[i] = mid[i + 1];
            }
            if (!HamCheck(ham))
            {
                //TODO: сделать уведомление о неправильной передаче
            }
            if (HamCheck(ham))
            {
                //TODO: сделать уведомление о правильной передаче

                byte[] halfInBits = UnHam(ham);
                this.ReceivedHalfs.Add(halfInBits);
            }
        }

        public void MakeFile()
        {
            int l = this.ReceivedHalfs.Count;
            this.blockNumber = this.ReceivedHalfs.Count / this.blockSize;

            byte[][] mid = new byte[l / 2][];
            byte[] nul = { 0, 0, 0, 0, 0, 0, 0, 0 };
            byte[] rez;
            for (int i = 0; i < l / 2; i++)
            {
                mid[i] = ret(nul, 8);
            }

            int x = 0;
            int add = 0;

            //соединяем половинки в целые байты

            for (int i = 0; i < l; i++)
            {
                for (int a = 0; a < 4; a++)
                {
                    mid[x][a + add] = this.ReceivedHalfs[i][a];
                    if (a == 3 && add != 4)
                    {
                        i++;
                        add = 4;
                        a = -1;
                    }
                }
                x++;
                add = 0;
            }

            //преобразуем из псевдодвоичного в нормальный вид

            rez = new byte[l / 2];

            for (int i = 0; i < l / 2; i++)
            {
                rez[i] = tobyte(mid[i]);
            }

            FileStream fstream = new FileStream(this.path, FileMode.OpenOrCreate);

            // запись массива байтов в файл
            fstream.Write(rez, 0, rez.Length);
            Console.WriteLine("Файл передан");
            fstream.Close();
        }
    }
}
