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

        private static byte[] strToToHexByte(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if ((hexString.Length % 2) != 0)
                hexString += " ";
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;
        }

        static void Main(string[] args)
        {
            String host = args[0];
            String response = String.Empty;
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

                String[] response_v1 = BitConverter.ToString(response_v0.Skip(40).ToArray()).Replace("-", "").Split(new String[] { "0900FFFF00" }, StringSplitOptions.RemoveEmptyEntries);
                String[] response_v2 = response_v1[0].Split(new String[] { "0700" }, StringSplitOptions.RemoveEmptyEntries);
                String hostname = Encoding.Default.GetString(strToToHexByte(response_v2[0])).Replace("\0", "");

                response = String.Format("Retrieving network interfaces of {0}", host);
                response += String.Format("\n  [>] HostName: {0}", hostname);


                for (int i = 0; i < response_v2.Length; i++)
                {
                    if (response_v2[i].Length > 3)
                    {
                        response += String.Format("\n  [>] Address : {0}", Encoding.Default.GetString(strToToHexByte(response_v2[i])).Replace("\0", ""));
                    }
                }
                Console.WriteLine(response);
            } 
            catch (Exception ex)
            {
                Console.WriteLine("[!] Error: {0}", ex.Message);
            }
            
        }
    }
}
