using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XMLhelper;

namespace ADBSrv_NS.Classes
{
    class configReader
    {
        XMLReader _newReader;

        public configReader(XMLReader configXMLReader)
        {
            _newReader = configXMLReader;
        }

        public Dictionary<string, string> GetMiscFromConfig()
        {
            Dictionary<string, string> returnableDict = new Dictionary<string, string>();

            foreach (var item in _newReader.returnAllChildNodesFromParent("/config/misc", true))
            {
                returnableDict.Add(item.Substring(0, item.LastIndexOf('|')), item.Substring(item.LastIndexOf('|') + 1));
            }

            return returnableDict;
        }

        public Dictionary<string, Dictionary<string, string>> GetButtonsWithParameters()
        {
            Dictionary<string, Dictionary<string, string>> returnableDict = new Dictionary<string, Dictionary<string, string>>();

            foreach (var item in _newReader.returnAllChildNodesFromParent("/config/buttons", true))
            {
                Dictionary<string, string> tempDict = new Dictionary<string, string>();
                string searcTerm = item.Substring(0, item.LastIndexOf('|'));
                foreach (var item2 in _newReader.returnAllChildNodesFromParent($"/config/buttons/{searcTerm}", true))
                {
                    tempDict.Add(item2.Substring(0, item2.LastIndexOf('|')), item2.Substring(item2.LastIndexOf('|') + 1));
                }

                returnableDict.Add(item.Substring(0, item.LastIndexOf('|')), tempDict);
            }

            return returnableDict;
        }
    }
}
