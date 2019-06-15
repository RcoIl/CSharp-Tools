using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NetWorkConnectIPC
{
    class Program
    {

        #region Define NetWare Connect Class

        public enum ERROR_ID
        {
            ERROR_SUCCESS = 0,  // Success
            ERROR_BUSY = 170,
            ERROR_MORE_DATA = 234,
            ERROR_NO_BROWSER_SERVERS_FOUND = 6118,
            ERROR_INVALID_LEVEL = 124,
            ERROR_ACCESS_DENIED = 5,
            ERROR_INVALID_PASSWORD = 86,
            ERROR_INVALID_PARAMETER = 87,
            ERROR_BAD_DEV_TYPE = 66,
            ERROR_NOT_ENOUGH_MEMORY = 8,
            ERROR_NETWORK_BUSY = 54,
            ERROR_BAD_NETPATH = 53,
            ERROR_NO_NETWORK = 1222,
            ERROR_INVALID_HANDLE_STATE = 1609,
            ERROR_EXTENDED_ERROR = 1208,
            ERROR_DEVICE_ALREADY_REMEMBERED = 1202,
            ERROR_NO_NET_OR_BAD_PATH = 1203,
            ERROR_SESSION_CREDENTIAL_CONFLICT = 1219
        }

        public enum RESOURCE_SCOPE
        {
            RESOURCE_CONNECTED = 1,
            RESOURCE_GLOBALNET = 2,
            RESOURCE_REMEMBERED = 3,
            RESOURCE_RECENT = 4,
            RESOURCE_CONTEXT = 5
        }

        public enum RESOURCE_TYPE
        {
            RESOURCETYPE_ANY = 0,
            RESOURCETYPE_DISK = 1,
            RESOURCETYPE_PRINT = 2,
            RESOURCETYPE_RESERVED = 8,
        }

        public enum RESOURCE_USAGE
        {
            RESOURCEUSAGE_CONNECTABLE = 1,
            RESOURCEUSAGE_CONTAINER = 2,
            RESOURCEUSAGE_NOLOCALDEVICE = 4,
            RESOURCEUSAGE_SIBLING = 8,
            RESOURCEUSAGE_ATTACHED = 16,
            RESOURCEUSAGE_ALL = (RESOURCEUSAGE_CONNECTABLE | RESOURCEUSAGE_CONTAINER | RESOURCEUSAGE_ATTACHED),
        }

        public enum RESOURCE_DISPLAYTYPE
        {
            RESOURCEDISPLAYTYPE_GENERIC = 0,
            RESOURCEDISPLAYTYPE_DOMAIN = 1,
            RESOURCEDISPLAYTYPE_SERVER = 2,
            RESOURCEDISPLAYTYPE_SHARE = 3,
            RESOURCEDISPLAYTYPE_FILE = 4,
            RESOURCEDISPLAYTYPE_GROUP = 5,
            RESOURCEDISPLAYTYPE_NETWORK = 6,
            RESOURCEDISPLAYTYPE_ROOT = 7,
            RESOURCEDISPLAYTYPE_SHAREADMIN = 8,
            RESOURCEDISPLAYTYPE_DIRECTORY = 9,
            RESOURCEDISPLAYTYPE_TREE = 10,
            RESOURCEDISPLAYTYPE_NDSCONTAINER = 11
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct NETRESOURCE
        {
            public RESOURCE_SCOPE dwScope;
            public RESOURCE_TYPE dwType;
            public RESOURCE_DISPLAYTYPE dwDisplayType;
            public RESOURCE_USAGE dwUsage;

            [MarshalAs(UnmanagedType.LPStr)]
            public string lpLocalName;

            [MarshalAs(UnmanagedType.LPStr)]
            public string lpRemoteName;

            [MarshalAs(UnmanagedType.LPStr)]
            public string lpComment;

            [MarshalAs(UnmanagedType.LPStr)]
            public string lpProvider;

        }

        public class NetworkConnection
        {

            [DllImport("mpr.dll")]
            public static extern int WNetAddConnection2A(NETRESOURCE[] lpNetResource, string lpPassword, string lpUserName, int dwFlags);

            [DllImport("mpr.dll")]
            public static extern int WNetCancelConnection2A(string sharename, int dwFlags, int fForce);

            public static int Connect(string remotePath, string localPath, string username, string password)
            {
                NETRESOURCE[] share_driver = new NETRESOURCE[1];
                share_driver[0].dwScope = RESOURCE_SCOPE.RESOURCE_GLOBALNET;
                share_driver[0].dwType = RESOURCE_TYPE.RESOURCETYPE_DISK;
                share_driver[0].dwDisplayType = RESOURCE_DISPLAYTYPE.RESOURCEDISPLAYTYPE_SHARE;
                share_driver[0].dwUsage = RESOURCE_USAGE.RESOURCEUSAGE_CONNECTABLE;
                share_driver[0].lpLocalName = localPath;
                share_driver[0].lpRemoteName = remotePath;

                Disconnect(localPath);
                int ret = WNetAddConnection2A(share_driver, password, username, 1);

                return ret;
            }

            public static int Disconnect(string localpath)
            {
                return WNetCancelConnection2A(localpath, 1, 1);
            }

            
        }
        #endregion

        #region Define GetUserInfo Class
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct USER_INFO_0
        {
            public string UserName;
        }

        [DllImport("Netapi32.dll")]
        extern static int NetUserEnum([MarshalAs(UnmanagedType.LPWStr)] string servername, int level, int filter, out IntPtr bufptr, int prefmaxlen, out int entriesread, out int totalentries, out int resume_handle);

        [DllImport("Netapi32.dll")]
        extern static int NetApiBufferFree(IntPtr Buffer);

        public static List<string> GetAllUsersOfSystem(string ip)
        {
            List<string> users = new List<string>();

            int EntriesRead;
            int TotalEntries;
            int Resume;
            IntPtr bufPtr;

            NetUserEnum(ip, 0, 2, out bufPtr, -1, out EntriesRead, out TotalEntries, out Resume);

            if (EntriesRead > 0)
            {
                USER_INFO_0[] Users = new USER_INFO_0[EntriesRead];
                IntPtr iter = bufPtr;

                for (int i = 0; i < EntriesRead; i++)
                {
                    Users[i] = (USER_INFO_0)Marshal.PtrToStructure(iter, typeof(USER_INFO_0));
                    iter = (IntPtr)((int)iter + Marshal.SizeOf(typeof(USER_INFO_0)));
                    users.Add(Users[i].UserName);
                }
                NetApiBufferFree(bufPtr);
            }
            return users;
        }
        #endregion

        static void Main(string[] args)
        {
            string ip = "192.10.22.233";
            string serverPath = @"\\" + ip +"\\IPC$";
            string loginUser = "administrator";
            string loginPassword = "xxxxx";

            Console.WriteLine("[+] IP Address: {0}", ip);
            int status = NetworkConnection.Connect(serverPath, null, loginUser, loginPassword);
            if (status == (int)ERROR_ID.ERROR_SUCCESS)
            {
                List<string> users = GetAllUsersOfSystem(ip);
                foreach (string user in users)
                {
                    Console.WriteLine("    [>]: "+ user);
                }
            }
            else
            {   
                // 连接失败
                Console.WriteLine("  [-] Connection Error：{0}", status);
            }                
            // 断开连接
            NetworkConnection.Disconnect(serverPath);
        }
    }
}
