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

using SilverSim.Main.Common.HttpServer;
using SilverSim.Types;
using SilverSim.Types.StructuredData.Llsd;
using System.IO;
using System.Net;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
    {
        void Cap_MeshUploadFlag(HttpRequest httpreq)
        {
            if (httpreq.Method != "GET")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
            }
            else
            {
                using (var res = httpreq.BeginResponse())
                {
                    res.ContentType = "application/llsd+xml";
                    var m = new Map();
                    m.Add("username", Agent.FirstName + "." + Agent.LastName);
                    m.Add("display_name_next_update", new Date());
                    m.Add("legacy_first_name", Agent.FirstName);
                    m.Add("mesh_upload_status", "valid");
                    m.Add("display_name", Agent.FirstName + " " + Agent.LastName);
                    m.Add("legacy_last_name", Agent.LastName);
                    m.Add("id", Agent.ID);
                    m.Add("is_display_name_default", false);
                    using (var o = res.GetOutputStream())
                    {
                        LlsdXml.Serialize(m, o);
                    }
                }
            }
        }
    }
}
