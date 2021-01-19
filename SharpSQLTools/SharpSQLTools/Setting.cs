using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace SharpSQLTools
{
    class Setting
    {
        private String Command = String.Empty;
        public SqlConnection Conn = null;
        public Setting(SqlConnection Connection)
        {
            Conn = Connection;
        }

        /// <summary>
        /// 判断文件是否存在
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="value">返回值匹配</param>
        /// <returns>存在则返回 true</returns>
        public bool File_Exists(String path)
        {
            Command = String.Format(@"
DECLARE @r INT
EXEC master.dbo.xp_fileexist '{0}', @r OUTPUT
SELECT @r as n", path);          
            if (int.Parse(Batch.RemoteExec(Conn, Command, false)) == 1)
                return true;
            return false;
        }

        
        /// <summary>
        /// 设置 configuration
        /// </summary>
        /// <param name="option">查询内容选项</param>
        /// <param name="value">返回值匹配</param>
        /// <returns></returns>
        public bool Set_configuration(String option, int value)
        {
            Command = String.Format("exec master.dbo.sp_configure '{0}',{1}; RECONFIGURE;", option, value);
            Batch.RemoteExec(Conn, Command, false);
            return Check_configuration(option, value);
        }

        /// <summary>
        /// 检查 configuration 的配置
        /// </summary>
        /// <param name="option">查询内容选项</param>
        /// <param name="value">返回值匹配</param>
        /// <returns></returns>
        public bool Check_configuration(String option, int value)
        {
            Command = String.Format("SELECT cast(value as INT) as v FROM sys.configurations where name = '{0}';", option);            
            if (int.Parse(Batch.RemoteExec(Conn, Command, false)) == value)
                return true;
            return false;
        }

        #region 启用/关闭 OLE Automation Procedures 配置
        /// <summary>
        /// 开启 OLA
        /// </summary>
        /// <returns>true/false</returns>
        public bool Enable_ola()
        {
            if (!Set_configuration("show advanced options", 1))
            {
                Console.WriteLine("[!] cannot enable 'show advanced options'");
                return false;
            }
            if (!Set_configuration("Ole Automation Procedures", 1))
            {
                Console.WriteLine("[!] cannot enable 'Ole Automation Procedures'");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 关闭 OLA
        /// </summary>
        /// <returns>true/false</returns>
        public bool Disable_ole()
        {
            if (!Set_configuration("show advanced options", 1))
            {
                Console.WriteLine("[!] cannot enable 'show advanced options'");
                return false;
            }
            if (!Set_configuration("Ole Automation Procedures", 0))
            {
                Console.WriteLine("[!] cannot disable 'Ole Automation Procedures'");
                return false;
            }
            if (!Set_configuration("show advanced options", 0))
            {
                Console.WriteLine("[!] cannot disable 'show advanced options'");
                return false;
            }
            return true;
        }

        #endregion


        #region 启用/关闭 xp_cmdshell
        /// <summary>
        /// 开启 xp_cmdshell
        /// </summary>
        /// <returns>true/false</returns>
        public bool Enable_xp_cmdshell()
        {
            if (!Set_configuration("show advanced options", 1))
            {
                Console.WriteLine("[!] cannot enable 'show advanced options'");
                return false;
            }
            if (!Set_configuration("xp_cmdshell", 1))
            {
                Console.WriteLine("[!] cannot enable 'xp_cmdshell'");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 关闭 xp_cmdshell
        /// </summary>
        /// <returns>true/false</returns>
        public bool Disable_xp_cmdshell()
        {
            if (!Set_configuration("show advanced options", 1))
            {
                Console.WriteLine("[!] cannot enable 'show advanced options'");
                return false;
            }
            if (!Set_configuration("xp_cmdshell", 0))
            {
                Console.WriteLine("[!] cannot disable 'xp_cmdshell'");
                return false;
            }
            if (!Set_configuration("show advanced options", 0))
            {
                Console.WriteLine("[!] cannot disable 'show advanced options'");
                return false;
            }
            return true;
        }

        #endregion

    }
}
