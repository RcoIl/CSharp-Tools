using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;

namespace SharpDomainSpray
{
    public class DomainUserList
    {
        public static List<string> Users()
        {
            List<string> UserList = new List<string>();

            try
            {
                //DirectoryEntry DirEntry = new DirectoryEntry("LDAP://" + System.DirectoryServices.ActiveDirectory.ActiveDirectorySite.GetComputerSite().InterSiteTopologyGenerator.Name);
                DirectoryEntry DirEntry = new DirectoryEntry("LDAP://" + Domain.GetCurrentDomain());
                DirectorySearcher UserSearcher = new DirectorySearcher(DirEntry);
                UserSearcher.Filter = "(&(objectCategory=Person)(sAMAccountName=*)(!userAccountControl:1.2.840.113556.1.4.803:=16)(!(userAccountControl:1.2.840.113556.1.4.803:=2)))";
                UserSearcher.PageSize = 1000;
                UserSearcher.PropertiesToLoad.Add("sAMAccountName");
                UserSearcher.SearchScope = SearchScope.Subtree;
                SearchResultCollection results = UserSearcher.FindAll();
                if (results != null)
                {
                    for (var i = 0; i < results.Count; i++)
                    {
                        UserList.Add((string)results[i].Properties["sAMAccountName"][0]);
                    }
                }
                else
                {
                    Console.WriteLine("[-] 无法从 AD 域中检索用户名");
                    Environment.Exit(1);
                }
            }
            catch
            {
                Console.WriteLine("[-] 未找到域或无法连接到域");
                Environment.Exit(1);
            }
            return UserList;
        }
    }
}
