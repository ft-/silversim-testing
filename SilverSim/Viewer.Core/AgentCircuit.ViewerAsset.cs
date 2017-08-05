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
using SilverSim.Types.Asset;
using System;
using System.Net;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
    {
        private void Cap_ViewerAsset(HttpRequest httpreq)
        {
            if (httpreq.CallerIP != RemoteIP)
            {
                httpreq.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }
            if (httpreq.Method != "GET")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method Not Allowed");
                return;
            }

            int pos = httpreq.RawUrl.IndexOf('?');
            if (pos < 0)
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not found");
                return;
            }
            var reqUrl = httpreq.RawUrl.Substring(pos + 1);
            // Format: ?<type_name>_id=<uuid>
            pos = reqUrl.IndexOf("_id=");

            var assetType_string = reqUrl.Substring(0, pos);
            var assetId_string = reqUrl.Substring(pos + 4);
            UUID assetId;
            var assetType = assetType_string.StringToAssetType();
            if (!UUID.TryParse(assetId_string, out assetId))
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not found");
                return;
            }

            switch (assetType)
            {
                case AssetType.Animation:
                case AssetType.Sound:
                case AssetType.Notecard:
                case AssetType.Gesture:
                case AssetType.Bodypart:
                case AssetType.Clothing:
                case AssetType.Landmark:
                case AssetType.CallingCard:
                case AssetType.Texture:
                case AssetType.Mesh:
                    break;

                default:
                    m_Log.InfoFormat("ViewerAsset not enabled for {0}", assetType.ToString());
                    httpreq.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                    return;
            }

            AssetData asset;
            try
            {
                /* let us prefer the sim asset service */
                asset = Scene.AssetService[assetId];
            }
            catch (Exception e1)
            {
                try
                {
                    asset = Agent.AssetService[assetId];
                    try
                    {
                        /* try to store the asset on our sim's asset service */
                        asset.Temporary = true;
                        Scene.AssetService.Store(asset);
                    }
                    catch (Exception e3)
                    {
                        m_Log.DebugFormat("Failed to store asset {0} locally (Cap_ViewerAsset): {1}", assetId, e3.Message);
                    }
                }
                catch (Exception e2)
                {
                    if (Server.LogAssetFailures)
                    {
                        m_Log.DebugFormat("Failed to download asset {0} (Cap_ViewerAsset): {1} or {2}\nA: {3}\nB: {4}", assetId, e1.Message, e2.Message, e1.StackTrace, e2.StackTrace);
                    }
                    httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                    return;
                }
            }

            if (asset.Type != assetType)
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            ReturnRangeProcessedAsset(httpreq, asset, asset.ContentType, "ViewerAsset");
        }
    }
}
