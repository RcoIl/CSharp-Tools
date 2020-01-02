/* ==============================================================================
 * Author：RcoIl
 * Creation date：2019/12/23 23:20:31
 * CLR Version :4.0.30319.42000
 * Blog: https://rcoil.me
 * ==============================================================================*/

// http://www.utools.nl/downloads/UnlockADDS.pdf#509

using System;
using System.Collections.Generic;
using System.Data;
using System.DirectoryServices;
using System.Text;

namespace SharpReversiblePWD
{
    class PSO
    {
        internal static Int64 ConvertLargeIntegerToInt64(object largeInteger)
        {

            Int32 highPart = (Int32)largeInteger.GetType().InvokeMember("HighPart",
                                                                        System.Reflection.BindingFlags.GetProperty,
                                                                        null,
                                                                        largeInteger,
                                                                        null);

            Int32 lowPart = (Int32)largeInteger.GetType().InvokeMember("LowPart",
                                                                        System.Reflection.BindingFlags.GetProperty,
                                                                        null,
                                                                        largeInteger,
                                                                        null);

            return ((Int64)highPart) << 32 + lowPart;
        }

        private static string ConvertTime(long timeAge)
        {
            timeAge = Math.Abs(timeAge);
            TimeSpan helpTS = TimeSpan.FromTicks(timeAge);
            string timeScale = String.Format("{0:d2}.{1:d2}:{2:d2}:{3:d2}", helpTS.Days, helpTS.Hours, helpTS.Minutes, helpTS.Seconds);
            return timeScale;
        }

        private static string PSC()
        {
            string strContext = null;
            using (var container = new DirectoryEntry("LDAP://RootDSE"))
            {
                strContext = "CN=Password Settings Container,CN=System," + container.Properties["defaultNamingContext"].Value.ToString();
                // CN=Password Settings Container,CN=System,DC=rcoil,DC=me
            }
            return strContext;
        }

        public static void ReadPSO()
        {
            using (var psoEntry = new DirectoryEntry(@"LDAP://" + PSC()))
            {
                DirectorySearcher search = new DirectorySearcher(psoEntry);
                search.Filter = "(objectClass=msDS-PasswordSettings)";
                SearchResultCollection results = search.FindAll();
                if (results != null)
                {
                    Console.WriteLine("  [+] Reading PSO...");
                    foreach (SearchResult psoResult in results)
                    {
                        DirectoryEntry pso = psoResult.GetDirectoryEntry();

                        if (pso.Properties.Contains("cn"))
                        {
                            Console.WriteLine("    [>] PSO Name: {0}", pso.Properties["cn"].Value.ToString());
                            Console.WriteLine("    [>] PSO GUID: {0}", pso.Guid.ToString()); //Guid guid = new Guid((byte[])pso.Properties["objectGUID"].Value);Console.WriteLine("    [>] PSO GUID: {0}", guid.ToString());
                            Console.WriteLine("    [>] PSO distinguishedName: {0}", (string)pso.Properties["distinguishedName"].Value.ToString());
                            Console.WriteLine("    [>] PSO Precedence: {0}", (int)pso.Properties["msDS-PasswordSettingsPrecedence"].Value);
                            Console.WriteLine("    [>] MinimumPasswordLength: {0}", (int)pso.Properties["msDS-MinimumPasswordLength"].Value);
                            Console.WriteLine("    [>] PasswordHistoryLength: {0}", (int)pso.Properties["msDS-PasswordHistoryLength"].Value);
                            if ((bool)pso.Properties["msDS-PasswordComplexityEnabled"].Value)
                            {
                                Console.WriteLine("    [>] PasswordComplexityEnabled: TRUE");
                            }
                            else
                            {
                                Console.WriteLine("    [>] PasswordComplexityEnabled: FALSE");
                            }
                            if ((bool)pso.Properties["msDS-PasswordReversibleEncryptionEnabled"].Value)
                            {
                                Console.WriteLine("    [>] ReversibleEncryptionEnabled: TRUE");
                            }
                            else
                            {
                                Console.WriteLine("    [>] ReversibleEncryptionEnabled: FALSE");
                            }

                            long minPwdAge = ConvertLargeIntegerToInt64(pso.Properties["msDS-MinimumPasswordAge"].Value);
                            Console.WriteLine("    [>] MinimumPasswordAge: {0}", ConvertTime(minPwdAge));

                            long maxPwdAge = ConvertLargeIntegerToInt64(pso.Properties["msDS-MaximumPasswordAge"].Value);
                            Console.WriteLine("    [>] MaximumPasswordAge: {0}", ConvertTime(maxPwdAge));

                            int lockoutThreshold = (int)pso.Properties["msDS-LockoutThreshold"].Value;
                            Console.WriteLine("    [>] LockoutThreshold: {0}", lockoutThreshold);

                            long lockoutObservationWindow = ConvertLargeIntegerToInt64(pso.Properties["msDS-LockoutObservationWindow"].Value);
                            Console.WriteLine("    [>] LockoutObservationWindow: {0}", ConvertTime(lockoutObservationWindow));

                            long lockoutDuration = ConvertLargeIntegerToInt64(pso.Properties["msDS-LockoutDuration"].Value);
                            Console.WriteLine("    [>] LockoutDuration: {0}", ConvertTime(lockoutDuration));

                            if ((string)pso.Properties["msDS-PSOAppliesTo"].Value == null)
                            {
                                Console.WriteLine("    [>] PSOAppliesTo: NULL");
                            }
                            else
                            {
                                Console.WriteLine("    [>] PSOAppliesTo: {0}", (string)pso.Properties["msDS-PSOAppliesTo"].Value);
                            }
                            Console.WriteLine();
                        }
                    }
                }
                else
                {
                    Console.WriteLine("  [+] Nothing exists");
                }
            }
            

        }

        public static void CreatePSO()
        {
            Console.WriteLine("  [+] Creating PSO...");
            using (var container = new DirectoryEntry("LDAP://" + PSC()))
            {
                using (var newEntry = container.Children.Add("CN=TestPSO", "msDS-PasswordSettings"))
                {
                    try
                    {
                        newEntry.Properties["msDS-PasswordSettingsPrecedence"].Value = 1;
                        newEntry.Properties["msDS-PasswordReversibleEncryptionEnabled"].Value = true;
                        newEntry.Properties["msDS-PasswordHistoryLength"].Value = 24;
                        newEntry.Properties["msDS-PasswordComplexityEnabled"].Value = true;
                        newEntry.Properties["msDS-MinimumPasswordLength"].Value = 8;
                        newEntry.Properties["msDS-MinimumPasswordAge"].Value = -10;
                        newEntry.Properties["msDS-MaximumPasswordAge"].Value = -9000;
                        newEntry.Properties["msDS-LockoutThreshold"].Value = 0;
                        newEntry.Properties["msDS-LockoutObservationWindow"].Value = -10;
                        newEntry.Properties["msDS-LockoutDuration"].Value = -20;
                        //newEntry.Properties["msDS-PSOAppliesTo"].Value = "CN=Domain Admins,CN=Users,DC=rcoil,DC=me";
                        newEntry.CommitChanges();
                        Console.WriteLine("  [+] Create PSO successfully");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("  [+] Create PSO failure... Exception: {0}", e.Message);
                    }
                }
            }
        }

        public static void UpdatePSO(string cnPSO)
        {
            Console.WriteLine("  [+] Update rawPSO");
            using (var pso = new DirectoryEntry("LDAP://" + cnPSO))
            {
                try
                {
                    pso.Properties["msDS-PasswordReversibleEncryptionEnabled"].Value = true;
                    //pso.Properties["msDS-PSOAppliesTo"].Value = "CN=Domain Admins,CN=Users,DC=rcoil,DC=me";
                    pso.CommitChanges();
                    Console.WriteLine("  [+] Update completed");
                }
                catch(Exception e)
                {
                    Console.WriteLine("  [+] Update failed -> Exception: {0}", e.Message);
                }
            }
        }

        public static void DeletePSO(string cnPSO)
        {
            using (var pso = new DirectoryEntry("LDAP://" + PSC()))
            {
                using (var dnPSO = new DirectoryEntry("LDAP://"+ cnPSO))
                {
                    try
                    {
                        pso.Children.Remove(dnPSO);
                        pso.CommitChanges();
                        Console.WriteLine("  [+]  Successfully deleted");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("  [+] Failed to delete -> Exception: {0}", e.Message);
                    }

                }
            }
        }
    }
}
