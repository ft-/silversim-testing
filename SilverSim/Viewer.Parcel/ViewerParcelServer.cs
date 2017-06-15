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

#pragma warning disable IDE0018
#pragma warning disable RCS1029
#pragma warning disable RCS1163

using SilverSim.Main.Common;
using SilverSim.Threading;
using SilverSim.Viewer.Core;
using SilverSim.Viewer.Messages;
using System.Collections.Generic;
using System;
using System.Threading;
using SilverSim.Scene.Types.Scene;
using SilverSim.Viewer.Messages.Parcel;
using SilverSim.Types.Parcel;
using SilverSim.Types;
using log4net;
using SilverSim.Types.Grid;
using SilverSim.Scene.Management.Scene;
using System.ComponentModel;

namespace SilverSim.Viewer.Parcel
{
    [Description("Viewer Parcel Handler")]
    [PluginName("ViewerParcelServer")]
    public class ViewerParcelServer : IPlugin, IPacketHandlerExtender, ICapabilityExtender, IPluginShutdown
    {
        private static readonly ILog m_Log = LogManager.GetLogger("LL PARCEL");

        [PacketHandler(MessageType.ParcelInfoRequest)]
        private readonly BlockingQueue<KeyValuePair<AgentCircuit, Message>> RequestQueue = new BlockingQueue<KeyValuePair<AgentCircuit, Message>>();

        public ShutdownOrder ShutdownOrder => ShutdownOrder.LogoutRegion;
        private bool m_ShutdownParcel = false;
        private SceneList m_Scenes;

        public void Shutdown()
        {
            m_ShutdownParcel = true;
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_Scenes = loader.Scenes;
            ThreadManager.CreateThread(HandlerThread).Start();
        }

        public void HandlerThread()
        {
            Thread.CurrentThread.Name = "Parcel Handler Thread";

            while (!m_ShutdownParcel)
            {
                KeyValuePair<AgentCircuit, Message> req;
                try
                {
                    req = RequestQueue.Dequeue(1000);
                }
                catch
                {
                    continue;
                }

                Message m = req.Value;
                SceneInterface scene = req.Key.Scene;
                if (scene == null)
                {
                    continue;
                }
                try
                {
                    switch (m.Number)
                    {
                        case MessageType.ParcelInfoRequest:
                            HandleParcelInfoRequest(req.Key, scene, m);
                            break;

                    }
                }
                catch (Exception e)
                {
                    m_Log.Debug("Unexpected exception " + e.Message, e);
                }
            }
        }

        private void SendParcelInfo(AgentCircuit circuit, GridVector location, string simname, ParcelMetaInfo pinfo)
        {
            var reply = new ParcelInfoReply()
            {
                AgentID = circuit.AgentID,
                OwnerID = pinfo.Owner.ID,
                ParcelID = new ParcelID(location, pinfo.ParcelBasePosition),
                Name = pinfo.Name,
                Description = pinfo.Description,
                ActualArea = pinfo.ActualArea,
                BillableArea = pinfo.BillableArea,
                Flags = (byte)pinfo.Flags,
                SimName = simname,
                SnapshotID = UUID.Zero,
                Dwell = pinfo.Dwell,
                SalePrice = pinfo.SalePrice,
                AuctionID = pinfo.AuctionID
            };
            circuit.SendMessage(reply);
        }

        private void HandleParcelInfoOnLocal(AgentCircuit circuit, GridVector location, SceneInterface scene, ParcelInfoRequest req)
        {
            ParcelInfo pinfo;
            if (scene.Parcels.TryGetValue(req.ParcelID.RegionPos, out pinfo))
            {
                SendParcelInfo(circuit, location, scene.Name, pinfo);
            }
        }

        private void HandleParcelInfoRequest(AgentCircuit circuit, SceneInterface scene, Message m)
        {
            var req = (ParcelInfoRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            ParcelInfo pinfo;
            ParcelMetaInfo minfo;
            GridVector location = scene.GetRegionInfo().Location;
            RegionInfo regionInfo;
            if (req.ParcelID.Location == location)
            {
                /* local region */
                if (scene.Parcels.TryGetValue(req.ParcelID.RegionPos, out pinfo))
                {
                    HandleParcelInfoOnLocal(circuit, location, scene, req);
                }
            }
            else if (scene.GridService.TryGetValue(scene.ScopeID, req.ParcelID.Location, out regionInfo))
            {
                SceneInterface remoteSceneLocal;
                if(m_Scenes.TryGetValue(regionInfo.ID, out remoteSceneLocal))
                {
                    HandleParcelInfoOnLocal(circuit, req.ParcelID.Location, remoteSceneLocal, req);
                }
                else if(scene.GridService.RemoteParcelService.TryGetRequestRemoteParcel(regionInfo.ServerURI, req.ParcelID, out minfo))
                {
                    SendParcelInfo(circuit, location, regionInfo.Name, minfo);
                }
            }
        }
    }
}
