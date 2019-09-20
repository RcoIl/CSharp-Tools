using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using WeblogicRCE.WeblogicPOC;

namespace WeblogicRCE
{
    class Program
    {
        private static void Usage()
        {
            string usage = "";
            usage += "[+] Usage:\r\n";
            usage += "[+] WeblogicRCE.exe -check ip port\r\n";
            Console.WriteLine(usage);
        }

        private static void Check(string ip, int port)
        {
            Console.WriteLine();
            Console.WriteLine("[+] Start Check CVE-2016-0638");
            CVE_2016_0638_POC.Check(ip, port);
            Console.WriteLine();
            Console.WriteLine("[+] Start Check CVE-2016-3510");
            CVE_2016_3510_POC.Check(ip, port);
            Console.WriteLine();
            Console.WriteLine("[+] Start Check CVE-2017-3248");
            CVE_2017_3248_POC.Check(ip, port);
            Console.WriteLine();
            Console.WriteLine("[+] Start Check CVE-2017-10271");
            CVE_2017_10271_POC.Check(ip, port);
            Console.WriteLine();
            Console.WriteLine("[+] Start Check CVE-2018-2628");
            CVE_2018_2628_POC.Check(ip, port);
            Console.WriteLine();
            Console.WriteLine("[+] Start Check CVE-2018-2893");
            CVE_2018_2893_POC.Check(ip, port);
            Console.WriteLine();
            Console.WriteLine("[+] Start Check CVE-2019-2725");
            CVE_2019_2725_POC.Check(ip, port);
        }

        static void Main(string[] args)
        {
            String Banner = Properties.Resources.banner;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(Banner);

            if (args.Length != 3)
            {
                Usage();
                Environment.Exit(0);
            }
            else if (args.Length == 3)
            {
                if (args[0] == "-check")
                {
                    Console.WriteLine("\n[+] Welcome To WeblogicRCE Check !!!\n");
                    string ip = args[1];
                    int port = Convert.ToInt32(args[2]);
                    Check(ip, port);
                }
                else
                {
                    Environment.Exit(0);
                }
            }

        }
    }
}
