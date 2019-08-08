using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SharpOfficeInfo
{
    class Program
    {
        // 通过注册表检测 Office 是否开启宏
        private static void OfficeVBAWarnings(string OfficeVersion)
        {
            Console.WriteLine();
            List<string> OfficeFeatures = new List<string>()
            {
                "Excel", "Word", "PowerPoint"
            };
            foreach (string Features in OfficeFeatures)
            {
                string basekey = @"SOFTWARE\Microsoft\Office\" + OfficeVersion + @"\" + Features + @"\Security";
                RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(basekey);
                if (registryKey != null)
                {
                    string[] ValueNames = registryKey.GetValueNames();
                    if (registryKey.ValueCount == 0)
                    {
                        Console.WriteLine("  [>] {0,-10} 宏状态：禁用所有宏，并发出通知（D）- 2", Features);
                    }
                    else
                    {
                        foreach (string KeyName in ValueNames)
                        {
                            object VBAWarnings = registryKey.GetValue("VBAWarnings");
                            if (VBAWarnings.ToString() == "1")
                            {
                                Console.WriteLine("  [>] {0,-10} 宏状态: 启用所有宏（不推荐；可能会运行有潜在危险的代码）（E）- 1", Features);
                            }
                            else if (VBAWarnings.ToString() == "2")
                            {
                                Console.WriteLine("  [>] {0,-10} 宏状态：禁用所有宏，并发出通知（D）- 2", Features);
                            }
                            else if (VBAWarnings.ToString() == "3")
                            {
                                Console.WriteLine("  [>] {0,-10} 宏状态: 禁用无数字签署的所有宏（G）- 3", Features);
                            }
                            else if (VBAWarnings.ToString() == "4")
                            {
                                Console.WriteLine("  [>] {0,-10} 宏状态：禁用所有宏，并不发出通知（L）- 4", Features);
                            }
                        }
                    }
                }
            }
        }

        // 通过注册表检测 Office 版本
        private static void OfficeIsInstall(string OfficeVersion)
        {
            string basekey = @"SOFTWARE\Microsoft\Office\" + OfficeVersion + @"\Common\InstallRoot";
            RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(basekey);
            if (registryKey != null)
            {
                if (registryKey.GetValue("Path") != null)
                {
                    if (OfficeVersion == "8.0")
                    {
                        Console.WriteLine("  [>] Microsoft Office Version: Office97");
                        Console.WriteLine("  [>] Office 安装路径：{0}", registryKey.GetValue("Path"));
                        OfficeVBAWarnings(OfficeVersion);
                    }
                    else if (OfficeVersion == "9.0")
                    {
                        Console.WriteLine("  [>] Microsoft Office Version: Office2000");
                        Console.WriteLine("  [>] Office 安装路径：{0}", registryKey.GetValue("Path"));
                        OfficeVBAWarnings(OfficeVersion);
                    }
                    else if (OfficeVersion == "10.0")
                    {
                        Console.WriteLine("  [>] Microsoft Office Version: OfficeXP");
                        Console.WriteLine("  [>] Office 安装路径：{0}", registryKey.GetValue("Path"));
                        OfficeVBAWarnings(OfficeVersion);
                    }
                    else if (OfficeVersion == "11.0")
                    {
                        Console.WriteLine("  [>] Microsoft Office Version: Office2003");
                        Console.WriteLine("  [>] Office 安装路径：{0}", registryKey.GetValue("Path"));
                        OfficeVBAWarnings(OfficeVersion);
                    }
                    else if (OfficeVersion == "12.0")
                    {
                        Console.WriteLine("  [>] Microsoft Office Version: Office2007");
                        Console.WriteLine("  [>] Office 安装路径：{0}", registryKey.GetValue("Path"));
                        OfficeVBAWarnings(OfficeVersion);
                    }
                    else if (OfficeVersion == "14.0")
                    {
                        Console.WriteLine("  [>] Microsoft Office Version: Office2010");
                        Console.WriteLine("  [>] Office 安装路径：{0}", registryKey.GetValue("Path"));
                        OfficeVBAWarnings(OfficeVersion);
                    }
                    else if (OfficeVersion == "15.0")
                    {
                        Console.WriteLine("  [>] Microsoft Office Version: Office2013");
                        Console.WriteLine("  [>] Office 安装路径：{0}", registryKey.GetValue("Path"));
                        OfficeVBAWarnings(OfficeVersion);
                    }
                    else if (OfficeVersion == "15.0")
                    {
                        Console.WriteLine("  [>] Microsoft Office Version: Office2013");
                        Console.WriteLine("  [>] Office 安装路径：{0}", registryKey.GetValue("Path"));
                        OfficeVBAWarnings(OfficeVersion);
                    }
                    else if (OfficeVersion == "16.0")
                    {
                        Console.WriteLine("  [>] Microsoft Office Version: Office2016");
                        Console.WriteLine("  [>] Office 安装路径：{0}", registryKey.GetValue("Path"));
                        OfficeVBAWarnings(OfficeVersion);
                    }
                }
            }
            else
            {
                arrayList.Add("  [!] 未安装 Office 软件");
            }
        }

        public static ArrayList arrayList = new ArrayList();

        static void Main(string[] args)
        {
            Console.WriteLine();
            List<string> OfficeVersions = new List<string>()
            {
                "8.0", "9.0", "10.0", "11.0", "12.0", "14.0", "15.0", "16.0"
            };
            foreach (string OfficeVersion in OfficeVersions)
            {
                OfficeIsInstall(OfficeVersion);
            }
            if (arrayList.Count == 8)
            {
                Console.WriteLine();
                Console.WriteLine("  [!] 未安装 Office 软件");
            }
        }
    }
}
