using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpGetBasisDown
{
    class SavedRDPConnections
    {
    
        public static string GetRegValue(string hive, string path, string value)
        {
            string regKeyValue = "";
            if (hive == "HKCU")
            {
                var regKey = Registry.CurrentUser.OpenSubKey(path);
                if (regKey != null)
                {
                    regKeyValue = String.Format("{0}", regKey.GetValue(value));
                }
                return regKeyValue;
            }
            else if (hive == "HKU")
            {
                var regKey = Registry.Users.OpenSubKey(path);
                if (regKey != null)
                {
                    regKeyValue = String.Format("{0}", regKey.GetValue(value));
                }
                return regKeyValue;
            }
            else
            {
                var regKey = Registry.LocalMachine.OpenSubKey(path);
                if (regKey != null)
                {
                    regKeyValue = String.Format("{0}", regKey.GetValue(value));
                }
                return regKeyValue;
            }
        }

        public static string[] GetRegSubkeys(string hive, string path)
        {
            try
            {
                Microsoft.Win32.RegistryKey myKey = null;
                if (hive == "HKLM")
                {
                    myKey = Registry.LocalMachine.OpenSubKey(path);
                }
                else if (hive == "HKU")
                {
                    myKey = Registry.Users.OpenSubKey(path);
                }
                else
                {
                    myKey = Registry.CurrentUser.OpenSubKey(path);
                }
                String[] subkeyNames = myKey.GetSubKeyNames();
                return myKey.GetSubKeyNames();
            }
            catch
            {
                return new string[0];
            }
        }

        public static void ListSavedRDPConnections()
        {
            string[] SIDs = Registry.Users.GetSubKeyNames();
            foreach (string SID in SIDs)
            {
                if (SID.StartsWith("S-1-5") && !SID.EndsWith("_Classes"))
                {
                    string[] subkeys = GetRegSubkeys("HKU", String.Format("{0}\\Software\\Microsoft\\Terminal Server Client\\Servers", SID));
                    if (subkeys != null)
                    {
                        string sid = ("\r\n\r\n=== Saved RDP Connection Information ("+ SID +") ===");
                        foreach (string host in subkeys)
                        {
                            string username = GetRegValue("HKCU", String.Format("Software\\Microsoft\\Terminal Server Client\\Servers\\{0}", host), "UsernameHint");
                            if (username != "")
                            {
                                string RDPConnections = sid + "\r\n" + username + "\r\n" + host + "\r\n";
                                BasisInfo.TxtWriter(RDPConnections, "SavedRDPConnections");
                            }
                        }
                    }
                }
            }
        }
    }
}
