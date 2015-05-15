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

using SilverSim.Main.Common.HttpServer;
using SilverSim.StructuredData.LLSD;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace SilverSim.LL.Core
{
    public partial class Circuit
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
