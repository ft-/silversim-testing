// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using SilverSim.Types.Parcel;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Parcel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThreadedClasses;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
    {
        [PacketHandler(MessageType.ParcelPropertiesUpdate)]
        public void HandleParcelPropertiesUpdate(Message m)
        {
            ParcelPropertiesUpdate req = (ParcelPropertiesUpdate)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            ParcelInfo pInfo;
            if (Scene.Parcels.TryGetValue(req.LocalID, out pInfo) &&
                Scene.CanEditParcelDetails(Agent.Owner, pInfo))
            {
                pInfo.Flags = req.ParcelFlags;
                pInfo.SalePrice = req.SalePrice;
                pInfo.Name = req.Name;
                pInfo.Description = req.Description;
                pInfo.MusicURI = (req.MusicURL.Length != 0) ?
                        new URI(req.MusicURL) : null;

                pInfo.MediaURI = (req.MediaURL.Length != 0) ?
                    new URI(req.MediaURL) : null;
                pInfo.MediaAutoScale = req.MediaAutoScale;
                UGI ugi;
                if (req.GroupID == UUID.Zero)
                {
                    ugi = UGI.Unknown;
                }
                else if (Scene.GroupsNameService.TryGetValue(req.GroupID, out ugi))
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
                    !Scene.AvatarNameService.TryGetValue(req.AuthBuyerID, out pInfo.AuthBuyer))
                {
                    pInfo.AuthBuyer = UUI.Unknown;
                }

                pInfo.SnapshotID = req.SnapshotID;
                pInfo.LandingPosition = req.UserLocation;
                pInfo.LandingLookAt = req.UserLookAt;
                pInfo.LandingType = req.LandingType;
                Scene.Parcels.Store(pInfo.ID);
            }
        }

        [PacketHandler(MessageType.ParcelDwellRequest)]
        public void HandleParcelDwellRequest(Message m)
        {
            ParcelDwellRequest req = (ParcelDwellRequest)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            ParcelInfo pInfo;
            if (Scene.Parcels.TryGetValue(req.LocalID, out pInfo))
            {
                ParcelDwellReply reply = new ParcelDwellReply();
                reply.AgentID = req.AgentID;
                reply.LocalID = req.LocalID;
                reply.ParcelID = pInfo.ID;
                reply.Dwell = 0;
                SendMessage(reply);
            }
        }

        const int P_AL_ACCESS = 1;
        const int P_AL_BAN = 2;
        const int P_MAX_ENTRIES = 1000 / 24;

        void SendParcelAccessList(int localID, ParcelAccessList listType, List<ParcelAccessEntry> list)
        {
            ParcelAccessListReply rep = new ParcelAccessListReply();
            int segments = (list.Count + P_MAX_ENTRIES - 1) / P_MAX_ENTRIES;
            int sequenceno = 1;
            if(list.Count == 0)
            {
                rep.AgentID = AgentID;
                rep.LocalID = localID;
                rep.Flags = listType;
                rep.SequenceID = sequenceno++;
                rep.AccessList.Add(new ParcelAccessListReply.Data());
                SendMessage(rep);
                return;
            }
            else
            {
                rep.AgentID = AgentID;
                rep.LocalID = localID;
                rep.Flags = listType;
                rep.SequenceID = sequenceno++;
                
                foreach (ParcelAccessEntry pae in list)
                {
                    if(rep.AccessList.Count == P_MAX_ENTRIES)
                    {
                        SendMessage(rep);
                        rep.AgentID = AgentID;
                        rep.LocalID = localID;
                        rep.Flags = listType;
                        rep.SequenceID = sequenceno++;
                    }
                    ParcelAccessListReply.Data pad = new ParcelAccessListReply.Data();
                    pad.Flags = listType;
                    pad.ID = pae.Accessor.ID;
                    rep.AccessList.Add(pad);
                }

                SendMessage(rep);
            }
        }

        [PacketHandler(MessageType.ParcelAccessListRequest)]
        public void HandleParcelAccessListRequest(Message m)
        {
            ParcelAccessListRequest req = (ParcelAccessListRequest)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            ParcelInfo pInfo;
            if (Scene.Parcels.TryGetValue(req.LocalID, out pInfo))
            {
                if ((req.Flags & ParcelAccessList.Access) != 0 && (pInfo.Owner.EqualsGrid(Agent.Owner) ||
                Scene.HasGroupPower(Agent.Owner, pInfo.Group, Types.Groups.GroupPowers.LandManageAllowed)))
                {
                    SendParcelAccessList(req.LocalID, ParcelAccessList.Access, Scene.Parcels.WhiteList[Scene.ID, pInfo.ID]);
                }
                if ((req.Flags & ParcelAccessList.Ban) != 0 && (pInfo.Owner.EqualsGrid(Agent.Owner) ||
                Scene.HasGroupPower(Agent.Owner, pInfo.Group, Types.Groups.GroupPowers.LandManageBanned)))
                {
                    SendParcelAccessList(req.LocalID, ParcelAccessList.Ban, Scene.Parcels.BlackList[Scene.ID, pInfo.ID]);
                }
            }
        }

        readonly object m_ParcelAccessListLock = new object();
        UUID m_ParcelAccessListTransaction = UUID.Zero;
        readonly Dictionary<int, ParcelAccessListUpdate> m_ParcelAccessListSegments = new Dictionary<int, ParcelAccessListUpdate>();

        readonly object m_ParcelBanListLock = new object();
        UUID m_ParcelBanListTransaction = UUID.Zero;
        readonly Dictionary<int, ParcelAccessListUpdate> m_ParcelBanListSegments = new Dictionary<int, ParcelAccessListUpdate>();

        void ParcelAccessListUpdateManage(UUID parcelID, Dictionary<UUID, ParcelAccessListUpdate.Data> entries, IParcelAccessList accessList)
        {
            foreach (ParcelAccessEntry listed in accessList[Scene.ID, parcelID])
            {
                if (!entries.ContainsKey(listed.Accessor.ID))
                {
                    accessList.Remove(Scene.ID, parcelID, listed.Accessor);
                }
            }
            foreach (ParcelAccessListUpdate.Data upd in entries.Values)
            {
                UUI uui;
                if (Scene.AvatarNameService.TryGetValue(upd.ID, out uui))
                {
                    ParcelAccessEntry pae = new ParcelAccessEntry();
                    pae.RegionID = Scene.ID;
                    pae.Accessor = uui;
                    pae.ParcelID = parcelID;
                    accessList.Store(pae);
                }
            }
        }

        [PacketHandler(MessageType.ParcelAccessListUpdate)]
        public void HandleParcelAccessListUpdateRequest(Message m)
        {
            ParcelAccessListUpdate req = (ParcelAccessListUpdate)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            ParcelInfo pInfo;
            if (!Scene.Parcels.TryGetValue(req.LocalID, out pInfo))
            {
                return;
            }

            if ((pInfo.Owner.EqualsGrid(Agent.Owner) ||
                Scene.HasGroupPower(Agent.Owner, pInfo.Group, Types.Groups.GroupPowers.LandManageAllowed)) &&
                req.Flags == ParcelAccessList.Access)
            {
                lock (m_ParcelAccessListLock)
                {
                    if (m_ParcelAccessListTransaction != req.TransactionID)
                    {
                        m_ParcelAccessListSegments.Clear();
                        m_ParcelAccessListTransaction = req.TransactionID;
                    }

                    m_ParcelAccessListSegments[req.SequenceID] = req;
                    if (req.Sections == 0)
                    {
                        ParcelAccessListUpdateManage(pInfo.ID, new Dictionary<UUID, ParcelAccessListUpdate.Data>(), Scene.Parcels.WhiteList);
                    }
                    else if (m_ParcelAccessListSegments.Count == req.Sections)
                    {
                        Dictionary<int, ParcelAccessListUpdate> list = new Dictionary<int, ParcelAccessListUpdate>(m_ParcelAccessListSegments);
                        m_ParcelAccessListSegments.Clear();
                        m_ParcelAccessListTransaction = UUID.Zero;
                        bool isComplete = true;
                        for (int i = 1; i < req.Sections; ++i)
                        {
                            if (!list.ContainsKey(i))
                            {
                                isComplete = false;
                                break;
                            }
                        }
                        if (!isComplete)
                        {
                            return;
                        }

                        Dictionary<UUID, ParcelAccessListUpdate.Data> entries = new Dictionary<UUID, ParcelAccessListUpdate.Data>();
                        foreach (ParcelAccessListUpdate upd in list.Values)
                        {
                            foreach (ParcelAccessListUpdate.Data d in upd.AccessList)
                            {
                                entries[d.ID] = d;
                            }
                        }

                        ParcelAccessListUpdateManage(pInfo.ID, entries, Scene.Parcels.WhiteList);
                    }
                }
            }

            if ((pInfo.Owner.EqualsGrid(Agent.Owner) ||
                Scene.HasGroupPower(Agent.Owner, pInfo.Group, Types.Groups.GroupPowers.LandManageAllowed)) &&
                req.Flags == ParcelAccessList.Ban)
            { 
                lock (m_ParcelBanListLock)
                {
                    if (m_ParcelBanListTransaction != req.TransactionID)
                    {
                        m_ParcelBanListSegments.Clear();
                        m_ParcelBanListTransaction = req.TransactionID;
                    }

                    m_ParcelBanListSegments[req.SequenceID] = req;
                    if (req.Sections == 0)
                    {
                        ParcelAccessListUpdateManage(pInfo.ID, new Dictionary<UUID, ParcelAccessListUpdate.Data>(), Scene.Parcels.BlackList);
                    }
                    else if (m_ParcelBanListSegments.Count == req.Sections)
                    {
                        Dictionary<int, ParcelAccessListUpdate> list = new Dictionary<int, ParcelAccessListUpdate>(m_ParcelBanListSegments);
                        m_ParcelBanListSegments.Clear();
                        m_ParcelBanListTransaction = UUID.Zero;
                        bool isComplete = true;
                        for (int i = 1; i < req.Sections; ++i)
                        {
                            if (!list.ContainsKey(i))
                            {
                                isComplete = false;
                                break;
                            }
                        }
                        if (!isComplete)
                        {
                            return;
                        }

                        Dictionary<UUID, ParcelAccessListUpdate.Data> entries = new Dictionary<UUID, ParcelAccessListUpdate.Data>();
                        foreach (ParcelAccessListUpdate upd in list.Values)
                        {
                            foreach (ParcelAccessListUpdate.Data d in upd.AccessList)
                            {
                                entries[d.ID] = d;
                            }
                        }

                        ParcelAccessListUpdateManage(pInfo.ID, entries, Scene.Parcels.WhiteList);
                    }
                }
            }
        }
    }
}
