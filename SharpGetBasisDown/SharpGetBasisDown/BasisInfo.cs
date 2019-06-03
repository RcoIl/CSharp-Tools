using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace SharpGetBasisDown
{
    class BasisInfo
    {

        public static void TxtWriter(string outlist, string FileName)
        {
            string Path = Program.CreateDirectory() + @"\" + FileName + ".txt";
            StreamWriter sw = new StreamWriter(Path, true, Encoding.UTF8);
            sw.WriteLine(outlist);
            sw.Flush();
            sw.Close();
        }

        public static void Command(string command)
        {
            ProcessStartInfo proccessStartInfo = new ProcessStartInfo("cmd.exe", " /c " + command);
            proccessStartInfo.CreateNoWindow = true;
            Process proc = new Process { StartInfo = proccessStartInfo };

            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;
            proc.Start();
        }

        public static void GetBasisInfo()
        {
            string[] commands = {
                "systeminfo",
                "netstat -anop tcp",
                "ipconfig /all",
                "tasklist /v",
                "set",
                "query user",
                "net share",
                "wmic startup list full",
                "wmic logicaldisk where drivetype=3 get name,freespace,systemname,filesystem,volumeserialnumber,size",
                "dir %WINDIR%\\Microsoft.NET\\Framework\\v*",
            };
            foreach (string command in commands)
            {
                string FileName = command.Replace("/", "").Replace("-", "").Replace("+", "").Replace("%", "").Replace(",", "").Replace("=", "").Replace("*", "").Replace("\\", "");
                ProcessStartInfo proccessStartInfo = new ProcessStartInfo("cmd.exe", " /c " + command);
                proccessStartInfo.CreateNoWindow = true;
                Process proc = new Process { StartInfo = proccessStartInfo };

                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();
                string outlist = proc.StandardOutput.ReadToEnd();
                TxtWriter(outlist, FileName);
                proc.WaitForExit();
                proc.Close();
            }
        }

        public static void GetProxyInformation()
        {

            IWebProxy wp = WebRequest.GetSystemWebProxy();
            string url = "https://www.google.com";
            Uri req = new Uri(url);
            Uri proxy = wp.GetProxy(req);
            if (String.Compare(req.AbsoluteUri, proxy.AbsoluteUri) != 0)
            {
                TxtWriter(proxy.AbsoluteUri, "网络代理情况");
            }
            else if (wp.Credentials != null)
            {
                NetworkCredential cred = wp.Credentials.GetCredential(req, "basic");
                string[] cerd = { cred.UserName, cred.Password, cred.Domain };
                foreach (string cers in cerd)
                {
                    TxtWriter(cers, "网络代理情况");
                }
                
            }

        }
        public static void Recent()
        {

            string userPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string recents = @"Microsoft\Windows\Recent";
            string recentsPath = Path.Combine(userPath, recents);
            DirectoryInfo di = new DirectoryInfo(recentsPath);
            foreach (var file in di.GetFiles())
            {
                TxtWriter(file.Name, "最近预览的文件");
            }
        }
        public static void AV_EDR()
        {
            string[] avproducts = { "Skynet", "CltAgent", "SkyMon", "Tanium", "360sp", "360RP", "360SD", "360Safe", "360leakfixer", "360rp", "360safe", "360sd", "360tray", "AAWTray", "ACAAS", "ACAEGMgr", "ACAIS", "AClntUsr", "ALERT", "ALERTSVC", "ALMon", "ALUNotify", "ALUpdate", "ALsvc", "AVENGINE", "AVGCHSVX", "AVGCSRVX", "AVGIDSAgent", "AVGIDSMonitor", "AVGIDSUI", "AVGIDSWatcher", "AVGNSX", "AVKProxy", "AVKService", "AVKTray", "AVKWCtl", "AVP", "AVP", "AVPDTAgt", "AcctMgr", "Ad-Aware", "Ad-Aware2007", "AddressExport", "AdminServer", "Administrator", "AeXAgentUIHost", "AeXNSAgent", "AeXNSRcvSvc", "AlertSvc", "AlogServ", "AluSchedulerSvc", "AnVir", "AppSvc32", "AtrsHost", "Auth8021x", "AvastSvc", "AvastUI", "Avconsol", "AvpM", "Avsynmgr", "Avtask", "BLACKD", "BWMeterConSvc", "CAAntiSpyware", "CALogDump", "CAPPActiveProtection", "CAPPActiveProtection", "CB", "CCAP", "CCenter", "CClaw", "CLPS", "CLPSLA", "CLPSLS", "CNTAoSMgr", "CPntSrv", "CTDataLoad", "CertificationManagerServiceNT", "ClShield", "ClamTray", "ClamWin", "Console", "CylanceUI", "DAO_Log", "DLService", "DLTray", "DLTray", "DRWAGNTD", "DRWAGNUI", "DRWEB32W", "DRWEBSCD", "DRWEBUPW", "DRWINST", "DSMain", "DWHWizrd", "DefWatch", "DolphinCharge", "EHttpSrv", "EMET_Agent", "EMET_Service", "EMLPROUI", "EMLPROXY", "EMLibUpdateAgentNT", "ETConsole3", "ETCorrel", "ETLogAnalyzer", "ETReporter", "ETRssFeeds", "EUQMonitor", "EndPointSecurity", "EngineServer", "EntityMain", "EtScheduler", "EtwControlPanel", "EventParser", "FAMEH32", "FCDBLog", "FCH32", "FPAVServer", "FProtTray", "FSCUIF", "FSHDLL32", "FSM32", "FSMA32", "FSMB32", "FWCfg", "FireSvc", "FireTray", "FirewallGUI", "ForceField", "FortiProxy", "FortiTray", "FortiWF", "FrameworkService", "FreeProxy", "GDFirewallTray", "GDFwSvc", "HWAPI", "ISNTSysMonitor", "ISSVC", "ISWMGR", "ITMRTSVC", "ITMRT_SupportDiagnostics", "ITMRT_TRACE", "IcePack", "IdsInst", "InoNmSrv", "InoRT", "InoRpc", "InoTask", "InoWeb", "IsntSmtp", "KABackReport", "KANMCMain", "KAVFS", "KAVStart", "KLNAGENT", "KMailMon", "KNUpdateMain", "KPFWSvc", "KSWebShield", "KVMonXP", "KVMonXP_2", "KVSrvXP", "KWSProd", "KWatch", "KavAdapterExe", "KeyPass", "KvXP", "LUALL", "LWDMServer", "LockApp", "LockAppHost", "LogGetor", "MCSHIELD", "MCUI32", "MSASCui", "ManagementAgentNT", "McAfeeDataBackup", "McEPOC", "McEPOCfg", "McNASvc", "McProxy", "McScript_InUse", "McWCE", "McWCECfg", "Mcshield", "Mctray", "MgntSvc", "MpCmdRun", "MpfAgent", "MpfSrv", "MsMpEng", "NAIlgpip", "NAVAPSVC", "NAVAPW32", "NCDaemon", "NIP", "NJeeves", "NLClient", "NMAGENT", "NOD32view", "NPFMSG", "NPROTECT", "NRMENCTB", "NSMdtr", "NTRtScan", "NVCOAS", "NVCSched", "NavShcom", "Navapsvc", "NaveCtrl", "NaveLog", "NaveSP", "Navw32", "Navwnt", "Nip", "Njeeves", "Npfmsg2", "Npfsvice", "NscTop", "Nvcoas", "Nvcsched", "Nymse", "OLFSNT40", "OMSLogManager", "ONLINENT", "ONLNSVC", "OfcPfwSvc", "PASystemTray", "PAVFNSVR", "PAVSRV51", "PNmSrv", "POPROXY", "POProxy", "PPClean", "PPCtlPriv", "PQIBrowser", "PSHost", "PSIMSVC", "PXEMTFTP", "PadFSvr", "Pagent", "Pagentwd", "PavBckPT", "PavFnSvr", "PavPrSrv", "PavProt", "PavReport", "Pavkre", "PcCtlCom", "PcScnSrv", "PccNTMon", "PccNTUpd", "PpPpWallRun", "PrintDevice", "ProUtil", "PsCtrlS", "PsImSvc", "PwdFiltHelp", "Qoeloader", "RAVMOND", "RAVXP", "RNReport", "RPCServ", "RSSensor", "RTVscan", "RapApp", "Rav", "RavAlert", "RavMon", "RavMonD", "RavService", "RavStub", "RavTask", "RavTray", "RavUpdate", "RavXP", "RealMon", "Realmon", "RedirSvc", "RegMech", "ReporterSvc", "RouterNT", "Rtvscan", "SAFeService", "SAService", "SAVAdminService", "SAVFMSESp", "SAVMain", "SAVScan", "SCANMSG", "SCANWSCS", "SCFManager", "SCFService", "SCFTray", "SDTrayApp", "SEVINST", "SMEX_ActiveUpdate", "SMEX_Master", "SMEX_RemoteConf", "SMEX_SystemWatch", "SMSECtrl", "SMSELog", "SMSESJM", "SMSESp", "SMSESrv", "SMSETask", "SMSEUI", "SNAC", "SNAC", "SNDMon", "SNDSrvc", "SPBBCSvc", "SPIDERML", "SPIDERNT", "SSM", "SSScheduler", "SVCharge", "SVDealer", "SVFrame", "SVTray", "SWNETSUP", "SavRoam", "SavService", "SavUI", "ScanMailOutLook", "SeAnalyzerTool", "SemSvc", "SescLU", "SetupGUIMngr", "SiteAdv", "Smc", "SmcGui", "SnHwSrv", "SnICheckAdm", "SnIcon", "SnSrv", "SnicheckSrv", "SpIDerAgent", "SpntSvc", "SpyEmergency", "SpyEmergencySrv", "StOPP", "StWatchDog", "SymCorpUI", "SymSPort", "TBMon", "TFGui", "TFService", "TFTray", "TFun", "TIASPN~1", "TSAnSrf", "TSAtiSy", "TScutyNT", "TSmpNT", "TmListen", "TmPfw", "Tmntsrv", "Traflnsp", "TrapTrackerMgr", "UPSCHD", "UcService", "UdaterUI", "UmxAgent", "UmxCfg", "UmxFwHlp", "UmxPol", "Up2date", "UpdaterUI", "UrlLstCk", "UserActivity", "UserAnalysis", "UsrPrmpt", "V3Medic", "V3Svc", "VPC32", "VPDN_LU", "VPTray", "VSStat", "VsStat", "VsTskMgr", "WEBPROXY", "WFXCTL32", "WFXMOD32", "WFXSNT40", "WebProxy", "WebScanX", "WinRoute", "WrSpySetup", "ZLH", "Zanda", "ZhuDongFangYu", "Zlh", "_avp32", "_avpcc", "_avpm", "aAvgApi", "aawservice", "acaif", "acctmgr", "ackwin32", "aclient", "adaware", "advxdwin", "aexnsagent", "aexsvc", "aexswdusr", "aflogvw", "afwServ", "agentsvr", "agentw", "ahnrpt", "ahnsd", "ahnsdsv", "alertsvc", "alevir", "alogserv", "alsvc", "alunotify", "aluschedulersvc", "amon9x", "amswmagt", "anti-trojan", "antiarp", "antivirus", "ants", "aphost", "apimonitor", "aplica32", "aps", "apvxdwin", "arr", "ashAvast", "ashBug", "ashChest", "ashCmd", "ashDisp", "ashEnhcd", "ashLogV", "ashMaiSv", "ashPopWz", "ashQuick", "ashServ", "ashSimp2", "ashSimpl", "ashSkPcc", "ashSkPck", "ashUpd", "ashWebSv", "ashdisp", "ashmaisv", "ashserv", "ashwebsv", "asupport", "aswDisp", "aswRegSvr", "aswServ", "aswUpdSv", "aswUpdsv", "aswWebSv", "aswupdsv", "atcon", "atguard", "atro55en", "atupdater", "atwatch", "atwsctsk", "au", "aupdate", "aupdrun", "aus", "auto-protect.nav80try", "autodown", "autotrace", "autoup", "autoupdate", "avEngine", "avadmin", "avcenter", "avconfig", "avconsol", "ave32", "avengine", "avesvc", "avfwsvc", "avgam", "avgamsvr", "avgas", "avgcc", "avgcc32", "avgcsrvx", "avgctrl", "avgdiag", "avgemc", "avgfws8", "avgfws9", "avgfwsrv", "avginet", "avgmsvr", "avgnsx", "avgnt", "avgregcl", "avgrssvc", "avgrsx", "avgscanx", "avgserv", "avgserv9", "avgsystx", "avgtray", "avguard", "avgui", "avgupd", "avgupdln", "avgupsvc", "avgvv", "avgw", "avgwb", "avgwdsvc", "avgwizfw", "avkpop", "avkserv", "avkservice", "avkwctl9", "avltmain", "avmailc", "avmcdlg", "avnotify", "avnt", "avp", "avp32", "avpcc", "avpdos32", "avpexec", "avpm", "avpncc", "avps", "avptc32", "avpupd", "avscan", "avsched32", "avserver", "avshadow", "avsynmgr", "avwebgrd", "avwin", "avwin95", "avwinnt", "avwupd", "avwupd32", "avwupsrv", "avxmonitor9x", "avxmonitornt", "avxquar", "backweb", "bargains", "basfipm", "bd_professional", "bdagent", "bdc", "bdlite", "bdmcon", "bdss", "bdsubmit", "beagle", "belt", "bidef", "bidserver", "bipcp", "bipcpevalsetup", "bisp", "blackd", "blackice", "blink", "blss", "bmrt", "bootconf", "bootwarn", "borg2", "bpc", "bpk", "brasil", "bs120", "bundle", "bvt", "bwgo0000", "ca", "caav", "caavcmdscan", "caavguiscan", "caf", "cafw", "caissdt", "capfaem", "capfasem", "capfsem", "capmuamagt", "casc", "casecuritycenter", "caunst", "cavrep", "cavrid", "cavscan", "cavtray", "ccApp", "ccEvtMgr", "ccLgView", "ccProxy", "ccSetMgr", "ccSetmgr", "ccSvcHst", "ccap", "ccapp", "ccevtmgr", "cclaw", "ccnfagent", "ccprovsp", "ccproxy", "ccpxysvc", "ccschedulersvc", "ccsetmgr", "ccsmagtd", "ccsvchst", "ccsystemreport", "cctray", "ccupdate", "cdp", "cfd", "cfftplugin", "cfgwiz", "cfiadmin", "cfiaudit", "cfinet", "cfinet32", "cfnotsrvd", "cfp", "cfpconfg", "cfpconfig", "cfplogvw", "cfpsbmit", "cfpupdat", "cfsmsmd", "checkup", "cka", "clamscan", "claw95", "claw95cf", "clean", "cleaner", "cleaner3", "cleanpc", "cleanup", "click", "cmdagent", "cmdinstall", "cmesys", "cmgrdian", "cmon016", "comHost", "connectionmonitor", "control_panel", "cpd", "cpdclnt", "cpf", "cpf9x206", "cpfnt206", "crashrep", "csacontrol", "csinject", "csinsm32", "csinsmnt", "csrss_tc", "ctrl", "cv", "cwnb181", "cwntdwmo", "cz", "datemanager", "dbserv", "dbsrv9", "dcomx", "defalert", "defscangui", "defwatch", "deloeminfs", "deputy", "diskmon", "divx", "djsnetcn", "dllcache", "dllreg", "doors", "doscan", "dpf", "dpfsetup", "dpps2", "drwagntd", "drwatson", "drweb", "drweb32", "drweb32w", "drweb386", "drwebcgp", "drwebcom", "drwebdc", "drwebmng", "drwebscd", "drwebupw", "drwebwcl", "drwebwin", "drwupgrade", "dsmain", "dssagent", "dvp95", "dvp95_0", "dwengine", "dwhwizrd", "dwwin", "ecengine", "edisk", "efpeadm", "egui", "ekrn", "elogsvc", "emet_agent", "emet_service", "emsw", "engineserver", "ent", "era", "esafe", "escanhnt", "escanv95", "esecagntservice", "esecservice", "esmagent", "espwatch", "etagent", "ethereal", "etrustcipe", "evpn", "evtProcessEcFile", "evtarmgr", "evtmgr", "exantivirus-cnet", "exe.avxw", "execstat", "expert", "explore", "f-agnt95", "f-prot", "f-prot95", "f-stopw", "fameh32", "fast", "fch32", "fih32", "findviru", "firesvc", "firetray", "firewall", "fmon", "fnrb32", "fortifw", "fp-win", "fp-win_trial", "fprot", "frameworkservice", "frminst", "frw", "fsaa", "fsaua", "fsav", "fsav32", "fsav530stbyb", "fsav530wtbyb", "fsav95", "fsavgui", "fscuif", "fsdfwd", "fsgk32", "fsgk32st", "fsguidll", "fsguiexe", "fshdll32", "fsm32", "fsma32", "fsmb32", "fsorsp", "fspc", "fspex", "fsqh", "fssm32", "fwinst", "gator", "gbmenu", "gbpoll", "gcascleaner", "gcasdtserv", "gcasinstallhelper", "gcasnotice", "gcasserv", "gcasservalert", "gcasswupdater", "generics", "gfireporterservice", "ghost_2", "ghosttray", "giantantispywaremain", "giantantispywareupdater", "gmt", "guard", "guarddog", "guardgui", "hacktracersetup", "hbinst", "hbsrv", "hipsvc", "hotactio", "hotpatch", "htlog", "htpatch", "hwpe", "hxdl", "hxiul", "iamapp", "iamserv", "iamstats", "ibmasn", "ibmavsp", "icepack", "icload95", "icloadnt", "icmon", "icsupp95", "icsuppnt", "idle", "iedll", "iedriver", "iface", "ifw2000", "igateway", "inetlnfo", "infus", "infwin", "inicio", "init", "inonmsrv", "inorpc", "inort", "inotask", "intdel", "intren", "iomon98", "isPwdSvc", "isUAC", "isafe", "isafinst", "issvc", "istsvc", "jammer", "jdbgmrg", "jedi", "kaccore", "kansgui", "kansvr", "kastray", "kav", "kav32", "kavfs", "kavfsgt", "kavfsrcn", "kavfsscs", "kavfswp", "kavisarv", "kavlite40eng", "kavlotsingleton", "kavmm", "kavpers40eng", "kavpf", "kavshell", "kavss", "kavstart", "kavsvc", "kavtray", "kazza", "keenvalue", "kerio-pf-213-en-win", "kerio-wrl-421-en-win", "kerio-wrp-421-en-win", "kernel32", "killprocesssetup161", "kis", "kislive", "kissvc", "klnacserver", "klnagent", "klserver", "klswd", "klwtblfs", "kmailmon", "knownsvr", "kpf4gui", "kpf4ss", "kpfw32", "kpfwsvc", "krbcc32s", "kvdetech", "kvolself", "kvsrvxp", "kvsrvxp_1", "kwatch", "kwsprod", "kxeserv", "launcher", "ldnetmon", "ldpro", "ldpromenu", "ldscan", "leventmgr", "livesrv", "lmon", "lnetinfo", "loader", "localnet", "lockdown", "lockdown2000", "log_qtine", "lookout", "lordpe", "lsetup", "luall", "luau", "lucallbackproxy", "lucoms", "lucomserver", "lucoms~1", "luinit", "luspt", "makereport", "mantispm", "mapisvc32", "masalert", "massrv", "mcafeefire", "mcagent", "mcappins", "mcconsol", "mcdash", "mcdetect", "mcepoc", "mcepocfg", "mcinfo", "mcmnhdlr", "mcmscsvc", "mcods", "mcpalmcfg", "mcpromgr", "mcregwiz", "mcscript", "mcscript_inuse", "mcshell", "mcshield", "mcshld9x", "mcsysmon", "mctool", "mctray", "mctskshd", "mcuimgr", "mcupdate", "mcupdmgr", "mcvsftsn", "mcvsrte", "mcvsshld", "mcwce", "mcwcecfg", "md", "mfeann", "mfevtps", "mfin32", "mfw2en", "mfweng3.02d30", "mgavrtcl", "mgavrte", "mghtml", "mgui", "minilog", "mmod", "monitor", "monsvcnt", "monsysnt", "moolive", "mostat", "mpcmdrun", "mpf", "mpfagent", "mpfconsole", "mpfservice", "mpftray", "mps", "mpsevh", "mpsvc", "mrf", "mrflux", "msapp", "msascui", "msbb", "msblast", "mscache", "msccn32", "mscifapp", "mscman", "msconfig", "msdm", "msdos", "msiexec16", "mskagent", "mskdetct", "msksrver", "msksrvr", "mslaugh", "msmgt", "msmpeng", "msmsgri32", "msscli", "msseces", "mssmmc32", "msssrv", "mssys", "msvxd", "mu0311ad", "mwatch", "myagttry", "n32scanw", "nSMDemf", "nSMDmon", "nSMDreal", "nSMDsch", "naPrdMgr", "nav", "navap.navapsvc", "navapsvc", "navapw32", "navdx", "navlu32", "navnt", "navstub", "navw32", "navwnt", "nc2000", "ncinst4", "LiveUpdate360", "SoftManagerLite", "360js", "coreFrameworkHost", "coreServiceShell", "AMSP_LogServer", "uiSeAgnt", "uiWatchDog", "fshoster32", "fshoster32,", "pavsrvx86", "TPSrvWow", "ApVxdWin", "Iface", "psksvc", "SrvLoad", "kxescore", "kxetray", "KSafeSvc", "KSafeTray", "KVXp", "vsserv", "downloader", "drwupsrv", "dwnetfilter", "dwservice", "frwl_notify", "frwl_svc", "spideragent", "RsTray", "popwndexe", "RsMgrSvc", "McSvHost", "McUICnt", "MOBKbackup", "mcsacore", "mfefire", "McAPExe", "avgcsrva", "avgcsrva", "avgcfgex", "avgemca", "avgidsagent", "avgnsa", "avgrsa", "MPSVC", "MPSVC1", "MPSVC2", "MPMon", "twssrv", "twister", "AVKWCtlX64", "GDFwSvcx64", "GDScan" };
            Process[] proces = Process.GetProcesses(Environment.MachineName);

            for (int i = 0; i < proces.Length; i++)
            {
                for (int a = 0; a < avproducts.Length; a++)
                {
                    string processSearch = avproducts[a];
                    if (proces[i].ProcessName.Equals(processSearch))
                    {
                        TxtWriter(proces[i].ProcessName, "杀软进程列表");
                    }
                }
            }

            string[] edrproducts = { "CiscoAMPCEFWDriver.sys", "CiscoAMPHeurDriver.sys", "cbstream.sys", "cbk7.sys", "Parity.sys", "libwamf.sys", "LRAgentMF.sys", "BrCow_x_x_x_x.sys", "brfilter.sys", "BDSandBox.sys", "TRUFOS.SYS", "AVC3.SYS", "Atc.sys", "AVCKF.SYS", "bddevflt.sys", "gzflt.sys", "bdsvm.sys", "hbflt.sys", "cve.sys", "psepfilter.sys", "cposfw.sys", "dsfa.sys", "medlpflt.sys", "epregflt.sys", "TmFileEncDmk.sys", "tmevtmgr.sys", "TmEsFlt.sys", "fileflt.sys", "SakMFile.sys", "SakFile.sys", "AcDriver.sys", "TMUMH.sys", "hfileflt.sys", "TMUMS.sys", "MfeEEFF.sys", "mfprom.sys", "hdlpflt.sys", "swin.sys", "mfehidk.sys", "mfencoas.sys", "epdrv.sys", "carbonblackk.sys", "csacentr.sys", "csaenh.sys", "csareg.sys", "csascr.sys", "csaav.sys", "csaam.sys", "esensor.sys", "fsgk.sys", "fsatp.sys", "fshs.sys", "eaw.sys", "im.sys", "csagent.sys", "rvsavd.sys", "dgdmk.sys", "atrsdfw.sys", "mbamwatchdog.sys", "edevmon.sys", "SentinelMonitor.sys", "edrsensor.sys", "ehdrv.sys", "HexisFSMonitor.sys", "CyOptics.sys", "CarbonBlackK.sys", "CyProtectDrv32.sys", "CyProtectDrv64.sys", "CRExecPrev.sys", "ssfmonm.sys", "CybKernelTracker.sys", "SAVOnAccess.sys", "savonaccess.sys", "sld.sys", "aswSP.sys", "FeKern.sys", "klifks.sys", "klifaa.sys", "Klifsm.sys", "mfeaskm.sys", "mfencfilter.sys", "WFP_MRT.sys", "groundling32.sys", "SAFE-Agent.sys", "groundling64.sys", "avgtpx86.sys", "avgtpx64.sys", "pgpwdefs.sys", "GEProtection.sys", "diflt.sys", "sysMon.sys", "ssrfsf.sys", "emxdrv2.sys", "reghook.sys", "spbbcdrv.sys", "bhdrvx86.sys", "bhdrvx64.sys", "SISIPSFileFilter.sys", "symevent.sys", "VirtualAgent.sys", "vxfsrep.sys", "VirtFile.sys", "SymAFR.sys", "symefasi.sys", "symefa.sys", "symefa64.sys", "SymHsm.sys", "evmf.sys", "GEFCMP.sys", "VFSEnc.sys", "pgpfs.sys", "fencry.sys", "symrg.sys", "cfrmd.sys", "cmdccav.sys", "cmdguard.sys", "CmdMnEfs.sys", "MyDLPMF.sys", "PSINPROC.SYS", "PSINFILE.SYS", "amfsm.sys", "amm8660.sys", "amm6460.sys" };
            string edrPath = @"C:\Windows\System32\drivers\";
            for (int e = 0; e < edrproducts.Length; e++)
            {
                if (File.Exists(edrPath + edrproducts[e]))
                {
                    TxtWriter(edrproducts[e], "EDR 产品列表");
                }
            }
        }

        public static void GetInstalledApplications()
        {

            string basekey = "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
            RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(basekey);
            if (registryKey != null)
            {
                foreach (string rname in registryKey.GetSubKeyNames())
                {
                    RegistryKey installedapp = registryKey.OpenSubKey(rname);
                    if (installedapp != null)
                    {
                        string displayname = (installedapp.GetValue("DisplayName") != null) ? installedapp.GetValue("DisplayName").ToString() : "";
                        string displayversion = (installedapp.GetValue("DisplayVersion") != null) ? installedapp.GetValue("DisplayVersion").ToString() : "";
                        string helplink = (installedapp.GetValue("HelpLink") != null) ? installedapp.GetValue("HelpLink").ToString() : "";

                        if (!(Regex.IsMatch(displayname, "^(Service Pack \\d+|(Definition\\s|Security\\s)?Update) for") && Regex.IsMatch(helplink, "support\\.microsoft")) && displayname != "")
                        {
                            if (displayversion != "")
                            {
                                string displaynameversion = displayname + " (" + displayversion + ")";
                                TxtWriter(displayname, "查找安装程序及版本");
                            }
                            else
                            {
                                TxtWriter(displayname, "查找安装程序及版本");
                            }
                        }                 
                    }
                }
            }
            basekey = "Software\\Microsoft\\Installer\\Products";
            registryKey = Registry.CurrentUser.OpenSubKey(basekey);
            if (registryKey != null)
            {
                foreach (string rname in registryKey.GetSubKeyNames())
                {
                    RegistryKey installedapp = registryKey.OpenSubKey(rname);
                    if (installedapp != null)
                    {
                        string displayname = (installedapp.GetValue("ProductName") != null) ? installedapp.GetValue("ProductName").ToString() : "";
                        if (displayname != "")
                            TxtWriter(displayname, "查找安装程序及版本");
                    }
                }
            }
        }

        public static void BasisInfos()
        {
            GetBasisInfo();
            GetProxyInformation();
            Recent();
            AV_EDR();
            GetInstalledApplications();
            SavedRDPConnections.ListSavedRDPConnections();
        }
    }
}
