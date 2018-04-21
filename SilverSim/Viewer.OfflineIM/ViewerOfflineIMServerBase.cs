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
using SilverSim.ServiceInterfaces.IM;
using SilverSim.Types;
using SilverSim.Types.IM;
using SilverSim.Types.StructuredData.Llsd;
using System.IO;
using System.Net;

namespace SilverSim.Viewer.OfflineIM
{
    public abstract class ViewerOfflineIMServerBase
    {
        protected void ProcessReadOfflineMsgs(UUID ownerID, HttpRequest req, OfflineIMServiceInterface offlineIMService)
        {
            if (req.Method != "GET")
            {
                req.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            var resmap = new Map();
            var msgs = new AnArray();
            resmap.Add("messages", msgs);
            if (offlineIMService != null)
            {
                try
                {
                    foreach (GridInstantMessage gim in offlineIMService.GetOfflineIMs(ownerID))
                    {
                        msgs.Add(new Map
                        {
                            { "binary_bucket", new BinaryData(gim.BinaryBucket) },
                            { "parent_estate_id", gim.ParentEstateID },
                            { "from_agent_id", gim.FromAgent.ID },
                            { "from_group", gim.IsFromGroup },
                            { "dialog", (int)gim.Dialog },
                            { "session_id", gim.IMSessionID },
                            { "timestamp", gim.Timestamp.AsInt },
                            { "from_agent_name", gim.FromAgent.FullName },
                            { "message", gim.Message },
                            { "region_id", gim.RegionID },
                            { "local_x", gim.Position.X },
                            { "local_y", gim.Position.Y },
                            { "local_z", gim.Position.Z },
                            { "asset_id", gim.FromGroup.ID } /* probably this gets changed in feature */
                        });
                    }
                }
                catch
                {
                    /* do not pass exceptions to caller */
                }
            }
            using (HttpResponse res = req.BeginResponse("application/llsd+xml"))
            {
                using (Stream s = res.GetOutputStream())
                {
                    LlsdXml.Serialize(resmap, s);
                }
            }
        }
    }
}
