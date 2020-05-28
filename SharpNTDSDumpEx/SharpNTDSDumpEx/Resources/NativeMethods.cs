/*
 * @Author: RcoIl
 * @Date: 2020/5/18 18:11:44
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpNTDSDumpEx.Resources
{
    public class NativeMethods
    {
        /// <summary>
        /// 打开指定的注册表项。键名不区分大小写
        /// </summary>
        /// <param name="hKey">打开的注册表项的句柄。</param>
        /// <param name="subKey">要打开的注册表子项的名称。</param>
        /// <param name="ulOptions"></param>
        /// <param name="samDesired">一个掩码，用于指定对要打开的密钥的所需访问权限。</param>
        /// <param name="hkResult">指向变量的指针，该变量接收打开的键的句柄</param>
        /// <returns></returns>
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        public static extern int RegOpenKeyEx(uint hKey, string subKey, int ulOptions, int samDesired, out IntPtr hkResult);
        /// <summary>
        /// 检索有关指定注册表项的信息
        /// </summary>
        /// <param name="hkey">打开的注册表项的句柄</param>
        /// <param name="lpClass">指向缓冲区的指针，该缓冲区接收用户定义的键类。此参数可以为NULL</param>
        /// <param name="lpcbClass"></param>
        /// <param name="lpReserved"></param>
        /// <param name="lpcSubKeys"></param>
        /// <param name="lpcbMaxSubKeyLen"></param>
        /// <param name="lpcbMaxClassLen"></param>
        /// <param name="lpcValues"></param>
        /// <param name="lpcbMaxValueNameLen"></param>
        /// <param name="lpcbMaxValueLen"></param>
        /// <param name="lpcbSecurityDescriptor"></param>
        /// <param name="lpftLastWriteTime"></param>
        /// <returns></returns>
        [DllImport("advapi32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        public static extern int RegQueryInfoKey(IntPtr hkey, StringBuilder lpClass, ref int lpcbClass, int lpReserved, out int lpcSubKeys, out int lpcbMaxSubKeyLen, out int lpcbMaxClassLen, out int lpcValues, out int lpcbMaxValueNameLen, out int lpcbMaxValueLen, out int lpcbSecurityDescriptor, IntPtr lpftLastWriteTime);
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern int RegCloseKey(IntPtr hKey);


        public const int CALGDES = 0x00006601;
        public const int CALGMD5 = 0x00008003;
        public const int CALGRC4 = 0x00006801;
        public const uint CRYPTVERIFYCONTEXT = 0xF0000000;
        public const int CURBLOBVERSION = 2;
        public const int PLAINTEXTKEYBLOB = 0x8;
        public const uint PROVRSAFULL = 1;

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptAcquireContext(
            ref IntPtr hProv,
            string pszContainer,
            string pszProvider,
            uint dwProvType,
            uint dwFlags);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptCreateHash(
            IntPtr hProv,
            uint algId,
            IntPtr hKey,
            uint dwFlags,
            ref IntPtr phHash);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptDecrypt(
            IntPtr hKey,
            IntPtr hHash,
            int final,
            uint dwFlags,
            byte[] pbData,
            ref uint pdwDataLen);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptDeriveKey(
            IntPtr hProv,
            int algid,
            IntPtr hBaseData,
            int flags,
            ref IntPtr phKey);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptDestroyHash(
            IntPtr hHash);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptDestroyKey(
            IntPtr phKey);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptEncrypt(
            IntPtr hKey,
            IntPtr hHash,
            int final,
            uint dwFlags,
            byte[] pbData,
            ref uint pdwDataLen,
            uint dwBufLen);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptHashData(
            IntPtr hHash,
            byte[] pbData,
            uint dataLen,
            uint flags);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptImportKey(
                    IntPtr hProv,
                    byte[] pbKeyData,
                    int dwDataLen,
                    IntPtr hPubKey,
                    int dwFlags,
                    ref IntPtr hKey);

        [DllImport("Advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptReleaseContext(
                    IntPtr hProv,
                    int dwFlags);

        [StructLayout(LayoutKind.Sequential)]
        public struct PUBLICKEYSTRUC
        {
            public byte BType;
            public byte BVersion;
            public short Reserved;
            public int AiKeyAlg;
        }
    }
}
