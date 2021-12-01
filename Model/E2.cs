using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CourseProject.Model
{
	public class E2
	{
        public int BlockSize = 16;

        public byte[] Key = new byte[16];
        private int key_bytes = 16;
        private const int num_of_keys = 16;

        public int Size => 16;

        private static byte[] SBox = {
                     225, 66, 62, 129, 78, 23, 158, 253, 180, 63, 44, 218, 49, 30, 224, 65,
                     204, 243, 130, 125, 124, 18, 142, 187, 228, 88, 21, 213, 111, 233, 76, 75,
                     53, 123, 90, 154, 144, 69, 188, 248, 121, 214, 27, 136, 2, 171, 207, 100,
                     9, 12, 240, 1, 164, 176, 246, 147, 67, 99, 134, 220, 17, 165, 131, 139,
                     201, 208, 25, 149, 106, 161, 92, 36, 110, 80, 33, 128, 47, 231, 83, 15,
                     145, 34, 4, 237, 166, 72, 73, 103, 236, 247, 192, 57, 206, 242, 45, 190,
                     93, 28, 227, 135, 7, 13, 122, 244, 251, 50, 245, 140, 219, 143, 37, 150,
                     168, 234, 205, 51, 101, 84, 6, 141, 137, 10, 94, 217, 22, 14, 113, 108,
                     11, 255, 96, 210, 46, 211, 200, 85, 194, 35, 183, 116, 226, 155, 223, 119,
                     43, 185, 60, 98, 19, 229, 148, 52, 177, 39, 132, 159, 215, 81, 0, 97,
                     173, 133, 115, 3, 8, 64, 239, 104, 254, 151, 31, 222, 175, 102, 232, 184,
                     174 ,189, 179, 235, 198, 107, 71, 169, 216, 167, 114, 238, 29, 126, 170, 182,
                     117, 203, 212, 48, 105, 32, 127, 55, 91, 157, 120, 163, 241, 118, 250, 5,
                     61, 58, 68, 87, 59, 202, 199, 138, 24, 70, 156, 191, 186, 56, 86, 26,
                     146, 77, 38, 41, 162, 152, 16, 153, 112, 160, 197, 40, 193, 109, 20, 172,
                     249, 95, 79, 196, 195, 209, 252, 221, 178, 89, 230, 181, 54, 82, 74, 42
        };

        public E2() { }

        public void GenerateKey()
        {
            Random rnd = new Random();
            rnd.NextBytes(Key);
        }

        public byte[] EncryptBlock(byte[] block)
        {
            var roundKeys = GenerateRoundKeys(Key);

            return ITFaistelFT(block, roundKeys);
        }

        public byte[] DecryptBlock(byte[] block)
        {
            var roundKeys = GenerateRoundKeys(Key);
            var roundKeysDecr = new byte[16][];

            for (int i = 0; i < 16; i++)
                roundKeysDecr[i] = new byte[16];

            for (int i = 0; i < 12; ++i)
                for (int j = 0; j < key_bytes; ++j)
                    roundKeysDecr[i][j] = roundKeys[11 - i][j];

            for (int i = 12; i < 16; ++i)
                for (int j = 0; j < key_bytes; ++j)
                    roundKeysDecr[i][j] = roundKeys[15 - (i - 12)][j];

            for (int i = 0; i < num_of_keys; ++i)
                for (int j = 0; j < key_bytes; ++j)
                    roundKeys[i][j] = roundKeysDecr[i][j];

            return ITFaistelFT(block, roundKeysDecr);
        }

        private byte[] ITFaistelFT(byte[] block, byte[][] roundKeys)
        {
            byte[] M = IT(block, roundKeys[12], roundKeys[13]);

            //12 раундов шифрования по схеме Фейстеля
            ulong L = BytesToULong(M.Take(8).ToArray());
            ulong R = BytesToULong(M.Skip(8).Take(8).ToArray());

            var currentKey = new ulong[2];
            for (int i = 0; i < 11; ++i)
            {
                currentKey[0] = BytesToULong(roundKeys[i].Take(8).ToArray());
                currentKey[1] = BytesToULong(roundKeys[i].Skip(8).Take(8).ToArray());
                L ^= F(R, currentKey);
                (L, R) = (R, L);
            }

            currentKey[0] = BytesToULong(roundKeys[11].Take(8).ToArray());
            currentKey[1] = BytesToULong(roundKeys[11].Skip(8).Take(8).ToArray());
            L ^= F(R, currentKey);

            //Заключительное преобразование
            M = ULongToBytes(L).Concat(ULongToBytes(R)).ToArray();

            return FT(M, roundKeys[14], roundKeys[15]);
        }

        //функция возвращает 64 битное число типа ulong из массива из 8 элементов типа byte
        private ulong BytesToULong(byte[] bytes)
        {
            ulong temp1 = 0;
            for (int i = 0; i < 8; ++i)
                temp1 = (temp1 << 8) | bytes[i];

            return temp1;
        }

        //функция записывает в d массив из 4 элементов типа byte на основе 32битного числа типа ulong
        private byte[] UIntToBytes(uint number)
        {
            return BitConverter.GetBytes(number).Reverse().ToArray();
        }

        //функция возвращает 32 битное число из массива из 4 элементов типа byte
        private uint BytesToUInt(byte[] bytes)
        {
            uint i32 = (uint)(bytes[3] | (bytes[2] << 8) | (bytes[1] << 16) | (bytes[0] << 24));

            return i32;
        }

        //функция записывает в res массив из 8 элементов типа byte на основе 64битного числа типа ulong
        private byte[] ULongToBytes(ulong number)
        {
            return BitConverter.GetBytes(number).Reverse().ToArray();
        }

        //расширенный алгоритм Евклида
        private ulong ExGCD(ulong a, ulong b, out ulong x, out ulong y)
        {
            if (b == 0)
            {
                x = 1;
                y = 0;
                return a;
            }
            ulong x1, y1;
            ulong d1 = ExGCD(b, a % b, out x1, out y1);
            x = y1;
            y = x1 - (a / b) * y1;

            return d1;
        }

        // функция для нахождения мультипликативного обратного к  a по модулю M на основе расширенного алгоритма Евклида
        private ulong ReverseElement(ulong a, ulong m)
        {
            ulong x, y, d;
            d = ExGCD(a, m, out x, out y);

            if (d != 1)
                throw new Exception();
            else
                return x;
        }

        private byte[] BinarX(byte[] x64, byte[] y64)
        {
            int num_of_bytes = 4;
            uint[] x = new uint[num_of_bytes];
            uint[] y = new uint[num_of_bytes];
            uint[] u = new uint[num_of_bytes];

            for (int i = 0; i < num_of_bytes; i++)
            {
                x[i] = BytesToUInt(x64.Skip(4 * i).Take(4).ToArray());
                y[i] = BytesToUInt(y64.Skip(4 * i).Take(4).ToArray());
            }

            for (int i = 0; i < num_of_bytes; i++)
            {
                uint z = y[i];
                z |= 1;
                u[i] = x[i] * z;
            }

            var u64 = new byte[] { };
            for (int i = 0; i < num_of_bytes; ++i)
                u64 = u64.Concat(UIntToBytes(u[i])).ToArray();

            return u64;
        }

        private byte[] BinarDeX(byte[] x64, byte[] y64)
        {
            int num_of_bytes = 4;
            var x = new uint[num_of_bytes];
            var y = new uint[num_of_bytes];
            var w = new uint[num_of_bytes];

            for (int j = 0; j < num_of_bytes; ++j)
            {
                x[j] = BytesToUInt(x64.Skip(4 * j).Take(4).ToArray());
                y[j] = BytesToUInt(y64.Skip(4 * j).Take(4).ToArray());
            }

            for (int i = 0; i < num_of_bytes; ++i)
            {
                uint z = y[i];
                z |= 1;

                ulong pow_2_to_32 = 1UL << 32;
                ulong x_1 = ReverseElement(z, pow_2_to_32);

                w[i] = (uint)(x[i] * x_1);
            }

            var w64 = new byte[] { };
            for (int i = 0; i < num_of_bytes; ++i)
                w64 = w64.Concat(UIntToBytes(w[i])).ToArray();

            return w64;
        }

        private byte[] IT(byte[] x, byte[] a, byte[] b)
        {
            byte[] tempM = new byte[BlockSize];
            for (int i = 0; i < key_bytes; i++)
                tempM[i] = (byte)(x[i] ^ a[i]);
            tempM = BinarX(tempM, b);

            return BP(tempM);
        }

        private byte[] FT(byte[] x, byte[] a, byte[] b)
        {
            byte[] tempM = BPInv(x);

            tempM = BinarDeX(tempM, a);

            for (int i = 0; i < BlockSize; i++)
                tempM[i] ^= b[i];

            return tempM;
        }

        //BP(X)
        private byte[] BP(byte[] x)
        {
            var result = new byte[16];
            for (int i = 0; i < 16; i++)
                result[i] = x[(5 * i) % 16];

            return result;
        }
        //BP^-1(X)
        private byte[] BPInv(byte[] x)
        {
            var result = new byte[16];
            for (int i = 0; i < 16; i++)
                result[i] = x[(13 * i) % 16];

            return result;
        }
        //S(X) производится подстановка из таблицы замен
        private ulong S(ulong X)
        {
            ulong res = 0;
            ulong tempX = X;
            for (int i = 0; i < 8; i++)
            {
                var first = (byte)BitOperations.RotateRight(tempX, 56 - 8 * i);
                res ^= SBox[first];
                res = BitOperations.RotateLeft(res, 8 * (7 - i));
            }

            return res;
        }

        //P(X)
        private ulong P(ulong X)
        {
            ulong res = 0;
            byte[] y = new byte[8];
            byte[] x = ULongToBytes(X);
            y[7] = (byte)(x[7] ^ x[3]); y[6] = (byte)(x[6] ^ x[2]); y[5] = (byte)(x[5] ^ x[1]); y[4] = (byte)(x[4] ^ x[0]);
            y[3] = (byte)(x[3] ^ x[5]); y[2] = (byte)(x[2] ^ x[4]); y[1] = (byte)(x[1] ^ x[7]); y[0] = (byte)(x[0] ^ x[6]);
            y[7] = (byte)(y[7] ^ y[2]); y[6] = (byte)(y[6] ^ y[1]); y[5] = (byte)(y[5] ^ y[0]); y[4] = (byte)(y[4] ^ y[3]);
            y[3] = (byte)(y[3] ^ y[7]); y[7] = (byte)(y[2] ^ y[6]); y[1] = (byte)(y[1] ^ y[5]); y[0] = (byte)(y[0] ^ y[4]);
            res = BytesToULong(y);

            return res;
        }
        //циклический сдвиг влево на 8 бит
        private ulong BRL(ulong a)
        {
            //обычный сдвиг влево на 8 бит
            ulong b = BitOperations.RotateLeft(a, 8);
            //в с хранятся первые 8 бит, а сдвинуты в последние 8 разрядов
            ulong c = BitOperations.RotateRight(0xFF00000000000000 & a, 56);

            return b ^ c;
        }
        //раундовая функция
        private ulong F(ulong R, ulong[] roundk)
        {
            return BRL(S(P(S(R ^ roundk[0])) ^ roundk[1]));
        }

        private void G(ulong[] X, ulong U, ref ulong[] L, ref ulong[] Y, out ulong V)
        {
            L = new ulong[4];
            Y = new ulong[4];

            for (int i = 0; i < 4; ++i)
                Y[i] = P(S(X[i]));

            L[0] = Y[0] ^ P(S(U));
            for (int j = 1; j < 4; ++j)
            {
                L[j] = P(S(L[j - 1])) ^ Y[j];

            }
            V = L[3];
        }

        //генерирует раундовые ключи
        private byte[][] GenerateRoundKeys(byte[] key)
        {
            var roundKeys = new byte[num_of_keys][];
            for (int i = 0; i < num_of_keys; i++)
                roundKeys[i] = new byte[key_bytes];

            ulong g = 0x0123456789abcdef;
            ulong[] K = new ulong[4];

            K[0] = BytesToULong(key.Take(8).ToArray());
            K[1] = BytesToULong(key.Skip(4).Take(8).ToArray());

            if (key.Length == 16)
            {
                K[2] = S(S(S(g)));
                K[3] = S(K[2]);
            }
            else if (key.Length == 24)
            {
                K[2] = BytesToULong(key.Skip(8).Take(8).ToArray());
                K[3] = S(S(S(S(g))));
            }
            else if (key.Length == 32)
            {
                K[2] = BytesToULong(key.Skip(8).Take(8).ToArray());
                K[3] = BytesToULong(key.Skip(12).Take(8).ToArray());
            }

            ulong[] L = new ulong[4] { 0, 0, 0, 0 };
            ulong[] Y = new ulong[4] { 0, 0, 0, 0 };
            ulong U = g;
            ulong new_U;

            G(K, U, ref L, ref Y, out new_U);

            U = new_U;

            byte[][] q = new byte[32][];
            for (int i = 0; i < 32; i++)
                q[i] = new byte[8];

            for (int i = 0; i < 8; i++)
            {
                ulong[] Y_new = new ulong[4] { 0, 0, 0, 0 };

                G(Y, U, ref L, ref Y_new, out new_U);

                for (int j = 0; j < 4; j++)
                    Y[j] = Y_new[j];

                U = new_U;

                for (int j = 0; j < 4; j++)
                    q[4 * i + j] = ULongToBytes(L[j]);
            }

            int p = 0;
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 16; j++)
                {
                    roundKeys[2 * i + 1][j] = q[2 * j][p];
                    roundKeys[2 * i][j] = q[2 * j + 1][p];
                }
                p++;
            }

            return roundKeys;
        }
    }
}
