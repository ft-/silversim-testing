// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using System;
using System.Collections.Generic;
using System.Xml;

namespace SilverSim.Scripting.LSL
{
    public partial class LSLCompiler 
    {
        public IScriptState StateFromXml(XmlTextReader reader, Dictionary<string, string> attrs, ObjectPartInventoryItem item)
        {
            return Script.SavedScriptState.FromXML(reader, attrs, item);
        }
    }
}
