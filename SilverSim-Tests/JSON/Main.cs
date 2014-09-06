using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.StructuredData.JSON;
using System.IO;
using System.Globalization;
using SilverSim.Types;

namespace Tests.SilverSim.JSONTest
{
    public static class JSONTest
    {
        public static void Main(string[] args)
        {
            string json = "{" +
   "\"anObject\": {" +
      "\"numericProperty\": -122," +
      "\"stringProperty\": \"An offensive \\\" is problematic\"," +
      "\"nullProperty\": null," +
      "\"booleanProperty\": true," +
      "\"dateProperty\": \"2011-09-23\"" +
   "}," +
   "\"arrayOfObjects\": [" +
    "[" +
      "{" +
         "\"item\": 1" +
      "}," +
      "{" +
         "\"item\": 2" +
      "}," +
      "{" +
         "\"item\": 3" +
      "}" +
      "]"+
   "]," +
   "\"arrayOfIntegers\": [" +
      "1," +
      "2," +
      "3," +
      "4," +
      "5" +
   "]" +
"} ";
            IValue iv;
            using(MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                iv = JSON.Deserialize(ms);
            }
            MemoryStream os = new MemoryStream();
            JSON.Serialize(iv, os);
            System.Console.WriteLine(Encoding.UTF8.GetString(os.GetBuffer()));
        }
    }
}
