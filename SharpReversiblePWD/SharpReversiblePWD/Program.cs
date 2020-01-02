/* ==============================================================================
 * Author：RcoIl
 * Creation date：2019/12/23 22:22:22
 * CLR Version :4.0.30319.42000
 * Blog: https://rcoil.me
 * ==============================================================================*/

using System;

namespace SharpReversiblePWD
{
    class Program
    {
        private static void Usage()
        {
            string usage = "";
            usage += "[*] Usage:\r\n";
            usage += "  [+] SharpReversiblePWD.exe -Moudel <options>\r\n";
            usage += "  [+] Moudel: -Account User <-Repair>\r\n";
            usage += "  [+]         -GPO -Read/-Update/-Repair\r\n";
            usage += "  [+]         -PSO -Read/-Create/-Update <dnPSO>/-Delete <dnPSO>\r\n";
            Console.WriteLine(usage);
        }
        static void Main(string[] args)
        {
            String Banner = Properties.Resources.banner;
            Console.WriteLine(Banner);
            
            if (args.Length < 2)
            {
                Usage();
            }
            else if (args[0] == "-Account")
            {
                Console.WriteLine("[*] Account related");
                if (args.Length == 2)
                {
                    AccountContril.UpdateUsers(args[1]);
                }
                else if (args.Length == 3 || args[2] == "-Repair")
                {
                    AccountContril.RepairUsers(args[1]);
                }
            }
            else if (args[0] == "-GPO")
            {
                Console.WriteLine("[*] GPO related");
                if (args[1] == "-Read")
                    GPO.ReadPasswordPolicy();
                else if (args[1] == "-Update")
                    GPO.UpdatePasswordPolicy();
                else if (args[1] == "-Repair")
                    GPO.RepairPasswordPolicy();
            }
            else if (args[0] == "-PSO")
            {
                Console.WriteLine("[*] PSO related");
                if (args[1] == "-Read")
                    PSO.ReadPSO();
                if (args[1] == "-Create")
                    PSO.CreatePSO();
                else if (args[1] == "-Update")
                    PSO.UpdatePSO(args[2]);
                else if (args[1] == "-Delete")
                    PSO.DeletePSO(args[2]);
            }
        }
    }
}
