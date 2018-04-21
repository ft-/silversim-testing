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

using log4net;
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Economy;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Economy.Transactions;
using SilverSim.Types.Grid;
using SilverSim.Types.Parcel;
using SilverSim.Types.StructuredData.Llsd;
using SilverSim.Viewer.Core;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Parcel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading;

namespace SilverSim.Viewer.Parcel
{
    [Description("Viewer Parcel Handler")]
    [PluginName("ViewerParcelServer")]
    public class ViewerParcelServer : IPlugin, IPacketHandlerExtender, ICapabilityExtender, IPluginShutdown
    {
        private static readonly ILog m_Log = LogManager.GetLogger("LL PARCEL");

        [PacketHandler(MessageType.ParcelInfoRequest)]
        [PacketHandler(MessageType.ParcelBuy)]
        [PacketHandler(MessageType.ParcelGodForceOwner)]
        [PacketHandler(MessageType.ParcelDeedToGroup)]
        [PacketHandler(MessageType.ParcelGodMarkAsContent)]
        [PacketHandler(MessageType.ParcelRelease)]
        [PacketHandler(MessageType.ParcelJoin)]
        [PacketHandler(MessageType.ParcelDivide)]
        [PacketHandler(MessageType.ParcelReclaim)]
        [PacketHandler(MessageType.ParcelSetOtherCleanTime)]
        [PacketHandler(MessageType.ParcelPropertiesUpdate)]
        [PacketHandler(MessageType.ParcelClaim)]
        private readonly BlockingQueue<KeyValuePair<AgentCircuit, Message>> RequestQueue = new BlockingQueue<KeyValuePair<AgentCircuit, Message>>();

        public ShutdownOrder ShutdownOrder => ShutdownOrder.LogoutRegion;
        private bool m_ShutdownParcel;
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
                SceneInterface scene = req.Key?.Scene;
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

                        case MessageType.ParcelBuy:
                            HandleParcelBuy(req.Key, scene, m);
                            break;

                        case MessageType.ParcelGodForceOwner:
                            HandleParcelGodForceOwner(req.Key, scene, m);
                            break;

                        case MessageType.ParcelDeedToGroup:
                            HandleParcelDeedToGroup(req.Key, scene, m);
                            break;

                        case MessageType.ParcelGodMarkAsContent:
                            HandleParcelGodMarkAsContent(req.Key, scene, m);
                            break;

                        case MessageType.ParcelRelease:
                            HandleParcelRelease(req.Key, scene, m);
                            break;

                        case MessageType.ParcelJoin:
                            HandleParcelJoin(req.Key, scene, m);
                            break;

                        case MessageType.ParcelDivide:
                            HandleParcelDivide(req.Key, scene, m);
                            break;

                        case MessageType.ParcelReclaim:
                            HandleParcelReclaim(req.Key, scene, m);
                            break;

                        case MessageType.ParcelSetOtherCleanTime:
                            HandleParcelSetOtherCleanTime(req.Key, scene, m);
                            break;

                        case MessageType.ParcelPropertiesUpdate:
                            HandleParcelPropertiesUpdate(req.Key, scene, m);
                            break;

                        case MessageType.ParcelClaim:
                            HandleParcelClaim(req.Key, scene, m);
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
            byte parcelFlags = 0;
            if(pinfo.Access <= RegionAccess.PG)
            {
                parcelFlags = 0;
            }
            else if(pinfo.Access <= RegionAccess.Mature)
            {
                parcelFlags = 1;
            }
            else if(pinfo.Access <= RegionAccess.Adult)
            {
                parcelFlags = 2;
            }
            if((pinfo.Flags & ParcelFlags.ForSale) != 0)
            {
                parcelFlags |= (1 << 7);
            }
            var reply = new ParcelInfoReply
            {
                AgentID = circuit.AgentID,
                OwnerID = pinfo.Owner.ID,
                ParcelID = new ParcelID(location, pinfo.ParcelBasePosition),
                Name = pinfo.Name,
                Description = pinfo.Description,
                ActualArea = pinfo.ActualArea,
                BillableArea = pinfo.BillableArea,
                Flags = parcelFlags,
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

        public void HandleParcelGodForceOwner(AgentCircuit circuit, SceneInterface scene, Message m)
        {
            var req = (ParcelGodForceOwner)m;
            UGUIWithName agentID;
            ParcelInfo pInfo;
            IAgent godAgent = circuit.Agent;

            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID ||
                req.OwnerID != req.AgentID ||
                !scene.Agents.TryGetValue(req.AgentID, out godAgent) ||
                !scene.AvatarNameService.TryGetValue(req.OwnerID, out agentID) ||
                !scene.Parcels.TryGetValue(req.LocalID, out pInfo) ||
                !godAgent.IsActiveGod ||
                !godAgent.IsInScene(scene))
            {
                return;
            }
            m_Log.InfoFormat("Forced parcel {0} ({1}) to be owned by {2}", pInfo.Name, pInfo.ID, agentID.FullName);
            pInfo.Group = UGI.Unknown;
            pInfo.GroupOwned = false;
            pInfo.ClaimDate = Date.Now;
            pInfo.SalePrice = 0;
            pInfo.AuthBuyer = UGUI.Unknown;
            pInfo.Owner = agentID;
            pInfo.Flags &= ~(ParcelFlags.ForSale | ParcelFlags.ForSaleObjects | ParcelFlags.SellParcelObjects | ParcelFlags.ShowDirectory);
            scene.TriggerParcelUpdate(pInfo);
        }

        public void HandleParcelDeedToGroup(AgentCircuit circuit, SceneInterface scene, Message m)
        {
            var req = (ParcelDeedToGroup)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            IAgent agent = circuit.Agent;

            ParcelInfo pInfo;
            if (scene.Parcels.TryGetValue(req.LocalID, out pInfo) &&
                scene.CanDeedParcel(agent.Owner, pInfo))
            {
                if (!pInfo.Group.Equals(UGUI.Unknown))
                {
                    pInfo.GroupOwned = true;
                }
                scene.TriggerParcelUpdate(pInfo);
            }
        }

        
        public void HandleParcelGodMarkAsContent(AgentCircuit circuit, SceneInterface scene, Message m)
        {
            var req = (ParcelGodForceOwner)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            IAgent agent = circuit.Agent;

            ParcelInfo pInfo;
            if (scene.Parcels.TryGetValue(req.LocalID, out pInfo) &&
                scene.CanGodMarkParcelAsContent(agent.Owner, pInfo))
            {
                pInfo.Group = UGI.Unknown;
                pInfo.GroupOwned = false;
                pInfo.ClaimDate = Date.Now;
                pInfo.SalePrice = 0;
                pInfo.AuthBuyer = UGUI.Unknown;
                pInfo.Owner = scene.Owner;
                pInfo.Flags &= ~(ParcelFlags.ForSale | ParcelFlags.ForSaleObjects | ParcelFlags.SellParcelObjects | ParcelFlags.ShowDirectory);
                scene.TriggerParcelUpdate(pInfo);
            }
        }

        
        public void HandleParcelRelease(AgentCircuit circuit, SceneInterface scene, Message m)
        {
            var req = (ParcelRelease)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            IAgent agent = circuit.Agent;

            ParcelInfo pInfo;
            if (scene.Parcels.TryGetValue(req.LocalID, out pInfo) &&
                scene.CanReleaseParcel(agent.Owner, pInfo))
            {
                pInfo.Group = UGI.Unknown;
                pInfo.GroupOwned = false;
                pInfo.Owner = UGUI.Unknown;
                pInfo.SalePrice = 0;
                pInfo.AuthBuyer = UGUI.Unknown;
                pInfo.Flags &= ~(ParcelFlags.ForSale | ParcelFlags.ForSaleObjects | ParcelFlags.SellParcelObjects | ParcelFlags.ShowDirectory);
                scene.TriggerParcelUpdate(pInfo);
            }
        }
        
        public void HandleParcelJoin(AgentCircuit circuit, SceneInterface scene, Message m)
        {
            var req = (ParcelJoin)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            IAgent agent = circuit.Agent;

            scene.JoinParcels(agent.Owner, (int)Math.Round(req.West), (int)Math.Round(req.South), (int)Math.Round(req.East), (int)Math.Round(req.North));
        }

        public void HandleParcelDivide(AgentCircuit circuit, SceneInterface scene, Message m)
        {
            var req = (ParcelDivide)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            IAgent agent = circuit.Agent;

            scene.DivideParcel(agent.Owner, (int)Math.Round(req.West), (int)Math.Round(req.South), (int)Math.Round(req.East), (int)Math.Round(req.North));
        }

        public void HandleParcelReclaim(AgentCircuit circuit, SceneInterface scene, Message m)
        {
            var req = (ParcelReclaim)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            IAgent agent = circuit.Agent;

            ParcelInfo pInfo;
            if (scene.Parcels.TryGetValue(req.LocalID, out pInfo) &&
                scene.CanReclaimParcel(agent.Owner, pInfo))
            {
                pInfo.Group = UGI.Unknown;
                pInfo.GroupOwned = false;
                pInfo.ClaimDate = Date.Now;
                pInfo.SalePrice = 0;
                pInfo.AuthBuyer = UGUI.Unknown;
                pInfo.Owner = scene.Owner;
                pInfo.Flags &= ~(ParcelFlags.ForSale | ParcelFlags.ForSaleObjects | ParcelFlags.SellParcelObjects | ParcelFlags.ShowDirectory);
                scene.TriggerParcelUpdate(pInfo);
            }
        }

        public void HandleParcelSetOtherCleanTime(AgentCircuit circuit, SceneInterface scene, Message m)
        {
            var req = (ParcelSetOtherCleanTime)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            IAgent agent = circuit.Agent;
            ParcelInfo pInfo;
            if (scene.Parcels.TryGetValue(req.LocalID, out pInfo) &&
                scene.CanEditParcelDetails(agent.Owner, pInfo))
            {
                pInfo.OtherCleanTime = req.OtherCleanTime;
                scene.TriggerParcelUpdate(pInfo);
            }
        }

        public void HandleParcelPropertiesUpdate(AgentCircuit circuit, SceneInterface scene, Message m)
        {
            var req = (ParcelPropertiesUpdate)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            IAgent agent = circuit.Agent;

            ParcelInfo pInfo;
            if (scene.Parcels.TryGetValue(req.LocalID, out pInfo) &&
                scene.CanEditParcelDetails(agent.Owner, pInfo))
            {
                pInfo.Flags = req.ParcelFlags;
                pInfo.SalePrice = req.SalePrice;
                pInfo.Name = req.Name;
                pInfo.Description = req.Description;
                pInfo.MusicURI = (req.MusicURL.Length != 0) && Uri.IsWellFormedUriString(req.MusicURL, UriKind.Absolute) ?
                        new URI(req.MusicURL) : null;

                pInfo.MediaURI = (req.MediaURL.Length != 0) && Uri.IsWellFormedUriString(req.MediaURL, UriKind.Absolute) ?
                    new URI(req.MediaURL) : null;
                pInfo.MediaAutoScale = req.MediaAutoScale;
                UGI ugi;
                if (req.GroupID == UUID.Zero)
                {
                    ugi = UGI.Unknown;
                }
                else if (scene.GroupsNameService.TryGetValue(req.GroupID, out ugi))
                {
                    pInfo.Group = ugi;
                }
                else
                {
                    pInfo.Group = UGI.Unknown;
                }

                pInfo.PassPrice = req.PassPrice;
                pInfo.PassHours = req.PassHours;
                pInfo.Category = req.Category;
                if (req.AuthBuyerID == UUID.Zero ||
                    !scene.AvatarNameService.TryGetValue(req.AuthBuyerID, out pInfo.AuthBuyer))
                {
                    pInfo.AuthBuyer = UGUI.Unknown;
                }

                pInfo.SnapshotID = req.SnapshotID;
                pInfo.LandingPosition = req.UserLocation;
                pInfo.LandingLookAt = req.UserLookAt;
                pInfo.LandingType = req.LandingType;
                scene.TriggerParcelUpdate(pInfo);
            }
        }

        private void HandleParcelClaim(AgentCircuit circuit, SceneInterface scene, Message m)
        {
            var req = (ParcelClaim)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            ViewerAgent agent = circuit.Agent;

            int totalPrice = 0;
            foreach(ParcelClaim.ParcelDataEntry d in req.ParcelData)
            {
                if(!scene.IsParcelClaimable((int)Math.Round(d.West), (int)Math.Round(d.South), (int)Math.Round(d.East), (int)Math.Round(d.North)))
                {
                    return;
                }
                totalPrice += CalculateClaimPrice(d);
            }

            int parcelClaimPrice = scene.EconomyData.PriceParcelClaim;
            double parcelClaimFactor = scene.EconomyData.PriceParcelClaimFactor;

            totalPrice = (int)Math.Ceiling(totalPrice * parcelClaimPrice * parcelClaimFactor);
            EconomyServiceInterface economyService = agent.EconomyService;
            
            if(totalPrice == 0)
            {
                foreach (ParcelClaim.ParcelDataEntry d in req.ParcelData)
                {
                    scene.ClaimParcel(agent.Owner, (int)Math.Round(d.West), (int)Math.Round(d.South), (int)Math.Round(d.East), (int)Math.Round(d.North),
                        CalculateClaimPrice(d));
                }
            }
            else if(economyService != null)
            {
                economyService.ChargeAmount(agent.Owner, new LandClaimTransaction(scene.GridPosition, scene.ID, scene.Name), totalPrice, () =>
                {
                    foreach (ParcelClaim.ParcelDataEntry d in req.ParcelData)
                    {
                        scene.ClaimParcel(agent.Owner, (int)Math.Round(d.West), (int)Math.Round(d.South), (int)Math.Round(d.East), (int)Math.Round(d.North),
                            CalculateClaimPrice(d));
                    }
                });
            }
        }

        int CalculateClaimPrice(ParcelClaim.ParcelDataEntry d)
        {
            int w = (int)d.West & ~3;
            int e = (int)d.East & ~3;
            int n = (int)d.North & ~3;
            int s = (int)d.South & ~3;
            int dx = Math.Abs(w - e);
            int dy = Math.Abs(n - s);
            return dx * dy;
        }

        private void HandleParcelBuy(AgentCircuit circuit, SceneInterface scene, Message m)
        {
            var req = (ParcelBuy)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            ViewerAgent agent = circuit.Agent;

            ParcelInfo pinfo;
            if(scene.Parcels.TryGetValue(req.LocalID, out pinfo) && (pinfo.Flags & ParcelFlags.ForSale) != 0)
            {
                if(pinfo.AuthBuyer.EqualsGrid(agent.Owner) || pinfo.AuthBuyer == UGUI.Unknown)
                {
                    if(pinfo.SalePrice != 0)
                    {
                        /* we have to process it on economy */
                        EconomyServiceInterface economyService = agent.EconomyService;
                        if(economyService != null)
                        {
                            if (pinfo.GroupOwned)
                            {
                                economyService.TransferMoney(agent.Owner, pinfo.Group, new LandSaleTransaction(scene.GetRegionInfo().Location, scene.ID, scene.Name)
                                {
                                    ParcelID = pinfo.ID,
                                    ParcelName = pinfo.Name
                                }, pinfo.SalePrice, () => ChangeParcelOwnership(scene, pinfo, agent.Owner, pinfo.SalePrice, req));
                            }
                            else
                            {
                                economyService.TransferMoney(agent.Owner, pinfo.Owner, new LandSaleTransaction(scene.GetRegionInfo().Location, scene.ID, scene.Name)
                                {
                                    ParcelID = pinfo.ID,
                                    ParcelName = pinfo.Name
                                }, pinfo.SalePrice, () => ChangeParcelOwnership(scene, pinfo, agent.Owner, pinfo.SalePrice, req));
                            }
                        }
                    }
                    else
                    {
                        ChangeParcelOwnership(scene, pinfo, agent.Owner, pinfo.SalePrice, req);
                    }
                }
            }
        }

        private void ChangeParcelOwnership(SceneInterface scene, ParcelInfo pinfo, UGUI newOwner, int soldforprice, ParcelBuy req)
        {
            bool sellParcelObjects = (pinfo.Flags & ParcelFlags.SellParcelObjects) != 0;
            UGUI oldOwner = pinfo.Owner;
            UGI oldGroup = pinfo.Group;
            bool wasGroupOwned = pinfo.GroupOwned;

            pinfo.Owner = newOwner;
            pinfo.Group = UGI.Unknown;
            pinfo.GroupOwned = false;
            pinfo.ClaimDate = Date.Now;
            pinfo.ClaimPrice = soldforprice;
            pinfo.SalePrice = 0;
            pinfo.AuthBuyer = UGUI.Unknown;
            pinfo.Flags &= ~(ParcelFlags.ForSale | ParcelFlags.ForSaleObjects | ParcelFlags.SellParcelObjects | ParcelFlags.ShowDirectory);
            scene.TriggerParcelUpdate(pinfo);

            if(sellParcelObjects)
            {
                foreach(ObjectGroup grp in scene.ObjectGroups)
                {
                    try
                    {
                        if (pinfo.LandBitmap.ContainsLocation(grp.GlobalPosition) && 
                            wasGroupOwned ? (grp.IsGroupOwned && grp.Group == oldGroup) : (!grp.IsGroupOwned && grp.Owner == oldOwner))
                        {
                            grp.Owner = newOwner;
                        }
                    }
                    catch
                    {
                        /* do not trip on this */
                    }
                }
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

        [CapabilityHandler("RemoteParcelRequest")]
        public void HandleRemoteParcelRequest(ViewerAgent agent, AgentCircuit circuit, HttpRequest req)
        {
            if (req.CallerIP != circuit.RemoteIP)
            {
                req.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }
            if (req.Method != "POST")
            {
                req.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            Map reqmap;
            try
            {
                reqmap = LlsdXml.Deserialize(req.Body) as Map;
            }
            catch
            {
                req.ErrorResponse(HttpStatusCode.BadRequest, "Bad request");
                return;
            }

            if(reqmap == null)
            {
                req.ErrorResponse(HttpStatusCode.BadRequest, "Bad request");
                return;
            }

            SceneInterface scene = circuit.Scene;
            if (scene == null)
            {
                req.ErrorResponse(HttpStatusCode.BadRequest, "Bad request");
                return;
            }

            AnArray locationArray;
            IValue iv_target;
            var parcelid = new ParcelID();
            if(reqmap.TryGetValue("location", out locationArray))
            {
                uint x = locationArray[0].AsUInt;
                uint y = locationArray[1].AsUInt;

                if(reqmap.TryGetValue("region_handle", out iv_target))
                {
                    byte[] regHandleBytes = (BinaryData)iv_target;
                    var v = new GridVector(regHandleBytes, 0);
                    RegionInfo rInfo;

                    if(v == scene.GridPosition)
                    {
                        parcelid = new ParcelID(v, new Vector3(x, y, 0));
                    }
                    else if(scene.GridService.TryGetValue(scene.ScopeID, v, out rInfo))
                    {
                        /* shift coordinate to actual region begin */
                        Vector3 offset = (Vector3)v - rInfo.Location;
                        offset.X += x;
                        offset.Y += y;
                        /* ensure that the position is inside region */
                        if(offset.X > rInfo.Size.X)
                        {
                            offset.X = rInfo.Size.X;
                        }
                        if (offset.Y > rInfo.Size.Y)
                        {
                            offset.Y = rInfo.Size.Y;
                        }
                        parcelid = new ParcelID(rInfo.Location, offset);
                    }
                }
                else if(reqmap.TryGetValue("region_id", out iv_target))
                {
                    SceneInterface remoteSceneLocal;
                    RegionInfo rInfo;
                    if(m_Scenes.TryGetValue(iv_target.AsUUID, out remoteSceneLocal))
                    {
                        parcelid = new ParcelID(remoteSceneLocal.GridPosition, new Vector3(x, y, 0));
                    }
                    else if(scene.GridService.TryGetValue(iv_target.AsUUID, out rInfo))
                    {
                        parcelid = new ParcelID(rInfo.Location, new Vector3(x, y, 0));
                    }
                }
            }

            var resmap = new Map
            {
                ["parcel_id"] = new UUID(parcelid.GetBytes(), 0)
            };

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
