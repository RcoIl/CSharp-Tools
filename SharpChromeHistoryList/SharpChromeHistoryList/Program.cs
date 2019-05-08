using System;

namespace SharpChromeHistoryList
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = Environment.CurrentDirectory + @"\\History";
            HistoryList.ChromeHistory(path);
            HistoryList.ChromeDownloadList(path);
        }
    }
}
