using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;

namespace SharpChassisType
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("    [>] 此机器类型为: {0}",WMI.ChassisType());
        }
    }

    public static class WMI
    {
        public static ChassisTypes ChassisType()
        {
            ManagementClass systemEnclosures = new ManagementClass("Win32_SystemEnclosure");
            foreach (ManagementObject obj in systemEnclosures.GetInstances())
            {
                foreach (int i in (UInt16[])(obj["ChassisTypes"]))
                {
                    if (i > 0 && i < 25)
                    {
                        return (ChassisTypes)i;
                    }
                }
            }
            return ChassisTypes.Unknown;
        }
    }
    public enum ChassisTypes
    {
        Other = 1,
        Unknown,
        Desktop_桌面计算机,
        LowProfileDesktop_虚拟机,
        PizzaBox_披萨盒的机箱设计,
        MiniTower_迷你塔式机,
        Tower_塔式机,
        Portable_便携式计算机,
        Laptop_膝上型计算机,
        Notebook_笔记本,
        Handheld_手提式设备,
        DockingStation_插接站_扩展坞,
        AllInOne_一体机,
        SubNotebook_迷你型笔记本,
        SpaceSaving_节省空间型设备,
        LunchBox_盒型设备,
        MainSystemChassis_主_大型系统机架,
        ExpansionChassis_扩展机架,
        SubChassis_子机架,
        BusExpansionChassis_总线扩展机架,
        PeripheralChassis_外围机架,
        StorageChassis_机架式存储,
        RackMountChassis_机架式服务器,
        SealedCasePC_密封式计算机
    }
}
