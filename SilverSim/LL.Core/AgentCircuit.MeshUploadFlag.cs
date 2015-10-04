// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common.HttpServer;
using SilverSim.StructuredData.LLSD;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

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
                string utcstring = DateTime.Now.AddDays(1).ToUniversalTime().ToString("yyyy\\-MM\\-dd\\THH\\-mm\\-ss\\Z");
                HttpResponse res = httpreq.BeginResponse();
                res.ContentType = "application/llsd+xml";
                Map m = new Map();
                m.Add("username", Agent.FirstName + "." + Agent.LastName);
                m.Add("display_name_next_update", new Date());
                m.Add("legacy_first_name", Agent.FirstName);
                m.Add("mesh_upload_status", "valid");
                m.Add("display_name", Agent.FirstName + " " + Agent.LastName);
                m.Add("legacy_last_name", Agent.LastName);
                m.Add("id", Agent.ID);
                m.Add("is_display_name_default", false);
                LLSD_XML.Serialize(m, res.GetOutputStream());
                res.Close();
            }
        }
    }
}
