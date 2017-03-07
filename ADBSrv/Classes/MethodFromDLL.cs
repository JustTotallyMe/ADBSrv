using System;
using System.Collections.Generic;
using System.Reflection;
using XMLhelper;

namespace ADBSrv_NS.Classes
{
    class MethodFromDLL
    {
        string _DLLPath;
        Assembly myDllAssembly;
        int _ButtonID;

        XMLReader newReader = new XMLReader(false, $"{AppDomain.CurrentDomain.BaseDirectory}ButtonConfig.xml");
        Logging logger;

        public void SetAllValues(int buttonID)
        {
            logger = new Logging($"{Properties.ADBSrv.Default.LoggingPath}\\MethodCallerLog_ButtonID_{buttonID}.txt");
            _DLLPath = newReader.returnSingleNodeEntry($"config/buttons/button{buttonID}/DLLPath");

            logger.WriteErrorLog($"DLLPATH = {_DLLPath}");

            _ButtonID = buttonID;
            myDllAssembly = Assembly.LoadFile(_DLLPath);
        }

        public void CallMethod()
        {
            Type type = myDllAssembly.GetType(GetClassName());
            type.GetMethod(GetMethodName()).Invoke(Activator.CreateInstance(type), null);
        }

        public void CallMethod(List<string> values)
        {
            object[] newObject = new object[values.Count];

            for (int i = 0; i < values.Count; i++)
            {
                newObject[i] = values[i];
            }

            Type type = myDllAssembly.GetType(GetClassName());
            type.GetMethod(GetMethodName()).Invoke(Activator.CreateInstance(type), newObject);
        }

        public object CallMethodWithReturn()
        {
            return null;
        }

        string GetClassName()
        {
            string className = null;

            try
            {
                var buttonValues = newReader.returnAllChildNodesFromParent($"/config/buttons/button{_ButtonID}", true);

                foreach (var item in buttonValues)
                {
                    logger.WriteErrorLog(item);
                }

                foreach (var item in buttonValues)
                {
                    if (item.Substring(0, item.LastIndexOf('|')) == "ClassName")
                    {
                        className = item.Substring(item.LastIndexOf('|') + 1);
                        break;
                    }
                }

                logger.WriteErrorLog($"ClassName: {className}");
            }
            catch (Exception e)
            {
                logger.WriteErrorLog(e.Message);
                logger.WriteErrorLog("-------------");
                logger.WriteErrorLog(e.StackTrace);
            }

            return className;
        }

        string GetMethodName()
        {
            var buttonValues = newReader.returnAllChildNodesFromParent($"/config/buttons/button{_ButtonID}", true);
            string className = null;

            foreach (var item in buttonValues)
            {
                if (item.Substring(0, item.LastIndexOf('|')) == "MethodName")
                {
                    className = item.Substring(item.LastIndexOf('|') + 1);
                }
            }

            logger.WriteErrorLog($"ClassName: {className}");

            return className;
        }
    }
}
