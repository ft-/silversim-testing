﻿// SilverSim is distributed under the terms of the
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
        void Cap_GetMesh(HttpRequest httpreq)
        {
            var parts = httpreq.RawUrl.Substring(1).Split('/');

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

            if (parts.Length < 4)
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            UUID meshID;
            if (parts[3].Substring(0, 1) != "?")
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            var meshreq = parts[3].Substring(1);
            var texreq = meshreq.Split('&');
            var mID = string.Empty;

            foreach (var texreqentry in texreq)
            {
                if (texreqentry.StartsWith("mesh_id="))
                {
                    mID = texreqentry.Substring(8);
                }
            }

            try
            {
                meshID = UUID.Parse(mID);
            }
            catch
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            AssetData asset;
            try
            {
                /* let us prefer the sim asset service */
                asset = Scene.AssetService[meshID];
            }
            catch
            {
                try
                {
                    asset = Agent.AssetService[meshID];
                    try
                    {
                        /* try to store the asset on our sim's asset service */
                        Scene.AssetService.Store(asset);
                    }
                    catch(Exception e)
                    {
                        m_Log.WarnFormat("Storing of asset failed: {0}: {1}\n{2}",
                            e.GetType().FullName,
                            e.Message,
                            e.StackTrace);
                    }
                }
                catch
                {
                    httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                    return;
                }
            }

            if (asset.Type != AssetType.Mesh)
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            ReturnRangeProcessedAsset(httpreq, asset, "application/vnd.ll.mesh", "GetMesh");
        }
    }
}
