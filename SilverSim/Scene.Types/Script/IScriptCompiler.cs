/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace SilverSim.Scene.Types.Script
{
    public class CompilerException : Exception
    {
        public Dictionary<int, string> Messages = new Dictionary<int,string>();

        public CompilerException(int linenumber, string message)
            :base(message)
        {
            Messages.Add(linenumber, message);
        }

        public CompilerException(Dictionary<int, string> messages)
        {
            Messages = messages;
        }

        public new string Message
        {
            get
            {
                string o = string.Empty;
                foreach(KeyValuePair<int, string> m in Messages)
                {
                    if(o != "")
                    {
                        o += "\n";
                    }
                    o += string.Format("{0}:{1}", m.Key, m.Value);
                }
                return o;
            }
        }
    }

    public interface IScriptCompiler
    {
        IScriptAssembly Compile(AppDomain appDom, UUI user, Dictionary<int, string> shbangs, UUID assetID, TextReader reader, int linenumber = 1);
        void SyntaxCheck(UUI user, Dictionary<int, string> shbangs, UUID assetID, TextReader reader, int linenumber = 1);
        IScriptState StateFromXml(XmlTextReader reader, Dictionary<string, string> attrs, ObjectPartInventoryItem item);

        /* for testing */
        void SyntaxCheckAndDump(Stream s, UUI user, Dictionary<int, string> shbangs, UUID assetID, TextReader reader, int linenumber = 1);
    }
}
