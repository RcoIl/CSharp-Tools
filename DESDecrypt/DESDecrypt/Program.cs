using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DESDecrypt
{
    class Program
    {
        // 将命令运行结果写入文本
        public static void TxtWriter(string outlist, string outfile)
        {
            string Path = Environment.CurrentDirectory + @"\" + outfile;
            StreamWriter sw = new StreamWriter(Path, true, Encoding.UTF8);
            sw.WriteLine(outlist);
            sw.Flush();
            sw.Close();
        }

        // 修改自 https://www.cnblogs.com/liqipeng/archive/2013/03/23/4576174.html
        // 关键点：DES标准密钥就是56bit，8个字符即8个字节，每个字节的最高位不用，即每个字节只用7位，8个字符正好是56bit。如果少于8个字符，就用0填充，最后参与运算的一定是56bit。
        static void Main(string[] args)
        {
            // Uses: DESDecrypt.exe Key Vector Encrypt.txt Decrypt.txt
            string Key = args[0].PadRight(8, '0');
            string Vector = args[1];
            string EncryptFile = args[2];
            string DecryptFile = args[3];
            FileStream fileStream;
            FileStream stream = fileStream = new FileStream(EncryptFile, FileMode.Open, FileAccess.Read);
            try
            {
                StreamReader streamReader2;
                StreamReader streamReader = streamReader2 = new StreamReader(stream, Encoding.Default);
                try
                {
                    while (!streamReader.EndOfStream)
                    {
                        string EncryptTXT;
                        if ((EncryptTXT = streamReader.ReadLine()) != null && EncryptTXT.Length != 0)
                        {
                            Console.WriteLine(DESDecrypt(EncryptTXT, Key, Vector));
                            TxtWriter(DESDecrypt(EncryptTXT, Key, Vector), DecryptFile);
                        }
                    }
                }
                finally
                {
                    if (streamReader2 != null)
                    {
                        ((IDisposable)streamReader2).Dispose();
                    }
                }
            }
            finally
            {
                if (fileStream != null)
                {
                    ((IDisposable)fileStream).Dispose();
                }
            }
            Console.WriteLine("Finish!");
            GC.Collect();
        }

        /// <summary>
        /// DES解密
        /// </summary>
        /// <param name="Data">被解密的密文</param>
        /// <param name="Key">密钥</param>
        /// <param name="Vector">向量</param>
        /// <returns>明文</returns>
        public static string DESDecrypt(String EncryptTXT, String Key, String Vector)
        {
            byte[] EncryptData = Convert.FromBase64String(EncryptTXT);
            Byte[] bKey = new Byte[8];
            Array.Copy(Encoding.UTF8.GetBytes(Key.PadRight(bKey.Length)), bKey, bKey.Length);
            Byte[] bVector = new Byte[8];
            Array.Copy(Encoding.UTF8.GetBytes(Vector.PadRight(bVector.Length)), bVector, bVector.Length);

            string original = null;
            //MemoryStream original = new MemoryStream();

            DESCryptoServiceProvider CryptoProvider = new DESCryptoServiceProvider();
            CryptoProvider.Mode = CipherMode.CBC;
            CryptoProvider.Padding = PaddingMode.Zeros;

            try
            {
                // 开辟一块内存流，存储密文
                using (MemoryStream Memory = new MemoryStream(EncryptData))
                {
                    // 把内存流对象包装成加密流对象
                    using (CryptoStream Decryptor = new CryptoStream(Memory,
                    CryptoProvider.CreateDecryptor(bKey, bVector),
                    CryptoStreamMode.Read))
                    {
                        // 明文存储区
                        using (MemoryStream originalMemory = new MemoryStream())
                        {
                            Byte[] Buffer = new Byte[1024];
                            Int32 readBytes = 0;
                            while ((readBytes = Decryptor.Read(Buffer, 0, Buffer.Length)) > 0)
                            {
                                originalMemory.Write(Buffer, 0, readBytes);
                            }

                            original = Encoding.UTF8.GetString(originalMemory.ToArray());
                        }
                    }
                }
            }
            catch
            {
                original = null;
            }

            return original;
        }
    }
}
