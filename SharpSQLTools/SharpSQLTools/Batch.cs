using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;

namespace SharpSQLTools
{
    class Batch
    {
        /// <summary>
        /// 查询输入的语句
        /// </summary>
        /// <param name="Conn">mssql 连接</param>
        /// <param name="query">查询语句</param>
        /// <param name="Flag">是否返回多行结果</param>
        /// <returns>查询结果</returns>
        public static string RemoteExec(SqlConnection Conn, String query, Boolean Flag)
        {
            String value = String.Empty;
            try
            {
                SqlCommand cmd = new SqlCommand("", Conn)
                {                    
                    CommandType = CommandType.Text,
                    CommandText = query
                };

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (Flag)
                        {
                            value += String.Format("{0}\r\n", reader[0].ToString());
                        }
                        else
                        {
                            value = reader[0].ToString();
                        }                        
                    }
                }
                return value;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[!] Error log: \r\n" + ex.Message);
            }
            return null;
        }
    }
}
