using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CourseProject.Model
{
    class Scrambler
    {
        public enum EncryptionMode { ECB, CBC, CFB, OFB };
        public EncryptionMode encryptionMode;
        private int blockSize = 10240;
        private byte[] _iv;
        public E2 e2;
        byte[] prevBlock;
        byte[] curBlock;


        public byte[] IV
        {
            set
            {
                _iv = new byte[value.Length];
                prevBlock = new byte[value.Length];
                Array.Copy(value, _iv, value.Length);
                
            }
            get => _iv;
        }

        public void GenerateIV(int size)
        {
            Random rnd = new Random();
            
            byte[] newIV = new byte[size];
            rnd.NextBytes(newIV);
            IV = newIV;
        }

        public void EncryptFile(string source, string dest, Action<double> action)
        {
            FileInfo file = new FileInfo(source);
            if (encryptionMode != EncryptionMode.ECB) Array.Copy(_iv, prevBlock, prevBlock.Length);
            using (FileStream sourcefs = File.Open(source, FileMode.Open))
            {
                using (FileStream destfs = File.Open(dest, FileMode.Create))
                {
                    long size = file.Length / blockSize + (file.Length % blockSize == 0 ? 0 : 1);
                    byte[] buf = new byte[blockSize];
                    for (long i = 0; i < size - 1; i++)
                    {
                        action(Convert.ToDouble(i) / size * 100);
                        sourcefs.Read(buf, 0, blockSize);
                        destfs.Write(Encrypt(buf));
                    }
                    int lastBlocksize = sourcefs.Read(buf, 0, blockSize);
                    Array.Resize(ref buf, lastBlocksize);
                    buf = MakePadding(buf);
                    destfs.Write(Encrypt(buf));
                    action(100);
                }
            }         
        }

        public void DecryptFile(string source, string dest, Action<double> action)
        {
            FileInfo file = new FileInfo(source);
            if (encryptionMode != EncryptionMode.ECB) Array.Copy(_iv, prevBlock, prevBlock.Length);
            using (FileStream sourcefs = File.Open(source, FileMode.Open))
            {
                using (FileStream destfs = File.Open(dest, FileMode.Create))
                {
                    long size = file.Length / blockSize + (file.Length % blockSize == 0 ? 0 : 1);
                    byte[] buf = new byte[blockSize];
                    for (long i = 0; i < size - 1; i++)
                    {
                        sourcefs.Read(buf, 0, blockSize);
                        destfs.Write(Decrypt(buf));
                        action(Convert.ToDouble(i) / size * 100);
                    }
                    int lastBlocksize = sourcefs.Read(buf, 0, blockSize);
                    Array.Resize(ref buf, lastBlocksize);
                    buf = Decrypt(buf);
                    Array.Resize(ref buf, buf.Length - buf[buf.Length - 1]);
                    destfs.Write(buf);
                    action(100);
                }

            }
        }
        private byte[] MakePadding(byte[] data)
        {
            long addingBlocks = 2 * e2.BlockSize - data.Length % e2.BlockSize;
            byte[] addedData = new byte[data.Length + addingBlocks];
            data.CopyTo(addedData, 0);
            addedData[addedData.Length - 1] = (byte)addingBlocks;
            return addedData;
        }
        public byte[] Encrypt(byte[] data)
        {
            byte[] res = new byte[data.Length];
            data.CopyTo(res, 0);
            curBlock = new byte[e2.BlockSize];
            switch (encryptionMode)
            {
                case EncryptionMode.ECB:
                    {
                        List<byte[]> blocks = MakeListFromArray(data);
                        List<byte[]> list = blocks.AsParallel().AsOrdered().Select(part =>
                        {
                            return e2.EncryptBlock(part);
                        }).ToList();
                        Array.Copy(MakeArrayFromList(list), res, res.Length);
                        break;
                    }
                case EncryptionMode.CBC:
                    {

                        
                        for (int i = 0; i < res.Length / e2.BlockSize; i++)
                        {
                            Array.Copy(res, i * e2.BlockSize, curBlock, 0, e2.BlockSize);
                            Array.Copy(e2.EncryptBlock(XOR(curBlock, prevBlock)), curBlock, e2.BlockSize);
                            Array.Copy(curBlock, 0, res, i * e2.BlockSize, e2.BlockSize);
                            Array.Copy(curBlock, prevBlock, e2.BlockSize);
                        }
                        break;
                    }
                case EncryptionMode.OFB:
                    {
                                            
                        for (int i = 0; i < res.Length / e2.BlockSize; i++)
                        {
                            Array.Copy(e2.EncryptBlock(prevBlock), prevBlock, prevBlock.Length);
                            Array.Copy(res, i * e2.BlockSize, curBlock, 0, e2.BlockSize);
                            Array.Copy(XOR(prevBlock, curBlock), curBlock, curBlock.Length);
                            Array.Copy(curBlock, 0, res, i * e2.BlockSize, e2.BlockSize);
                        }
                        break;
                    }
                case EncryptionMode.CFB:
                    {         
                        for (int i = 0; i < res.Length / e2.BlockSize; i++)
                        {
                            Array.Copy(e2.EncryptBlock(prevBlock), prevBlock, prevBlock.Length);
                            Array.Copy(res, i * e2.BlockSize, curBlock, 0, e2.BlockSize);
                            Array.Copy(XOR(prevBlock, curBlock), prevBlock, prevBlock.Length);
                            Array.Copy(prevBlock, 0, res, i * e2.BlockSize, e2.BlockSize);
                        }
                        break;
                    }
            }
            return res;
        }

        public byte[] Decrypt(byte[] data)
        {
            byte[] res = new byte[data.Length];
            switch (encryptionMode)
            {
                case EncryptionMode.ECB:
                    {
                        List<byte[]> blocks = MakeListFromArray(data);                       
                        List<byte[]> list = blocks.AsParallel().AsOrdered().Select(part =>
                        {
                            return e2.DecryptBlock(part);
                        }).ToList();
                        Array.Copy(MakeArrayFromList(list), res, res.Length);
                        break;
                    }
                case EncryptionMode.CBC:
                    {
                        data.CopyTo(res, 0);
                        curBlock = new byte[e2.BlockSize];
                        
                        for (int i = 0; i < data.Length / e2.BlockSize; i++)
                        {
                            Array.Copy(res, i * e2.BlockSize, curBlock, 0, e2.BlockSize);
                            byte[] buf = XOR(prevBlock, e2.DecryptBlock(curBlock));
                            Array.Copy(buf, 0, res, i * e2.BlockSize, e2.BlockSize);
                            Array.Copy(curBlock, prevBlock, e2.BlockSize);
                        }

                        break;
                    }

                case EncryptionMode.OFB:
                    {
                        data.CopyTo(res, 0);
                        curBlock = new byte[e2.BlockSize];

                        for (int i = 0; i < res.Length / e2.BlockSize; i++)
                        {
                            Array.Copy(e2.EncryptBlock(prevBlock), prevBlock, prevBlock.Length);
                            Array.Copy(res, i * e2.BlockSize, curBlock, 0, e2.BlockSize);
                            Array.Copy(XOR(prevBlock, curBlock), curBlock, curBlock.Length);
                            Array.Copy(curBlock, 0, res, i * e2.BlockSize, e2.BlockSize);
                        }

                        break;
                    }
                case EncryptionMode.CFB:
                    {
                        data.CopyTo(res, 0);
                        curBlock = new byte[e2.BlockSize];
                        Array.Copy(e2.EncryptBlock(prevBlock), prevBlock, prevBlock.Length);
                        for (int i = 0; i < res.Length / e2.BlockSize; i++)
                        {
                            Array.Copy(res, i * e2.BlockSize, curBlock, 0, e2.BlockSize);
                            Array.Copy(XOR(prevBlock, curBlock), 0, res, i * e2.BlockSize, e2.BlockSize);
                            Array.Copy(e2.EncryptBlock(curBlock), prevBlock, prevBlock.Length);
                        }

                        break;
                    }
            }
            return res;
        }

        private List<byte[]> MakeListFromArray(byte[] data)
        {
            List<byte[]> res = new List<byte[]>();
            for (int i = 0; i < data.Length / e2.BlockSize; i++)
            {
                res.Add(new byte[e2.BlockSize]);
                Array.Copy(data, i * e2.BlockSize, res[i], 0, e2.BlockSize);
            }
            return res;
        }
        private byte[] XOR(byte[] left, byte[] right)
        {
            byte[] res = new byte[left.Length];
            for (int i = 0; i < left.Length; i++)
            {
                res[i] = (byte)(left[i] ^ right[i]);
            }
            return res;
        }
        private byte[] MakeArrayFromList(List<byte[]> data)
        {
            byte[] res = new byte[e2.BlockSize * data.Count];
            for (int i = 0; i < data.Count; i++)
            {
                Array.Copy(data[i], 0, res, i * e2.BlockSize, e2.BlockSize);
            }
            return res;
        }
    }
}
