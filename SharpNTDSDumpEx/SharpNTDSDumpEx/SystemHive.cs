/*
 * @Author: RcoIl
 * @Date: 2020/5/18 18:14:44
*/

using SharpNTDSDumpEx.Resources;
using System;
using System.Text;


namespace SharpNTDSDumpEx
{
    class SystemHive
    {
        private static readonly int[] SYSTEMKEYTRANSFORMS = { 0x8, 0x5, 0x4, 0x2, 0xb, 0x9, 0xd, 0x3, 0x0, 0x6, 0x1, 0xc, 0xe, 0xa, 0xf, 0x7 };

        /// <summary>
        /// 将字符转16进制字节数组
        /// </summary>
        /// <param name="hexkey"></param>
        /// <param name="systemkey"></param>
        /// <returns></returns>
        public static Boolean ParseSystemKey(String hexkey, out byte[] systemkey)
        {
            systemkey = new byte[16];
            var syskey = new byte[16];
            if (hexkey.Length != 32)
            {
                Console.WriteLine("[!] SYSKEY must be a hex-string of 32 characters.");
                return false;
            }
            
            for (int i = 0; i < 16; i++)
            {
                syskey[i] = Convert.ToByte(hexkey.Substring(i * 2, 2), 16);
            }
            systemkey = syskey;
            return true;
        }

        /// <summary>
        /// /// 获取当前机器的 system 中的 key 字符串（从 SYSTEM 注册表配置单元中提取系统密钥）。
        /// 此处引用代码：http://www.zcgonvh.com/post/ntds_dit_pwd_dumper.html
        /// 如果需要离线解析注册表，请参考：Registry 项目。 
        /// </summary>
        /// <param name="HivemKey">syskey</param>
        public static Boolean ReadSystemKey(out byte[] systemkey)
        {
            string[] keys = { "JD", "Skew1", "GBG", "Data" };
            String key = String.Empty;

            foreach (string subKey in keys)
            {
                IntPtr hkResult = IntPtr.Zero;
                // HKEY_LOCAL_MACHINE = 0x80000002
                NativeMethods.RegOpenKeyEx(0x80000002, @"SYSTEM\CurrentControlSet\Control\Lsa\" + subKey, 0, 0x19, out hkResult);
                StringBuilder sbuilder = new StringBuilder();
                int len = 64;
                NativeMethods.RegQueryInfoKey(hkResult,
                    sbuilder,
                    ref len,
                    0,
                    out len,
                    out len,
                    out len,
                    out len,
                    out len,
                    out len,
                    out len,
                    IntPtr.Zero);
                key += sbuilder.ToString();
                NativeMethods.RegCloseKey(hkResult);
            }

            byte[] b = new byte[16];
            systemkey = new byte[16];
            
            for (int i = 0; i < 16; i++)
            {
                b[i] = Convert.ToByte(key.Substring(i * 2, 2), 16);
            }
            for (int i = 0; i < 16; i++)
            {
                systemkey[i] = b[SYSTEMKEYTRANSFORMS[i]];
            }

            return true;
        }
    }
}
