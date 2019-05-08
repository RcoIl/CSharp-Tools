using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;

namespace SharpChromeHistoryList
{
    class HistoryList
    {
        public static SQLiteDataReader ConnectionCommands(string path, string query)
        {
            // 创建连接字符串
            SQLiteConnection conn = new SQLiteConnection("data source=" + path);
            // 打开数据库
            conn.Open();
            SQLiteCommand cmd = new SQLiteCommand(query, conn);
            SQLiteDataReader reader = cmd.ExecuteReader();
            //conn.Close();
            return reader;
        }

        public static void ChromeHistory(string path)
        {

            //创建命令，查询浏览记录
            string query = "SELECT visit_count,title,url FROM urls ORDER BY visit_count desc";
            SQLiteDataReader reader = ConnectionCommands(path, query);

            string ChromeHistory = Environment.CurrentDirectory + "\\ChromeHistory.txt";
            if (File.Exists(ChromeHistory))
            {
                File.Delete(ChromeHistory);
            }
            FileStream fs = new FileStream(ChromeHistory, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);
            while (reader.Read())
            {
                string downloads_path_url = "[*] " + reader.GetInt64(0) + "\t" + reader.GetString(1) + "\t\t" + reader.GetString(2) + "\r\n";
                sw.Write(downloads_path_url);
            }
            sw.Close();
        }

        public static void ChromeDownloadList(string path)
        {
            //创建命令，查询下载记录
            string query = "SELECT downloads.target_path,downloads_url_chains.url FROM downloads,downloads_url_chains WHERE downloads.id=downloads_url_chains.id";
            SQLiteDataReader reader = ConnectionCommands(path, query);

            string ChromeDownloadList = Environment.CurrentDirectory + "\\ChromeDownloadList.txt";
            if (File.Exists(ChromeDownloadList))
            {
                File.Delete(ChromeDownloadList);

            }
            FileStream fs = new FileStream(ChromeDownloadList, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);
            while (reader.Read())
            {
                string downloads_path_url = "[*] " + reader.GetString(0) + "\t\t" + reader.GetString(1) + "\r\n";
                sw.Write(downloads_path_url);
            }
            sw.Close();

        }
    }
}
