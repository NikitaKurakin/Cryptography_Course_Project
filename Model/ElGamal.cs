using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CourseProject.Model
{
    class ElGamal
    {
        public struct publicKey
        {
            public BigInteger p;
            public BigInteger g;
            public BigInteger y;

            public publicKey(BigInteger p, BigInteger g, BigInteger y)
            {
                this.p = p;
                this.g = g;
                this.y = y;
            }

            public void Parse(string[] s)
            {
                this.p = BigInteger.Parse(s[0]);
                this.g = BigInteger.Parse(s[1]);
                this.y = BigInteger.Parse(s[2]);
            }

            public string ToString()
            {
                return new string(p.ToString() + "\n" + g.ToString() + "\n" + y.ToString() + "\n");
            }


        }

        public struct EncryptedMessage
        {
            public BigInteger a;
            public BigInteger b;

            public EncryptedMessage(publicKey PublicKey, BigInteger k, BigInteger message)
            {
                this.a = BigInteger.ModPow(PublicKey.g, k, PublicKey.p);
                this.b = BigInteger.ModPow(PublicKey.y, k, PublicKey.p) * message % PublicKey.p;
            }

            public byte[] DecryptMessage(publicKey PublicKey, BigInteger PrivateKey)
            { 
                return (this.b * BigInteger.ModPow(this.a, PublicKey.p - 1 - PrivateKey, PublicKey.p) % PublicKey.p).ToByteArray();
            }

            public void Parse(string[] s)
            {
                this.a = BigInteger.Parse(s[0]);
                this.b = BigInteger.Parse(s[1]);
            }

            public string ToString()
            {
                return new string(a.ToString() + "\n" + b.ToString());
            }
        }

        public publicKey PublicKey = new publicKey();
        BigInteger PrivateKey = 0;

        public void GenerateKeys(int byteSize)
        {
            Random rnd = new Random();
            PublicKey.p = BigIntAlgos.GetPrime(byteSize);
            byte[] buffer = new byte[PublicKey.p.GetByteCount(true) - 1];
            rnd.NextBytes(buffer);
            PublicKey.g = new BigInteger(buffer);
            PrivateKey = BigIntAlgos.GetPrime(byteSize - 1);
            PublicKey.y = BigInteger.ModPow(PublicKey.g, PrivateKey, PublicKey.p);
        }

        public EncryptedMessage Encrypt(byte[] data)
        {
            Random rnd = new Random();
            BigInteger message = new BigInteger(data);
            BigInteger k = BigIntAlgos.GetPrime(data.Length - 1);
            EncryptedMessage res = new EncryptedMessage(PublicKey, k, message);
            return res;
        }

        public byte[] Decrypt(EncryptedMessage data) { return data.DecryptMessage(PublicKey, PrivateKey); }
    }
}
