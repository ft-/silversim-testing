// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Common;
using SilverSim.StructuredData.LLSD;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;

namespace SilverSim.LL.Core
{
    public partial class Circuit
    {
        void Cap_LSLSyntax(HttpRequest httpreq)
        {
            IValue o;
            if (httpreq.Method != "POST")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            try
            {
                o = LLSD_XML.Deserialize(httpreq.Body);
            }
            catch (Exception e)
            {
                m_Log.WarnFormat("Invalid LLSD_XML: {0} {1}", e.Message, e.StackTrace.ToString());
                httpreq.ErrorResponse(HttpStatusCode.UnsupportedMediaType, "Unsupported Media Type");
                return;
            }
            if (!(o is Map))
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML");
                return;
            }

            HttpResponse res = httpreq.BeginResponse("application/llsd+xml");
            IScriptCompiler compiler = CompilerRegistry.ScriptCompilers["lsl"];
            MethodInfo mi = compiler.GetType().GetMethod("WriteLSLSyntaxFile", new Type[] { typeof(Stream) });
            mi.Invoke(compiler, new object[] { res.GetOutputStream() });
            res.Close();
        }
    }
}
