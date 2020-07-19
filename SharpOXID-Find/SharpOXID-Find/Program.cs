using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace SharpOXID_Find
{
    class Program
    {
        #region OXID 请求解析
        static byte[] buffer_v1 ={ /* Packet 431 */
            0x05, 0x00, 0x0b, 0x03, 0x10, 0x00, 0x00, 0x00,
            0x48, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
            0xb8, 0x10, 0xb8, 0x10, 0x00, 0x00, 0x00, 0x00,
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00,
            0xc4, 0xfe, 0xfc, 0x99, 0x60, 0x52, 0x1b, 0x10,
            0xbb, 0xcb, 0x00, 0xaa, 0x00, 0x21, 0x34, 0x7a,
            0x00, 0x00, 0x00, 0x00, 0x04, 0x5d, 0x88, 0x8a,
            0xeb, 0x1c, 0xc9, 0x11, 0x9f, 0xe8, 0x08, 0x00,
            0x2b, 0x10, 0x48, 0x60, 0x02, 0x00, 0x00, 0x00 };

        static byte[] buffer_v2 ={/* Packet 433 */
            0x05, 0x00, 0x00, 0x03, 0x10, 0x00, 0x00, 0x00,
            0x18, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05, 0x00 };
        #endregion

        #region byte[] 与 hex 的互转
        /// <summary>
        /// byte[]数组转16进制文件
        /// </summary>
        /// <param name="byteContents"></param>
        /// <returns></returns>
        private static String Byte2Hex(byte[] bytContents)
        {
            int length = bytContents.Length;
            StringBuilder builder = new StringBuilder(length * 3);
            foreach (byte value in bytContents)
            {
                builder.AppendFormat("{0:x} ", value);

            }
            return builder.ToString();
        }
 
        /// <summary>
        /// 16 进制转 byte[] 数组
        /// </summary>
        /// <param name="hexContent">16 进制字符串</param>
        /// <returns></returns>
        public static byte[] Hex2Byte(string hexContent)
        {
            string[] arry = hexContent.Split(' ');
            arry = arry.Skip(0).Take(arry.Length - 1).ToArray();
            List<byte> lstRet = new List<byte>();
            foreach (string s in arry)
            {
                lstRet.Add(Convert.ToByte(s, 16));
            }
            return lstRet.ToArray();
        }
        #endregion

        static void Main(string[] args)
        {
            string host = args[0];
            try 
            {
                Console.WriteLine("[*] Retrieving network interfaces of {0}", host);
                byte[] response_v0 = new byte[1024];
                using (var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    sock.Connect(host, 135);
                    sock.Send(buffer_v1);
                    sock.Receive(response_v0);
                    sock.Send(buffer_v2);
                    sock.Receive(response_v0);
                }

                String response_v1 = Byte2Hex(response_v0.Skip(40).ToArray());
                String response_v2 = response_v1.Substring(0, int.Parse(response_v1.IndexOf("9 0 ff ff 0").ToString()));
                String[] hostname_list = response_v2.Split(new string[] { "0 0 0 " }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < hostname_list.Length; i++)
                {
                    if (hostname_list[i].Length > 3)
                    {
                        Console.WriteLine("  [>] Address: " + Encoding.Default.GetString(Hex2Byte(hostname_list[i].Replace(" 0", "").Substring(2))));
                    }
                }
            } 
            catch (Exception ex)
            {
                Console.WriteLine("[!] Error: {0}", ex.Message);
            }
            
        }
    }
}
