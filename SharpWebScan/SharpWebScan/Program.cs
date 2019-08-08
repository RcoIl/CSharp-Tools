using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace SharpWebScan
{
    class Program
    {
        /// <summary>
        /// ip处理，线程分配
        /// </summary>
        /// <param name="ip"></param>
        public static void ThreadList(string ip, string port)
        {
            Console.WriteLine("");
            try
            {
                ip = ip.Trim();
                string cip = "";
                if (regexAll(ip))
                {
                    cip = ip.Substring(0, ip.LastIndexOf('.'));
                }
                else if (regexAll_1(ip))
                {
                    cip = ip;
                }
                for (int i = 1; i < 255; i++)
                {
                    arrayList.Add(new threadStart(cip + "." + i.ToString(), port));
                }
                Thread[] array = new Thread[arrayList.Count];
                for (int j = 0; j < arrayList.Count; j++)
                {
                    array[j] = new Thread(new ThreadStart(((threadStart)arrayList[j]).method_0));
                    array[j].Start();
                }
                for (int j = 0; j < array.Length; j++)
                {
                    array[j].Join();
                }
                GC.Collect();
                arrayList.Clear();
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }
        }

        /// <summary>
        /// 输出 站点容器、标题信息
        /// </summary>
        /// <param name="ip"></param>
        public static void GetAll(string ip, string port)
        {
            
            string url = String.Format("http://{0}:{1}",ip , port);
            String regex = @"<title>.+</title>";
            try
            {
                var req = (HttpWebRequest)WebRequest.CreateDefault(new Uri(url));
                req.Method = "GET";
                req.Timeout = 10000;
                var res = (HttpWebResponse)req.GetResponse();
                if (res.StatusCode == HttpStatusCode.OK || res.StatusCode == HttpStatusCode.Forbidden || res.StatusCode == HttpStatusCode.Redirect || res.StatusCode == HttpStatusCode.MovedPermanently)
                {
                    int_0++;
                    try
                    {
                        WebClient web = new WebClient();
                        byte[] buffer = web.DownloadData(url);
                        string html = Encoding.UTF8.GetString(buffer);
                        String title = Regex.Match(html, regex).ToString();

                        title = Regex.Replace(title, @"<title>", "");
                        title = Regex.Replace(title, @"</title>", "");
                        Console.WriteLine("{0,-25} {1,-15} {2,-25} {3,-25}", url, Convert.ToInt32(res.StatusCode), res.Server, title);
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
            catch (WebException ex)
            {
            }
        }

        public static int int_0 = 0;
        public static ArrayList arrayList = new ArrayList();
        public static bool regexAll(string string_0)
        {
            Regex regex = new Regex("^(\\d{1,2}|1\\d\\d|2[0-4]\\d|25[0-5])\\.(\\d{1,2}|1\\d\\d|2[0-4]\\d|25[0-5])\\.(\\d{1,2}|1\\d\\d|2[0-4]\\d|25[0-5])\\.(\\d{1,2}|1\\d\\d|2[0-4]\\d|25[0-5])$");
            return regex.IsMatch(string_0);
        }
        public static bool regexAll_1(string string_0)
        {
            Regex regex = new Regex("^(\\d{1,2}|1\\d\\d|2[0-4]\\d|25[0-5])\\.(\\d{1,2}|1\\d\\d|2[0-4]\\d|25[0-5])\\.(\\d{1,2}|1\\d\\d|2[0-4]\\d|25[0-5])$");
            return regex.IsMatch(string_0);
        }
        
        static void Main(string[] args)
        {
            // 加入多端口扫描
            Console.WriteLine();
            Console.WriteLine("Scaning Web Title....");
            Console.WriteLine("{0,-25} {1,-15} {2,-25} {3,-25}", "URL", "StatusCode", "res.Server", "Title");
            string ports = args[2];
            string[] port = ports.Split(new char[] { ',' });
            for (int i = 0; i < port.Length; i++)
            {
                
                if (args.Contains("-CIP"))
                {
                    ThreadList(args[1], port[i]);
                }
                else if (args.Contains("-IP"))
                {
                    GetAll(args[1], port[i]);
                    
                }
            }
            Console.WriteLine("Count:" + int_0);
            Console.WriteLine("Finish!");
        }
    }
}
