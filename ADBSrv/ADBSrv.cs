using PacketDotNet;
using SharpPcap;
using System;
using System.Net.NetworkInformation;
using System.ServiceProcess;
using ADBSrv_NS.Classes;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using XMLhelper;
using System.Diagnostics;
using System.IO;

namespace ADBSrv_NS
{
    public partial class ADBSrv : ServiceBase
    {
        private const int ReadTimeoutMilliseconds = 1000;
        XMLReader _configReader = new XMLReader(false, $"{AppDomain.CurrentDomain.BaseDirectory}config.xml");
        ICaptureDevice device;
        Logging generalLog;
        Logging ARPLog;
        List<PhysicalAddress> MACList = new List<PhysicalAddress>();
        List<DateTime> lastEventTimersList = new List<DateTime>();
        List<MethodFromDLL> methodCallerList = new List<MethodFromDLL>();
        Dictionary<PhysicalAddress, int> MACdict = new Dictionary<PhysicalAddress, int>();
        Dictionary<string, string> configMisc = new Dictionary<string, string>();
        Dictionary<string, Dictionary<string, string>> buttonDict = new Dictionary<string, Dictionary<string, string>>();

        configReader newReaderHelper;

        public ADBSrv()
        {
            InitializeComponent();

            try
            {
                generalLog = new Logging($"{_configReader.returnSingleNodeEntry("config/misc/loggingPath")}\\GeneralLog.txt");
                ARPLog = new Logging($"{_configReader.returnSingleNodeEntry("config/misc/loggingPath")}\\ARPLog.txt");
                newReaderHelper = new configReader(_configReader);
            }
            catch (Exception e)
            {
                StreamWriter sw = new StreamWriter(@"C:\temp\test.txt", true);
                sw.WriteLine(e.Message);
                sw.WriteLine(e.StackTrace);
                sw.Flush();
                sw.Close();
                Environment.Exit(1337);
            }
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                readConfigMiscToDict();
                readConfigButtonsToDict();

                for (int i = 0; i < buttonDict.Keys.Count; i++)
                {
                    //generalLog.WriteErrorLog($"Button ID = {i}");
                    
                    string tempMac = buttonDict[$"button{i + 1}"]["mac"];

                    //generalLog.WriteErrorLog($"Curent MAC = {tempMac}");

                    MACList.Add(PhysicalAddress.Parse(tempMac.ToString().Replace("-", "").Replace(":", "").ToUpper()));
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

                if (Convert.ToBoolean(configMisc["debugLogging"]))
                {
                    generalLog.WriteErrorLog("The following devices are available on this machine:");

                    for (int i = 0; i < devices.Count; i++)
                    {
                        ICaptureDevice dev = devices[i];
                        generalLog.WriteErrorLog($"{i}: {dev.Description}");
                    }
                }

                device = devices[Convert.ToInt32(configMisc["interfaceIndex"])];
                device.OnPacketArrival += device_OnPacketArrival;
                device.Open(DeviceMode.Promiscuous, ReadTimeoutMilliseconds);
                device.StartCapture();

                if (Convert.ToBoolean(configMisc["debugLogging"]))
                {
                    generalLog.WriteErrorLog($"loopcounter {loopCounter}");
                    generalLog.WriteErrorLog($"lastEventTimerCount: {lastEventTimersList.Count}");
                }

                generalLog.WriteErrorLog($"-- Listening on {configMisc["interfaceIndex"]} --");
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
                methodCallerList[o].SetAllValues(o + 1, configMisc["loggingPath"]);
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
            //eventLog1.WriteEntry("Server stopped");
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
            catch (Exception E)
            {
                generalLog.WriteErrorLog(E.Message);
                generalLog.WriteErrorLog("-------------------");
                generalLog.WriteErrorLog(E.StackTrace);

                Environment.Exit(1337);
            }
        }

        private void HandleArpPacket(ARPPacket arpPacket)
        {
            if (Convert.ToBoolean(configMisc["discoveryMode"]))
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
                        if (Convert.ToBoolean(configMisc["debugLogging"]))
                        {
                            generalLog.WriteErrorLog($"IF DashButton {MACdict[item].ToString()}");
                            generalLog.WriteErrorLog("IF: " + item.ToString());
                        }

                        processButtonPress(MACdict[item]);
                        break;
                    }
                    else
                    {
                        if (Convert.ToBoolean(configMisc["debugLogging"]))
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
            if (Convert.ToBoolean(configMisc["debugLogging"]))
            {
                ARPLog.WriteErrorLog(DateTime.Now + " Dash ARP");
            }

            if (isIntervalOver(callerID))
            {
                if (Convert.ToBoolean(configMisc["debugLogging"]))
                {
                    generalLog.WriteErrorLog($"DashButton {buttonDict[$"button{callerID}"]["name"]} event captured");
                }

                //generalLog.WriteErrorLog($"Found {methodCallerList.Count} methodCall objects");

                if (buttonDict[$"button{callerID}"]["overloadValue"] == "0")
                {
                    Task.Factory.StartNew(() => { CallMethodFromMethodCaller(callerID); });
                    //CallMethodFromMethodCaller(callerID); 
                }
                else
                {
                    Task.Factory.StartNew(() => { CallMethodFromMethodCaller(callerID, buttonDict[$"button{callerID}"]["overloadValue"]); });
                    //CallMethodFromMethodCaller(callerID, buttonDict[$"button{callerID}"]["overloadValue"]);
                }
            }
            else
            {
                if (Convert.ToBoolean(configMisc["debugLogging"]))
                {
                    generalLog.WriteErrorLog($"Interval for \"{buttonDict[$"button{callerID}"]["name"]}\" not ready yet");
                }
            }
        }

        void CallMethodFromMethodCaller(int callerID)
        {
            methodCallerList[callerID -1].CallMethod();
        }

        void CallMethodFromMethodCaller(int callerID, string value)
        {
            methodCallerList[callerID - 1].CallMethod(value);
        }

        bool isIntervalOver(int callerID)
        {
            TimeSpan duplicateIgnoreInterval = TimeSpan.Parse(configMisc["duplicateIgnoreInterval"]);

            if (Convert.ToBoolean(configMisc["debugLogging"]))
            {
                generalLog.WriteErrorLog($"CallerID = {callerID}");
                generalLog.WriteErrorLog($"Found {lastEventTimersList.Count} lastEventTimers");
            }

            var now = DateTime.Now;
            if (now - duplicateIgnoreInterval > lastEventTimersList[callerID - 1])
            {
                lastEventTimersList[callerID - 1] = now;
                return true;
            }
            else
            {
                return false;
            }
        }

        void readConfigMiscToDict()
        {
            configMisc = newReaderHelper.GetMiscFromConfig();
        }

        void readConfigButtonsToDict()
        {
            buttonDict = newReaderHelper.GetButtonsWithParameters();
        }
    }
}
