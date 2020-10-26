using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpSetFileTimes
{
    class Program
    {
        #region DllImport kernel32.dll
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetFileTime(
            IntPtr hFile,
            ref long lpCreationTime,
            ref long lpLastAccessTime,
            ref long lpLastWriteTime);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr CreateFile(
           string lpFileName,
           EFileAccess dwDesiredAccess,
           EFileShare dwShareMode,
           IntPtr lpSecurityAttributes,
           ECreationDisposition dwCreationDisposition,
           EFileAttributes dwFlagsAndAttributes,
           IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern void CloseHandle(IntPtr hFile);


        [Flags]
        enum EFileAccess : uint
        {
            //
            // Standart Section
            //

            AccessSystemSecurity = 0x1000000,   // AccessSystemAcl access type
            MaximumAllowed = 0x2000000,     // MaximumAllowed access type

            Delete = 0x10000,
            ReadControl = 0x20000,
            WriteDAC = 0x40000,
            WriteOwner = 0x80000,
            Synchronize = 0x100000,

            StandardRightsRequired = 0xF0000,
            StandardRightsRead = ReadControl,
            StandardRightsWrite = ReadControl,
            StandardRightsExecute = ReadControl,
            StandardRightsAll = 0x1F0000,
            SpecificRightsAll = 0xFFFF,

            FILE_READ_DATA = 0x0001,        // file & pipe
            FILE_LIST_DIRECTORY = 0x0001,       // directory
            FILE_WRITE_DATA = 0x0002,       // file & pipe
            FILE_ADD_FILE = 0x0002,         // directory
            FILE_APPEND_DATA = 0x0004,      // file
            FILE_ADD_SUBDIRECTORY = 0x0004,     // directory
            FILE_CREATE_PIPE_INSTANCE = 0x0004, // named pipe
            FILE_READ_EA = 0x0008,          // file & directory
            FILE_WRITE_EA = 0x0010,         // file & directory
            FILE_EXECUTE = 0x0020,          // file
            FILE_TRAVERSE = 0x0020,         // directory
            FILE_DELETE_CHILD = 0x0040,     // directory
            FILE_READ_ATTRIBUTES = 0x0080,      // all
            FILE_WRITE_ATTRIBUTES = 0x0100,     // all

            //
            // Generic Section
            //

            GenericRead = 0x80000000,
            GenericWrite = 0x40000000,
            GenericExecute = 0x20000000,
            GenericAll = 0x10000000,

            SPECIFIC_RIGHTS_ALL = 0x00FFFF,
            FILE_ALL_ACCESS =
            StandardRightsRequired |
            Synchronize |
            0x1FF,

            FILE_GENERIC_READ =
            StandardRightsRead |
            FILE_READ_DATA |
            FILE_READ_ATTRIBUTES |
            FILE_READ_EA |
            Synchronize,

            FILE_GENERIC_WRITE =
            StandardRightsWrite |
            FILE_WRITE_DATA |
            FILE_WRITE_ATTRIBUTES |
            FILE_WRITE_EA |
            FILE_APPEND_DATA |
            Synchronize,

            FILE_GENERIC_EXECUTE =
            StandardRightsExecute |
              FILE_READ_ATTRIBUTES |
              FILE_EXECUTE |
              Synchronize
        }

        [Flags]
        public enum EFileShare : uint
        {
            /// <summary>
            ///
            /// </summary>
            None = 0x00000000,
            /// <summary>
            /// Enables subsequent open operations on an object to request read access.
            /// Otherwise, other processes cannot open the object if they request read access.
            /// If this flag is not specified, but the object has been opened for read access, the function fails.
            /// </summary>
            Read = 0x00000001,
            /// <summary>
            /// Enables subsequent open operations on an object to request write access.
            /// Otherwise, other processes cannot open the object if they request write access.
            /// If this flag is not specified, but the object has been opened for write access, the function fails.
            /// </summary>
            Write = 0x00000002,
            /// <summary>
            /// Enables subsequent open operations on an object to request delete access.
            /// Otherwise, other processes cannot open the object if they request delete access.
            /// If this flag is not specified, but the object has been opened for delete access, the function fails.
            /// </summary>
            Delete = 0x00000004
        }

        public enum ECreationDisposition : uint
        {
            /// <summary>
            /// Creates a new file. The function fails if a specified file exists.
            /// </summary>
            New = 1,
            /// <summary>
            /// Creates a new file, always.
            /// If a file exists, the function overwrites the file, clears the existing attributes, combines the specified file attributes,
            /// and flags with FILE_ATTRIBUTE_ARCHIVE, but does not set the security descriptor that the SECURITY_ATTRIBUTES structure specifies.
            /// </summary>
            CreateAlways = 2,
            /// <summary>
            /// Opens a file. The function fails if the file does not exist.
            /// </summary>
            OpenExisting = 3,
            /// <summary>
            /// Opens a file, always.
            /// If a file does not exist, the function creates a file as if dwCreationDisposition is CREATE_NEW.
            /// </summary>
            OpenAlways = 4,
            /// <summary>
            /// Opens a file and truncates it so that its size is 0 (zero) bytes. The function fails if the file does not exist.
            /// The calling process must open the file with the GENERIC_WRITE access right.
            /// </summary>
            TruncateExisting = 5
        }

        [Flags]
        public enum EFileAttributes : uint
        {
            Readonly = 0x00000001,
            Hidden = 0x00000002,
            System = 0x00000004,
            Directory = 0x00000010,
            Archive = 0x00000020,
            Device = 0x00000040,
            Normal = 0x00000080,
            Temporary = 0x00000100,
            SparseFile = 0x00000200,
            ReparsePoint = 0x00000400,
            Compressed = 0x00000800,
            Offline = 0x00001000,
            NotContentIndexed = 0x00002000,
            Encrypted = 0x00004000,
            Write_Through = 0x80000000,
            Overlapped = 0x40000000,
            NoBuffering = 0x20000000,
            RandomAccess = 0x10000000,
            SequentialScan = 0x08000000,
            DeleteOnClose = 0x04000000,
            BackupSemantics = 0x02000000,
            PosixSemantics = 0x01000000,
            OpenReparsePoint = 0x00200000,
            OpenNoRecall = 0x00100000,
            FirstPipeInstance = 0x00080000
        }

        #endregion

        /// <summary>
        /// 修改文件/文件夹属性 -> 文件/文件夹的创建时间，最后访问时间，最后修改时间
        /// </summary>
        /// <param name="hFile"></param>
        /// <param name="creationTime"></param>
        /// <param name="accessTime"></param>
        /// <param name="writeTime"></param>
        public static void SetFileTimes(IntPtr hFile, DateTime creationTime, DateTime accessTime, DateTime writeTime)
        {
            long lCreationTime = creationTime.ToFileTime();
            long lAccessTime = accessTime.ToFileTime();
            long lWriteTime = writeTime.ToFileTime();

            if (SetFileTime(hFile, ref lCreationTime, ref lAccessTime, ref lWriteTime))
            {
                Console.WriteLine("[*] Successfully modified....");
                Console.WriteLine("    [>] LastWriteTime: {0}", writeTime);
                Console.WriteLine("    [>] LastAccessTime: {0}", accessTime);
            }
            else
            {
                throw new Win32Exception();
            }
        }

        static void Usage()
        {
            string fileName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
            Console.WriteLine(@"
Usage: " + fileName + @" <-p file-path> <-a DateTime> <-w DateTime>
    -p      文件/文件夹路径
    -a      文件/文件夹最后一次访问时间
    -w      文件/文件夹最后一次修改时间

    Example: " + fileName + @" -p C:\Users\RcoIl\Desktop\Demo.txt -a 20201024111259 -w 20201024100150
");
        }

        static void Main(string[] args)
        {
            string filePath = String.Empty;
            string lastAccessTime = String.Empty;
            string lastWriteTime = String.Empty;

            if (args.Length > 3)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].ToLower().Contains("-p") && args.Length > i + 1)
                    {
                        filePath = args[i + 1];
                    }
                    else if (args[i].ToLower().Contains("-a") && args.Length > i + 1)
                    {
                        lastAccessTime = args[i + 1];
                    }
                    else if (args[i].ToLower().Contains("-w") && args.Length > i + 1)
                    {
                        lastWriteTime = args[i + 1];
                    }
                }
            }
            else 
            { 
                Usage();
                Environment.Exit(0);
            }

            DateTime NowTime = DateTime.UtcNow;
            if (lastAccessTime != String.Empty)
            {
                NowTime = DateTime.ParseExact(lastAccessTime, "yyyyMMddhhmmss", CultureInfo.CurrentCulture);
            }
            DateTime modified = DateTime.ParseExact(lastWriteTime, "yyyyMMddhhmmss", CultureInfo.CurrentCulture);

            try
            {
                FileInfo fileInfo = new FileInfo(filePath);
                DateTime lCreationTime = fileInfo.CreationTime;

                Console.WriteLine("[*] Get Handle ...");
                IntPtr hFile = CreateFile(filePath,
                         EFileAccess.FILE_WRITE_ATTRIBUTES,
                         EFileShare.None,
                         IntPtr.Zero,
                         ECreationDisposition.OpenExisting,
                         EFileAttributes.BackupSemantics,
                         IntPtr.Zero);
                Console.WriteLine("[*] File Handle: {0}", hFile);

                // 修改文件/文件夹的修改时间及访问时间
                //FixFileTime(filename, modified, NowTime);
                //FixDirTime(filename, modified, NowTime);
                SetFileTimes(hFile, lCreationTime, NowTime, modified);

                CloseHandle(hFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }

        /*
         * 在 C# 中，可直接使用 FileInfo/DirectoryInfo 修改文件/文件夹的属性
         
        /// <summary>
        /// Use FileInfo
        /// </summary>
        /// <param name="modified"></param>
        /// <param name="NowTime"></param>
        private static void FixFileTime(string filename, DateTime modified, DateTime NowTime)
        {

            FileInfo fileInfo = new FileInfo(filename)
            {
                LastWriteTime = modified,
                LastAccessTime = NowTime
            };
            Console.WriteLine("Fix:");
            Console.WriteLine(fileInfo.LastWriteTime);
            Console.WriteLine(fileInfo.LastAccessTime);
        }

        /// <summary>
        /// Use DirectoryInfo
        /// </summary>
        /// <param name="dirPaht"></param>
        /// <param name="modified"></param>
        /// <param name="NowTime"></param>
        private static void FixDirTime(string dirPaht, DateTime modified, DateTime NowTime)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(dirPaht)
            {
                LastWriteTime = modified,
                LastAccessTime = NowTime
            };
            Console.WriteLine("Fix:");
            Console.WriteLine(dirInfo.LastWriteTime);
            Console.WriteLine(dirInfo.LastAccessTime);
        }

        */
    }
}
