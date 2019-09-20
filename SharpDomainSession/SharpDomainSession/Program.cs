using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Security.Principal;

namespace SharpDomainSession
{
    class Program
    {
        // 第二种方法 : NetSessionEnum -> 普通域用户权限
        #region PInvoke Imports
        [DllImport("netapi32.dll", SetLastError = true)]
        private static extern int NetSessionEnum(
            [In, MarshalAs(UnmanagedType.LPWStr)] string ServerName,
            [In, MarshalAs(UnmanagedType.LPWStr)] string UncClientName,
            [In, MarshalAs(UnmanagedType.LPWStr)] string UserName,
            Int32 Level,
            out IntPtr bufptr,
            int prefmaxlen,
            ref Int32 entriesread,
            ref Int32 totalentries,
            ref Int32 resume_handle);


        [StructLayout(LayoutKind.Sequential)]
        public struct SESSION_INFO_10
        {
            /// <summary>
            /// Unicode字符串，指定建立会话的计算机的名称或 IP 地址.
            /// </summary>
            [MarshalAs(UnmanagedType.LPWStr)] public string sesi10_cname;
            /// <summary>
            /// Unicode字符串，指定建立会话的用户的名称
            /// </summary>
            [MarshalAs(UnmanagedType.LPWStr)] public string sesi10_username;
            /// <summary>
            /// 指定会话处于活动状态的秒数
            /// </summary>
            public uint sesi10_time;
            /// <summary>
            /// 指定会话空闲的秒数
            /// </summary>
            public uint sesi10_idle_time;
        }

        /// <summary>
        /// 各类错误解析
        /// </summary>
        public enum NERR
        {
            /// <summary>
            /// 操作成功。
            /// </summary>
            NERR_Success = 0,
            /// <summary>
            /// 可供阅读的更多数据。dderror获取所有数据。
            /// </summary>
            ERROR_MORE_DATA = 234,
            /// <summary>
            /// 网络浏览器不可用。
            /// </summary>
            ERROR_NO_BROWSER_SERVERS_FOUND = 6118,
            /// <summary>
            /// 指定的LEVEL对此调用无效。
            /// </summary>
            ERROR_INVALID_LEVEL = 124,
            /// <summary>
            /// 安全上下文无权进行此调用。
            /// </summary>
            ERROR_ACCESS_DENIED = 5,
            /// <summary>
            /// 参数不正确。
            /// </summary>
            ERROR_INVALID_PARAMETER = 87,
            /// <summary>
            /// 内存不足。
            /// </summary>
            ERROR_NOT_ENOUGH_MEMORY = 8,
            /// <summary>
            /// 无法联系资源。连接超时。
            /// </summary>
            ERROR_NETWORK_BUSY = 54,
            /// <summary>
            /// 找不到网络路径。
            /// </summary>
            ERROR_BAD_NETPATH = 53,
            /// <summary>
            /// 没有可用的网络连接来拨打电话。
            /// </summary>
            ERROR_NO_NETWORK = 1222,
            /// <summary>
            /// 指针无效。
            /// </summary>
            ERROR_INVALID_HANDLE_STATE = 1609,
            /// <summary>
            /// 扩展错误。
            /// </summary>
            ERROR_EXTENDED_ERROR = 1208,
            /// <summary>
            /// Base.
            /// </summary>
            NERR_BASE = 2100,
            /// <summary>
            /// 未知目录。
            /// </summary>
            NERR_UnknownDevDir = (NERR_BASE + 16),
            /// <summary>
            /// 服务器上已存在重复共享。
            /// </summary>
            NERR_DuplicateShare = (NERR_BASE + 18),
            /// <summary>
            /// 内存分配很小。
            /// </summary>
            NERR_BufTooSmall = (NERR_BASE + 23)
        }
        #endregion
        // 第二种方法 : NetWkstaUserEnum -> 需要域管权限
        #region PInvoke Imports
        [DllImport("netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int NetWkstaUserEnum(
            string servername,
            int level,
            out IntPtr bufptr,
            int prefmaxlen,
            out int entriesread,
            out int totalentries,
            ref int resume_handle);

        [DllImport("netapi32.dll")]
        private static extern int NetApiBufferFree(
            IntPtr Buff);

        const int NERR_SUCCESS = 0;
        const int ERROR_MORE_DATA = 234;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WKSTA_USER_INFO_0
        {
            public string wkui0_username;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WKSTA_USER_INFO_1
        {
            public string wkui1_username;
            public string wkui1_logon_domain;
            public string wkui1_oth_domains;
            public string wkui1_logon_server;
        }
        #endregion

        /// <summary>
        /// 返回指定服务器的所有 SESSIONS。返回 SESSION_INFO_10 结构的数组。
        /// https://www.pinvoke.net/default.aspx/netapi32/NetSessionEnum.html
        /// </summary>
        /// <param name="server">默认所有域内机器，隐形目标：域控制器+共享服务器</param>
        /// <returns>SESSION_INFO_10 STRUCTURE ARRAY</returns>
        public static SESSION_INFO_10[] EnumSessions(string server)
        {
            IntPtr Bufptr;
            int nStatus = 0;
            Int32 dwEntriesread = 0, dwTotalentries = 0, dwResume_handle = 0;

            Bufptr = (IntPtr)Marshal.SizeOf(typeof(SESSION_INFO_10));
            SESSION_INFO_10[] results = new SESSION_INFO_10[0];
            do
            {
                nStatus = NetSessionEnum(server, null, null, 10, out Bufptr, -1, ref dwEntriesread, ref dwTotalentries, ref dwResume_handle);
                results = new SESSION_INFO_10[dwEntriesread];
                if (nStatus == (int)NERR.ERROR_MORE_DATA || nStatus == (int)NERR.NERR_Success)
                {
                    Int32 p = Bufptr.ToInt32();
                    for (int i = 0; i < dwEntriesread; i++)
                    {

                        SESSION_INFO_10 si = (SESSION_INFO_10)Marshal.PtrToStructure(new IntPtr(p), typeof(SESSION_INFO_10));
                        results[i] = si;
                        p += Marshal.SizeOf(typeof(SESSION_INFO_10));
                    }
                }
                // 释放先前从进程的非托管内存分配的内存。
                Marshal.FreeHGlobal(Bufptr);
            }
            while (nStatus == (int)NERR.ERROR_MORE_DATA);

            // 该 NetApiBufferFree函数释放该内存 NetApiBufferAllocate功能分配。应用程序还应调用NetApiBufferFree释放其他网络管理功能在内部使用的内存以返回信息。
            //NetApiBufferFree(BufPtr);

            return results;
        }

        /// <summary>
        /// API 调用的第二个参数是 API 调用的级别，其中 1 返回的数据多于 0，所以选择 1进行测试
        /// https://www.pinvoke.net/default.aspx/netapi32/netwkstauserenum.html
        /// </summary>
        /// <param name="server">默认所有域内机器，隐形目标：域控制器+共享服务器</param>
        /// <returns></returns>
        public static WKSTA_USER_INFO_1[] EnumWkstaUser(string server)
        {
            IntPtr Bufptr;
            int nStatus = 0;
            Int32 dwEntriesread = 0, dwTotalentries = 0, dwResumehandle = 0;

            Bufptr = (IntPtr)Marshal.SizeOf(typeof(WKSTA_USER_INFO_1));
            WKSTA_USER_INFO_1[] results = new WKSTA_USER_INFO_1[0];
            do
            {
                nStatus = NetWkstaUserEnum(server, 1, out Bufptr, 32768, out dwEntriesread, out dwTotalentries, ref dwResumehandle);
                results = new WKSTA_USER_INFO_1[dwEntriesread];
                if ((nStatus == NERR_SUCCESS) || (nStatus == ERROR_MORE_DATA))
                {
                    if (dwEntriesread > 0)
                    {
                        IntPtr pstruct = Bufptr;
                        for (int i = 0; i < dwEntriesread; i++)
                        {
                            WKSTA_USER_INFO_1 wui1 = (WKSTA_USER_INFO_1)Marshal.PtrToStructure(pstruct, typeof(WKSTA_USER_INFO_1));
                            results[i] = wui1;
                            pstruct = (IntPtr)((int)pstruct + Marshal.SizeOf(typeof(WKSTA_USER_INFO_1)));
                        }
                    }
                }

                if (Bufptr != IntPtr.Zero)
                    NetApiBufferFree(Bufptr);

            } while (nStatus == ERROR_MORE_DATA);
            return results;
        }

        // 第三种方法 : 远程注册表 -> 需要域管权限
        /// <summary>
        /// 利用 OpenRemoteBaseKey 读取 HKEY_USERS 的键项
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        private static IEnumerable<string> GetRegistryLoggedOn(string server)
        {
            
            var users = new List<string>();
            try
            {
                // 远程打开注册表配置单元，如果它不是我们当前的配置单元
                RegistryKey key = RegistryKey.OpenRemoteBaseKey(RegistryHive.Users, server);

                // 找到与我们的正则表达式匹配的所有子项
                var filtered = key.GetSubKeyNames().Where(sub => SidRegex.IsMatch(sub));

                foreach (var subkey in filtered)
                {
                    users.Add(subkey);
                }
            }
            catch (Exception)
            {
                yield break;
            }

            foreach (var user in users.Distinct())
            {
                yield return user;
            }
        }
        private static readonly Regex SidRegex = new Regex(@"S-1-5-21-[0-9]+-[0-9]+-[0-9]+-[0-9]+$", RegexOptions.Compiled);

        public static void Main(string[] args)
        {
            string host = Environment.MachineName;
            host = args[0];

            Console.WriteLine();
            Console.WriteLine("[+] NetSessionEnum 演示：");
            Console.WriteLine("  [*] {0,-20}{1,-35}{2,-15}{3,-10}", "Username", "Host", "time", "idle_time");
            Console.WriteLine();
            SESSION_INFO_10[] Info_10 = EnumSessions(host);
            for (int i = 0; i < Info_10.Length; i++)
            {
                Console.WriteLine("  [>] {0,-20}{1,-35}{2,-15}{3,-10}", Info_10[i].sesi10_username, Regex.Replace(Info_10[i].sesi10_cname, @"\\", ""), Info_10[i].sesi10_time, Info_10[i].sesi10_idle_time);
            }

            Console.WriteLine();
            Console.WriteLine("[+] NetWkstaUserEnum 演示：");
            Console.WriteLine("  [*] {0,-20}{1,-35}{2,-15}{3,-10}", "Username", "Logon_server", "Logon_domain", "Oth_domains");
            Console.WriteLine();
            WKSTA_USER_INFO_1[] Info_1 = EnumWkstaUser(host);
            for (int i = 0; i < Info_1.Length; i++)
            {
                Console.WriteLine("  [>] {0,-20}{1,-35}{2,-15}{3,-10}", Info_1[i].wkui1_username, Info_1[i].wkui1_logon_server, Info_1[i].wkui1_logon_domain, Info_1[i].wkui1_oth_domains);
            }

            Console.WriteLine();
            Console.WriteLine("[+] OpenRemoteBaseKey 演示：");
            Console.WriteLine("  [*] {0,-45}    {1,-25}{2,-35}", "SID", " ","Host");
            Console.WriteLine();
            string Username;
            foreach (string regSID in GetRegistryLoggedOn(host))
            {

                Username = new SecurityIdentifier(regSID).Translate(typeof(NTAccount)).ToString();
                Console.WriteLine("  [>] {0,-45} -> {1,-25}{2,-35}", regSID, Username, host);
            }
        }
    }
}
