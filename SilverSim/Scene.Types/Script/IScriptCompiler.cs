// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

namespace SilverSim.Scene.Types.Script
{
    [Serializable]
    public class CompilerException : Exception
    {
        public Dictionary<int, string> Messages = new Dictionary<int,string>();


        public CompilerException()
        {

        }

        public CompilerException(string msg)
            : base(msg)
        {

        }

        protected CompilerException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public CompilerException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        public CompilerException(int linenumber, string message)
            :base(message)
        {
            Messages.Add(linenumber, message);
        }

        public CompilerException(Dictionary<int, string> messages)
        {
            Messages = messages;
        }

        public override string Message
        {
            get
            {
                string o = string.Empty;
                foreach(KeyValuePair<int, string> m in Messages)
                {
                    if(o.Length != 0)
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
