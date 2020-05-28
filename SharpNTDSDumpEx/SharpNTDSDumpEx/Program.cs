using SharpNTDSDumpEx.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using static SharpNTDSDumpEx.NTDS;

namespace SharpNTDSDumpEx
{
    class Program
    {
        static void usage()
        {
            Console.WriteLine(@"
usage: SharpNTDSDumpEx.exe <-d ntds.dit> <-k HEX-SYS-KEY |-r
-d    path of ntds.dit database
-k    use specified SYSKEY
-r    read SYSKEY from registry

Example: ntdsdumpex.exe - r
Example : ntdsdumpex.exe -d ntds.dit -k BE1F38E309690E67B34AF8A040288663
NOTE : MUST BACKUP database file,and repair it frist(run [esentutl /p /o ntds.dit] command).
");
        }
        static void Main(string[] args)
        {
            string Banner = Properties.Resources.banner;
            Console.WriteLine(Banner);

            Stopwatch watch = new Stopwatch();
            watch.Start(); 

            var ditPath = "";
            var hasSYSKEY = false ;
            var systemkey = new byte[] { };
            var passwordkey = new Dictionary<uint, byte[]>();
            var entries = 0;


            if (args.Length < 2)
            {
                usage();
                Environment.Exit(0);
            }
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i].Contains("-d") && args.Length > i + 1)
                {
                    ditPath = args[++i];
                }
                else if (args[i].Contains("-k") && args.Length > i + 1)
                {
                    hasSYSKEY = SystemHive.ParseSystemKey(args[++i], out systemkey);
                }
                else if (args[i].Contains("-r"))
                {
                    hasSYSKEY = SystemHive.ReadSystemKey(out systemkey);
                }
                else
                {
                    usage();
                    Environment.Exit(0);
                }
            }

            if (!hasSYSKEY)
            {
                Console.WriteLine("[!] no SYSKEY set.");
                Environment.Exit(0);
            }
            Console.WriteLine("[*] SYSEKY = {0}", FormatBytes(systemkey));

            if (!File.Exists(ditPath))
            {
                Console.WriteLine("[!] no database set.");
                Environment.Exit(0);
            }
            var baseDateTime = new FileInfo(ditPath).LastWriteTimeUtc;
            Console.WriteLine($"[*] The base date used for statistics is {baseDateTime}");

            NTDS ntds = new NTDS();
            if (ntds.NTDSLoad(ditPath))
            {
                if (ntds.GetPEKey(systemkey, out passwordkey))
                {
                    Console.WriteLine("[*] PEK = {0}", FormatBytes(passwordkey[0]));

                    ////////////////////////////////////////////////////////////////////////////
                    DomainInfo domain = null;

                    DomainInfo[] Domains = ntds.CalculateDomainInfo();
                    UserInfo[] Users = ntds.CalculateUserInfo();

                    if (Domains.Length > 1)
                    {
                        var usersWithHashes = Users.Where(x => x.LmHash != EMPTY_LM_HASH || x.NtHash != EMPTY_NT_HASH).ToList();
                        domain = Domains.Single(x => x.Sid.Equals(usersWithHashes[0].DomainSid));
                    }
                    else
                    {
                        domain = Domains[0];
                    }

                    Console.WriteLine($"[*] Password stats for: {domain.Fqdn}");

                    String usersCsvPath = "usersCsv.csv";
                    using (var file = new StreamWriter(usersCsvPath, false))
                    {
                        file.WriteLine(@"Domain,Username,Rid,NT Hash,ClearText,Disabled,Expired,Password Never Expires,Password Not Required,Password Last Changed,Last Logon,DN");
                        foreach (var user in Users)
                        {
                            entries++;
                            domain = Domains.Single(x => x.Sid == user.DomainSid);
                            file.WriteLine($"{domain.Fqdn},{user.SamAccountName},{user.Rid},{user.NtHash},{user.ClearTextPassword},{user.Disabled},{!user.Disabled && user.Expires.HasValue && user.Expires.Value < baseDateTime},{user.PasswordNeverExpires},{user.PasswordNotRequired},{user.PasswordLastChanged},{user.LastLogon},\"{user.Dn}\"");
                            Console.WriteLine($"  {user.SamAccountName}:{user.Rid}:{user.LmHash}:{user.NtHash}:{user.ClearTextPassword}::");
                        }
                    }
                }
            }

            watch.Stop();
            TimeSpan timespan = watch.Elapsed;
            Console.WriteLine($"[*] dump completed in {timespan.TotalSeconds} seconds.");
            Console.WriteLine($"[*] total {entries} entries dumped.");
            Console.WriteLine();
        }
    }
}
