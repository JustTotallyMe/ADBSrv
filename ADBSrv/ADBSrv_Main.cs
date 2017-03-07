using PacketDotNet;
using SharpPcap;
using System;
using System.Net.NetworkInformation;
using System.ServiceProcess;
using ADBSrv_NS.Classes;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace ADBSrv_NS
{
    public partial class ADBSrv_Main : ServiceBase
    {
        private const int ReadTimeoutMilliseconds = 1000;
        ICaptureDevice device;
        Logging generalLog = new Logging($"{Properties.ADBSrv.Default.LoggingPath}\\GeneralLog.txt");
        Logging ARPLog = new Logging($"{Properties.ADBSrv.Default.LoggingPath}\\ARPLog.txt");
        List<PhysicalAddress> MACList = new List<PhysicalAddress>();
        List<DateTime> lastEventTimersList = new List<DateTime>();
        List<MethodFromDLL> methodCallerList = new List<MethodFromDLL>();
        Dictionary<PhysicalAddress, int> MACdict = new Dictionary<PhysicalAddress, int>();

        public ADBSrv_Main()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                int counter = 0;
                foreach (SettingsProperty item in Properties.ADBSrv.Default.Properties)
                {
                    if (item.Name.ToLower().StartsWith("dash"))
                    {
                        counter++;
                        if (Properties.ADBSrv.Default.DebugLogging)
                        {
                            generalLog.WriteErrorLog($"Found Property: {Properties.ADBSrv.Default[item.Name]}");
                        }
                    }
                }

                string[] orderArray = new string[counter];

                foreach (SettingsProperty item in Properties.ADBSrv.Default.Properties)
                {
                    if (item.Name.ToLower().StartsWith("dash"))
                    {
                        int dashButtonNumber = Convert.ToInt32(item.Name.Substring(item.Name.LastIndexOf('c') + 1));
                        if (Properties.ADBSrv.Default.DebugLogging)
                        {
                            generalLog.WriteErrorLog($"Adding {item.Name} to array");
                        }
                        orderArray[dashButtonNumber - 1] = item.Name;
                    }
                }

                foreach (var item in orderArray)
                {
                    if (Properties.ADBSrv.Default.DebugLogging)
                    {
                        generalLog.WriteErrorLog($"Current item is {item}");
                        generalLog.WriteErrorLog($"Adding {PhysicalAddress.Parse(Properties.ADBSrv.Default[item].ToString().Replace("-", "").Replace(":", "").ToUpper())} to MacList");
                    }
                    MACList.Add(PhysicalAddress.Parse(Properties.ADBSrv.Default[item].ToString().Replace("-", "").Replace(":", "").ToUpper()));
                }

                int loopCounter = MACList.Count;

                for (int i = 0; i < loopCounter; i++)
                {
                    MACdict.Add(MACList[i], i + 1);
                }

                for (int o = 0; o < loopCounter; o++)
                {
                    lastEventTimersList.Add(DateTime.Now);
                }

                Task.Factory.StartNew(() => { FillMethodCallList(loopCounter); });
                //FillMethodCallList(loopCounter);

                CaptureDeviceList devices = CaptureDeviceList.Instance;

                if (devices.Count < 1)
                {
                    generalLog.WriteErrorLog("No devices were found on this machine");
                    return;
                }

                if (Properties.ADBSrv.Default.DebugLogging)
                {
                    generalLog.WriteErrorLog("The following devices are available on this machine:");

                    for (int i = 0; i < devices.Count; i++)
                    {
                        ICaptureDevice dev = devices[i];
                        generalLog.WriteErrorLog($"{i}: {dev.Description}");
                    }
                }

                device = devices[Properties.ADBSrv.Default.InterfaceIndex];
                device.OnPacketArrival += device_OnPacketArrival;
                device.Open(DeviceMode.Promiscuous, ReadTimeoutMilliseconds);
                device.StartCapture();

                if (Properties.ADBSrv.Default.DebugLogging)
                {
                    generalLog.WriteErrorLog($"loopcounter {loopCounter}");
                    generalLog.WriteErrorLog($"lastEventTimerCount: {lastEventTimersList.Count}");
                }

                generalLog.WriteErrorLog($"-- Listening on {Properties.ADBSrv.Default.InterfaceIndex} --");
            }
            catch (Exception e)
            {
                generalLog.WriteErrorLog($"Message: {e.Message}");
                generalLog.WriteErrorLog("---------------");
                generalLog.WriteErrorLog(e.StackTrace);
                Environment.Exit(1337);
            }
        }

        public static string TimeSpanToString(TimeSpan ts)
        {
            return String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds,
            ts.Milliseconds / 10);
        }

        private void FillMethodCallList(int loopCounter)
        {
            for (int o = 0; o < loopCounter; o++)
            {
                methodCallerList.Add(new MethodFromDLL());
                methodCallerList[o].SetAllValues(o + 1);
            }
        }

        protected override void OnStop()
        {
            device.StopCapture();
            device.Close();
            generalLog.WriteErrorLog("Service was stoped");

            generalLog.WriteErrorLog("GC befor: " + GC.GetTotalMemory(false));
            this.Dispose();
            GC.Collect();
            generalLog.WriteErrorLog("GC after: " + GC.GetTotalMemory(false));
        }

        private void device_OnPacketArrival(object sender, CaptureEventArgs e)
        {
            try
            {
                var packet = Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);
                while (packet != null)
                {
                    var arpPacket = packet as ARPPacket;
                    if (arpPacket != null)
                    {
                        HandleArpPacket(arpPacket);
                    }

                    packet = packet.PayloadPacket;
                }
            }
            catch (Exception exce)
            {
                generalLog.WriteErrorLog(exce.Message);
                generalLog.WriteErrorLog("-------------------");
                generalLog.WriteErrorLog(exce.StackTrace);

                Environment.Exit(1337);
            }
        }

        private void HandleArpPacket(ARPPacket arpPacket)
        {
            if (Properties.ADBSrv.Default.DiscoveryMode)
            {
                ARPLog.WriteErrorLog(arpPacket.ToString());
                ARPLog.WriteErrorLog("-----------------------------------");
            }

            if (MACList.Contains(arpPacket.SenderHardwareAddress))
            {
                generalLog.WriteErrorLog("MAC addresses found: " + MACList.Count);
                generalLog.WriteErrorLog("Found MAC in list");

                foreach (var item in MACList)
                {
                    if (arpPacket.SenderHardwareAddress.Equals(item))
                    {
                        generalLog.WriteErrorLog($"IF DashButton {MACdict[item].ToString()}");

                        if (Properties.ADBSrv.Default.DebugLogging)
                        {
                            generalLog.WriteErrorLog("IF: " + item.ToString());
                        }

                        processButtonPress(MACdict[item]);
                        break;
                    }
                    else
                    {
                        if (Properties.ADBSrv.Default.DebugLogging)
                        {
                            generalLog.WriteErrorLog($"Else DashButton {MACdict[item].ToString()}");
                            generalLog.WriteErrorLog("Else: " + item.ToString());
                        }
                    }
                }
            }
        }

        void processButtonPress(int callerID)
        {
            if (Properties.ADBSrv.Default.DebugLogging)
            {
                ARPLog.WriteErrorLog(DateTime.Now + " Dash ARP");
            }

            if (isIntervalOver(callerID))
            {
                if (Properties.ADBSrv.Default.DebugLogging)
                {
                    generalLog.WriteErrorLog($"DashButton{callerID} event captured");
                }

                generalLog.WriteErrorLog($"Found {methodCallerList.Count} methodCall objects");
                Task.Factory.StartNew(() => { CallMethodFromMethodCaller(callerID); });
                //CallMethodFromMethodCaller(callerID);
            }
        }

        void CallMethodFromMethodCaller(int callerID)
        {
            methodCallerList[callerID -1].CallMethod();
        }

        bool isIntervalOver(int callerID)
        {
            if (Properties.ADBSrv.Default.DebugLogging)
            {
                generalLog.WriteErrorLog($"CallerID = {callerID}");
                generalLog.WriteErrorLog($"Found {lastEventTimersList.Count} lastEventTimers");
            }

            var now = DateTime.Now;
            if (now - Properties.ADBSrv.Default.DuplicateIgnoreInterval > lastEventTimersList[callerID - 1])
            {
                lastEventTimersList[callerID - 1] = now;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
