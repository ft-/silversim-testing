using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.StructuredData.LLSD;
using System.IO;
using System.Globalization;
using SilverSim.Types;

namespace Tests.SilverSim.LLSDXmlTest
{
    public static class LLSDXmlTest
    {
        public static void Main(string[] args)
        {
            string json = "<llsd><map><key>ack</key><undef /><key>done</key><boolean>0</boolean></map></llsd>";
            IValue iv;
            using(MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                iv = LLSD_XML.Deserialize(ms);
            }
            MemoryStream os = new MemoryStream();
            LLSD_XML.Serialize(iv, os);
            System.Console.WriteLine(Encoding.UTF8.GetString(os.GetBuffer()));
        }
    }
}
