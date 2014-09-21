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
            string json = "<llsd><map><key>folders</key><array><map><key>fetch_folders</key><integer>1</integer><key>fetch_items</key><boolean>1</boolean><key>folder_id</key><uuid>19341c8a-fa8c-482c-b497-6b0efd1d56eb</uuid><key>owner_id</key><uuid>c6ae0983-b983-4d3c-9858-ad1401adb480</uuid><key>sort_order</key><integer>1</integer></map></array></map></llsd>";
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
