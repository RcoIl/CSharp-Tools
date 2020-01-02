/* ==============================================================================
 * Author：RcoIl
 * Creation date：2019/12/23 23:39:31
 * CLR Version :4.0.30319.42000
 * Blog: https://rcoil.me
 * ==============================================================================*/

using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Text;

namespace SharpReversiblePWD
{
    class GPO
    {

        internal static Int64 ConvertLargeIntegerToInt64(object largeInteger)
        {
            Type ltype = largeInteger.GetType();
            Int32 highPart = (Int32)ltype.InvokeMember("HighPart",
                                                        System.Reflection.BindingFlags.GetProperty,
                                                        null,
                                                        largeInteger,
                                                        null);

            Int32 lowPart = (Int32)ltype.InvokeMember("LowPart",
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

        public static void ReadPasswordPolicy()
        {
            Console.WriteLine("  [+] Reading Defaul PasswordPolicy...");
            string strContext = "";

            using (var container = new DirectoryEntry("LDAP://rootDSE"))
            {
                strContext = container.Properties["defaultNamingContext"].Value.ToString();
            }

            using (var rootDomain = new DirectoryEntry("LDAP://" + strContext))
            {

                int pwdProperties = (int)rootDomain.Properties["pwdProperties"].Value;
                int minPwdLength = (int)rootDomain.Properties["minPwdLength"].Value;
                long minPwdAge = ConvertLargeIntegerToInt64(rootDomain.Properties["minPwdAge"].Value);
                long maxPwdAge = ConvertLargeIntegerToInt64(rootDomain.Properties["maxPwdAge"].Value);
                int pwdHistoryLength = (int)rootDomain.Properties["pwdHistoryLength"].Value;

                if ((pwdProperties & 0x00000001) != 0)
                {
                    Console.WriteLine("    [>] PasswordComplexityEnabled: TRUE");
                }
                else
                {
                    Console.WriteLine("    [>] PasswordComplexityEnabled: FALSE");
                }
                
                Console.WriteLine("    [>] MinimumPasswordLength: " + minPwdLength);
                Console.WriteLine("    [>] MinimumPasswordAge: " + ConvertTime(minPwdAge));
                Console.WriteLine("    [>] MaximumPasswordAge: " + ConvertTime(maxPwdAge));
                Console.WriteLine("    [>] PasswordHistoryLength: " + pwdHistoryLength);

                //用可还原的加密来存储密码
                if ((pwdProperties & 0x00000010) != 0)
                {
                    Console.WriteLine("    [>] ReversibleEncryptionEnabled: TRUE");
                }
                else
                {
                    Console.WriteLine("    [>] ReversibleEncryptionEnabled: FALSE");
                }
            }
        }

        public static void UpdatePasswordPolicy()
        {
            Console.WriteLine("  [+] Update PasswordPolicy");
            string strContext = "";

            using (var container = new DirectoryEntry("LDAP://rootDSE"))
            {
                strContext = container.Properties["defaultNamingContext"].Value.ToString();
            }

            using (var rootDomain = new DirectoryEntry("LDAP://" + strContext))
            {
                
                int pwdProperties = (int)rootDomain.Properties["pwdProperties"].Value;
                if ((pwdProperties & 0x00000010) == 0)
                {
                    rootDomain.Properties["pwdProperties"].Value = pwdProperties | 0x00000010;
                    rootDomain.CommitChanges();
                    Console.WriteLine("    [>] Update PasswordPolicy complete");
                }
            }
        }

        public static void RepairPasswordPolicy()
        {
            Console.WriteLine("  [+] Repair PasswordPolicy");
            string strContext = "";

            using (var container = new DirectoryEntry("LDAP://rootDSE"))
            {
                strContext = container.Properties["defaultNamingContext"].Value.ToString();
            }

            using (var rootDomain = new DirectoryEntry("LDAP://" + strContext))
            {
                int pwdProperties = (int)rootDomain.Properties["pwdProperties"].Value;
                if ((pwdProperties & 0x00000010) != 0)
                {
                    Console.WriteLine("    [>] Repairing PasswordPolicy....");
                    rootDomain.Properties["pwdProperties"].Value = pwdProperties & ~0x00000010;
                    rootDomain.CommitChanges();
                    Console.WriteLine("    [>] Repair PasswordPolicy complete");
                }
            }
        }
    }
}
