using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NavicatCrypto
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.White;
            List<string> Supersedence = new List<string>();
            Supersedence.AddRange(new string[]
            {
                @"MySql:Software\PremiumSoft\Navicat\Servers",
                @"SQL Server:Software\PremiumSoft\NavicatMSSQL\Servers",
                @"Oracle:Software\PremiumSoft\NavicatOra\Servers",
                @"pgsql:Software\PremiumSoft\NavicatPG\Servers",
                @"MariaDB:Software\PremiumSoft\NavicatMARIADB\Servers"
            });
            foreach (string Supersedences in Supersedence)
            {
                string[] sArray = Regex.Split(Supersedences, ":", RegexOptions.IgnoreCase);
                string Database_version = sArray[0].ToString();
                string Database_Reg = sArray[1].ToString();
                Console.WriteLine("[*] ConnectName: {0}", Database_version);
                DecryptStr(Database_Reg);
                Console.WriteLine();
            }
        }
        
        static void DecryptStr(string basekey)
        {
            Navicat11Cipher Decrypt = new Navicat11Cipher();
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(basekey);
            if (registryKey != null)
            {
                foreach (string rname in registryKey.GetSubKeyNames())
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("  [+] ConnectName: {0}", rname);
                    RegistryKey installedapp = registryKey.OpenSubKey(rname);
                    if (installedapp != null)
                    {
                        string Host = (installedapp.GetValue("Host") != null) ? installedapp.GetValue("Host").ToString() : "";
                        string UserName = (installedapp.GetValue("UserName") != null) ? installedapp.GetValue("UserName").ToString() : "";
                        string Pwd = (installedapp.GetValue("Pwd") != null) ? installedapp.GetValue("Pwd").ToString() : "";
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("    [>] Host: {0}", Host);
                        Console.WriteLine("    [>] UserName: {0}", UserName);
                        Console.WriteLine("    [>] Password: {0}", Decrypt.DecryptString(Pwd));
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }
            }
        }
    }
}
