using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CourseProject.Model
{
    class BigIntAlgos
    {
        public static BigInteger GetPrime(int size)
        {
            Random rnd = new Random();
            byte[] arr = new byte[size];
            rnd.NextBytes(arr);
            arr[size - 1] &= 0b01111111;
            BigInteger res = new BigInteger(arr);
            while (!MillerRabin(res, 30))
            {
                res++;
                if (res.ToByteArray().Length > size)
                {
                    rnd.NextBytes(arr);
                    arr[size - 1] &= 0b01111111;
                    res = new BigInteger(arr);
                }
            }

            return res;
        }

        public static bool MillerRabin(BigInteger n, int k)
        {
            if (n == 2 || n == 3)
                return true;

            if (n < 2 || n % 2 == 0)
                return false;

            BigInteger t = n - 1;
            int s = 0;

            while (t % 2 == 0)
            {
                t /= 2;
                s += 1;
            }

            for (int i = 0; i < k; i++)
            {
                RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
                byte[] _a = new byte[n.ToByteArray().LongLength];
                BigInteger a;

                do
                {
                    rng.GetBytes(_a);
                    a = new BigInteger(_a);
                }
                while (a < 2 || a >= n - 2);

                BigInteger x = BigInteger.ModPow(a, t, n);

                if (x == 1 || x == n - 1)
                    continue;

                for (int r = 1; r < s; r++)
                {
                    x = BigInteger.ModPow(x, 2, n);

                    if (x == 1)
                        return false;

                    if (x == n - 1)
                        break;
                }

                if (x != n - 1)
                    return false;
            }

            return true;
        }

        public static BigInteger ModPow(BigInteger a, BigInteger n, BigInteger mod)
        {
            if (a < 0)
            {
                a = a + (mod - 1);
            }

            BigInteger res = 1;
            a = a % mod;

            while (n > 0)
            {
                if ((n & 1) == 1)
                {
                    res *= a;
                    res %= mod;
                }
                a *= a;
                a %= mod;
                n >>= 1;
            }

            return res;
        }

        public static BigInteger GCD(BigInteger a, BigInteger b)
        {
            BigInteger c;

            while (b != 0)
            {
                a %= b;
                c = a;
                a = b;
                b = c;
            }

            return a;
        }


        public static BigInteger LCM(BigInteger a, BigInteger b)
        {
            return (a * b) / GCD(a, b);
        }

        public static int Legendre(BigInteger a, BigInteger p)
        {
            if (a == 0)
            {
                return 0;
            }
            if (a == 1)
            {
                return 1;
            }
            int result;
            if (a % 2 == 0)
            {
                result = Legendre(a / 2, p);
                if (((p * p - 1) & 8) != 0)
                {
                    result = -result;
                }
            }
            else
            {
                result = Legendre(p % a, a);
                if (((a - 1) * (p - 1) & 4) != 0)
                {
                    result = -result;
                }
            }
            return result;
        }


        public static BigInteger ExtendedGCD(BigInteger a, BigInteger b, ref BigInteger x, ref BigInteger y)
        {
            if (a == 0)
            {
                x = 0;
                y = 1;
                return b;
            }

            BigInteger x1 = 0, y1 = 0;
            BigInteger d = ExtendedGCD(b % a, a, ref x1, ref y1);
            x = y1 - (b / a) * x1;
            y = x1;

            return d;
        }

        public static BigInteger ModIversion(BigInteger value, BigInteger modulo)
        {
            BigInteger left = 0, right = 0;
            var egcd = ExtendedGCD(value, modulo, ref left, ref right);

            if (left < 0)
                left += modulo;

            return left % modulo;
        }
    }
}
