using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace SharpVssown
{
    class Program
    {
        // helper used to wrap long output
        public static IEnumerable<string> Split(string text, int partLength)
        {
            if (text == null) { throw new ArgumentNullException("singleLineString"); }

            if (partLength < 1) { throw new ArgumentException("'columns' must be greater than 0."); }

            var partCount = Math.Ceiling((double)text.Length / partLength);
            if (partCount < 2)
            {
                yield return text;
            }

            for (int i = 0; i < partCount; i++)
            {
                var index = i * partLength;
                var lengthLeft = Math.Min(partLength, text.Length - index);
                var line = text.Substring(index, lengthLeft);
                yield return line;
            }
        }

        static void listQuery(ManagementObject result)
        {
            Console.WriteLine("SHADNOW COPIES");
            Console.WriteLine("=============");
            Console.WriteLine();
            PropertyDataCollection props = result.Properties;
            foreach (PropertyData prop in props)
            {
                string propValue = String.Format("{0}", prop.Value);

                if (!String.IsNullOrEmpty(propValue) && (propValue.Length > 90))
                {
                    bool header = false;
                    foreach (string line in Split(propValue, 80))
                    {
                        if (!header)
                        {
                            Console.WriteLine(String.Format("{0,30} : {1}", prop.Name, line));
                        }
                        else
                        {
                            Console.WriteLine(String.Format("{0,30}   {1}", "", line));
                        }
                        header = true;
                    }
                }
                else
                {
                    Console.WriteLine(String.Format("{0,30} : {1}", prop.Name, prop.Value));
                }
            }
            Console.WriteLine();
        }

        /// <summary>
        /// StartService and StopService
        /// </summary>
        /// <param name="result"></param>
        /// <param name="Started"></param>
        static void Started(ManagementObject result, string Started)
        {
            ManagementScope scope = new ManagementScope(wmiNameSpace);
            ManagementPath path = new ManagementPath(String.Format("Win32_Service.Name='{0}'", result["Name"]));
            ManagementObject obj = new ManagementObject(scope, path, new ObjectGetOptions());
            ManagementBaseObject outParams = obj.InvokeMethod(Started, (ManagementBaseObject)null, null);
        }

        static void LocalWMIQuery(string wmiQuery, string flag)
        {
            ManagementObjectSearcher wmiData = null;
            try
            {
                wmiData = new ManagementObjectSearcher(wmiNameSpace, wmiQuery);
                foreach (ManagementObject result in wmiData.Get())
                {
                    if (flag == "list")
                    {
                        listQuery(result);
                    }
                    else if (flag == "status")
                    {
                        Console.WriteLine("[*] {0}", result["State"]);
                    }
                    else if (flag == "start" && result["Started"].Equals(false))
                    {
                        Started(result, "StartService");
                        Console.WriteLine("[*] Signal sent to start the " + result["Name"] + " service.");
                    }
                    else if (flag == "stop" && result["Started"].Equals(true))
                    {
                        Started(result, "StopService");
                        Console.WriteLine("[*] Signal sent to stop the " + result["Name"] + " service.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("  Exception : {0}", ex.Message));
            }
        }

        //http://www.windows-tech.info/13/73e33d211c023671.php
        static void createShadow(string Volume)
        {
            try
            {
                ManagementClass shadowCopy = new ManagementClass("Win32_ShadowCopy");
                ManagementBaseObject inParams = shadowCopy.GetMethodParameters("Create");

                //int numItems = inParams.Properties.Count;
                //Console.WriteLine("[*] Number of items: " + numItems);

                inParams.SetPropertyValue("Volume", Volume);
                inParams.SetPropertyValue("Context", "ClientAccessible");

                ManagementBaseObject outParams = shadowCopy.InvokeMethod("Create", inParams, null);
                if (outParams["ReturnValue"].ToString() == "0")
                {
                    Console.WriteLine("[*] Attempting to create a shadow copy. ShadowID: " + outParams["ShadowID"].ToString());
                }
            }
            catch (ManagementException err)
            {
                Console.WriteLine("An error occurred while trying to execute the WMI method: " + err.Message);
                Console.WriteLine(err.StackTrace);
            }
        }

        static void deleteShadow(string ShadowID)
        {
            // 没见到公开资料，也不研究了，直接使用powershell来删除吧。。
            string script = "gwmi win32_shadowcopy | where {$_.id -eq '"+ ShadowID +"'}|remove-wmiobject";
            try
            {
                Runspace MyRunspace = RunspaceFactory.CreateRunspace();
                MyRunspace.Open();
                Pipeline MyPipeline = MyRunspace.CreatePipeline();
                MyPipeline.Commands.AddScript(script);
                MyPipeline.Commands.Add("Out-String");
                Collection<PSObject> outputs = MyPipeline.Invoke();
                MyRunspace.Close();
                Console.WriteLine("[*] Attempting to delete shadow copy with ID: " + ShadowID);
            }
            catch (ManagementException err)
            {
                Console.WriteLine("An error occurred while trying to execute the WMI method: " + err.Message);
                Console.WriteLine(err.StackTrace);
            }
            //catch (Exception e) { Console.WriteLine(e.Message); }
        }

        static string wmiNameSpace = "root\\cimv2";

        static void Main(string[] args)
        {
            //demo();
            string flag = args[0];
            string wmiQuery, Volume, ShadowID = String.Empty;
            if (flag.Contains("list"))
            {
                wmiQuery = "Select * from Win32_ShadowCopy";
                LocalWMIQuery(wmiQuery, flag);
            }
            else
            {
                wmiQuery = "Select * from Win32_Service Where Name ='VSS'";
                LocalWMIQuery(wmiQuery, flag);
            }

            if (flag == "create")
            {
                Volume = args[1];
                createShadow(Volume);
            }
            else if (flag == "delete")
            {
                ShadowID = args[1];
                deleteShadow(ShadowID);
            }
        }
    }
}
