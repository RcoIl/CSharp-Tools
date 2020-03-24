using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;

namespace SharpGetCmdLine
{
    static class Program
    {
        static string GetCommandLineArgs(this Process process)
        {
            string wmiQuery = "SELECT CommandLine FROM Win32_Process WHERE ProcessId =" + process.Id;
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(wmiQuery))
            {
                using (ManagementObjectCollection objects = searcher.Get())
                {
                    return objects.Cast<ManagementBaseObject>().SingleOrDefault()?["CommandLine"]?.ToString();
                }
            }
        }

        public static void Main(string[] args)
        {
            Process[] processlist = Process.GetProcesses();
            foreach (Process process in processlist)
            {
                try
                {
                    //这里加if就是因为这两个进程的某些属性一旦访问就抛出没有权限的异常
                    if (process.ProcessName != "System" && process.ProcessName != "Idle")
                    {
                        string cmdLine = GetCommandLineArgs(process);
                        if (cmdLine != null)
                        {
                            Console.WriteLine("Process: {0,-11} ID: {1,-5} CmdLine: {2,-22}", process.ProcessName, process.Id, GetCommandLineArgs(process));
                        }
                    }
                }
                catch (Exception ex)
                {
                }
            }
        }
    }
}
