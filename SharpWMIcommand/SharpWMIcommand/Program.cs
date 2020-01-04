using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace SharpWMIcommand
{
    class Program
    {

        #region
        [DllImport("advapi32.dll", SetLastError = true, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool LogonUser(
          [MarshalAs(UnmanagedType.LPStr)] string lpszUsername,
          [MarshalAs(UnmanagedType.LPStr)] string lpszDomain,
          [MarshalAs(UnmanagedType.LPStr)] string lpszPassword,
          int dwLogonType,
          int dwLogonProvider,
          ref IntPtr phToken);


        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool ImpersonateLoggedOnUser(IntPtr hToken);

        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();

        const int LOGON32_LOGON_NEW_CREDENTIALS = 9;
        const int LOGON32_PROVIDER_DEFAULT = 0;
        #endregion

        static void Main(string[] args)
        {

            string host = args[0];
            string domain = ".";
            string username = args[1];
            string password = args[2];
            string command = args[3];

            IntPtr phToken = IntPtr.Zero;

            bool bResult = false;
            if (username != null)
            {
                Console.WriteLine("[*] Username was provided attempting to call LogonUser");
                bResult = LogonUser(username, domain, password, LOGON32_LOGON_NEW_CREDENTIALS, LOGON32_PROVIDER_DEFAULT, ref phToken);
                if (!bResult)
                {
                    Console.WriteLine("[!] LogonUser failed. Error:{0}", GetLastError());
                    Environment.Exit(0);
                }
            }

            bResult = ImpersonateLoggedOnUser(phToken);
            if (!bResult)
            {
                Console.WriteLine("[!] ImpersonateLoggedOnUser failed. Error:{0}", GetLastError());
                Environment.Exit(0);
            }

            if (PortScan(host))
            {
                if (RemoteWMIExecute(host, command, username, password))
                {
                    Thread.Sleep(1000);
                    ReadAndDeleteResults(host);
                }
            }
        }

        private static bool RemoteWMIExecute(string host, string command, string username, string password)
        {
            string wmiNameSpace = "root\\cimv2";
            ConnectionOptions options = new ConnectionOptions();

            Console.WriteLine("    [>] Host: {0}", host);
            Console.WriteLine("    [>] Command: {0}", command);

            if (!String.IsNullOrEmpty(username))
            {
                options.Username = username;
                options.Password = password;
            }
            Console.WriteLine();

            options.Timeout = new TimeSpan(0, 0, 3);
            ManagementScope scope = new ManagementScope(String.Format("\\\\{0}\\{1}", host, wmiNameSpace), options);

            try
            {
                scope.Connect();

                var wmiProcess = new ManagementClass(scope, new ManagementPath("Win32_Process"), new ObjectGetOptions());

                ManagementBaseObject inParams = wmiProcess.GetMethodParameters("Create");
                PropertyDataCollection properties = inParams.Properties;

                inParams["CommandLine"] = String.Format(@"cmd.exe /c {0} > C:\Windows\Temp\winstore.ini", command);

                Console.WriteLine("[*] Command executed successfully, waiting to read results...");
                Console.WriteLine("=============================================================");

                ManagementBaseObject outParams = wmiProcess.InvokeMethod("Create", inParams, null);

                if (outParams["returnValue"].ToString() == "0")
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("  [!] Could not connect to {0}", host);
                Console.WriteLine("  Exception : {0}", ex.Message);
            }
            return false;
        }

        private static void ReadAndDeleteResults(string host)
        {
            FileStream stream = File.OpenRead(String.Format(@"\\{0}\C$\Windows\Temp\winstore.ini", host));
            string contents = "";
            using (var sr = new StreamReader(stream, Encoding.Default))
            {
                contents = sr.ReadToEnd();
                foreach (string line in contents.Split('\n'))
                {
                    Console.WriteLine(line);
                }
            }
            File.Delete(String.Format(@"\\{0}\C$\Windows\Temp\winstore.ini", host));
        }

        private static bool PortScan(string host)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var result = client.BeginConnect(host, 445, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(new TimeSpan(0, 0, 3));
                    client.EndConnect(result);
                }
            }
            catch
            {
                Console.WriteLine("    [>] Port 445 closed on " + host);
                return false;
            }
            try
            {
                using (var client = new TcpClient())
                {
                    var result = client.BeginConnect(host, 135, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(new TimeSpan(0, 0, 3));
                    client.EndConnect(result);
                }
            }
            catch
            {
                Console.WriteLine("    [>] Port 135 closed on " + host);
                return false;
            }
            return true;
        }
    }
}
