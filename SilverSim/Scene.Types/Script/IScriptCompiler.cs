// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
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
                var o = new StringBuilder();
                foreach(var m in Messages)
                {
                    if(o.Length != 0)
                    {
                        o.Append("\n");
                    }
                    o.AppendFormat("{0}:{1}", m.Key, m.Value);
                }
                return o.ToString();
            }
        }
    }

    public interface IScriptCompiler
    {
        IScriptAssembly Compile(AppDomain appDom, UUI user, Dictionary<int, string> shbangs, UUID assetID, TextReader reader, int linenumber = 1, CultureInfo cultureInfo = null);
        void SyntaxCheck(UUI user, Dictionary<int, string> shbangs, UUID assetID, TextReader reader, int linenumber = 1, CultureInfo cultureInfo = null);
        IScriptState StateFromXml(XmlTextReader reader, Dictionary<string, string> attrs, ObjectPartInventoryItem item);

        /* for testing */
        void SyntaxCheckAndDump(Stream s, UUI user, Dictionary<int, string> shbangs, UUID assetID, TextReader reader, int linenumber = 1, CultureInfo cultureInfo = null);
        void CompileToDisk(string filename, AppDomain appDom, UUI user, Dictionary<int, string> shbangs, UUID assetID, TextReader reader, int linenumber = 1, CultureInfo cultureInfo = null);
    }
}
