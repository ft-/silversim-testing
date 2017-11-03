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

using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using SilverSim.Types.Experience;
using SilverSim.Types.Parcel;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Parcel;
using System.Collections.Generic;
using System.Linq;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
    {
        private const int P_AL_ACCESS = 1;
        private const int P_AL_BAN = 2;
        private const int P_MAX_ENTRIES = 48;

        private void SendParcelAccessList(int localID, ParcelAccessList listType, List<ParcelAccessEntry> list)
        {
            var rep = new ParcelAccessListReply();
            int sequenceno = 1;
            if(list.Count == 0)
            {
                rep.AgentID = AgentID;
                rep.LocalID = localID;
                rep.Flags = listType;
                rep.SequenceID = sequenceno++;
                rep.AccessList.Add(new ParcelAccessListReply.Data { Flags = listType });
                SendMessage(rep);
                return;
            }
            else
            {
                rep.AgentID = AgentID;
                rep.LocalID = localID;
                rep.Flags = listType;
                rep.SequenceID = sequenceno++;

                foreach (var pae in list)
                {
                    if(rep.AccessList.Count == P_MAX_ENTRIES)
                    {
                        SendMessage(rep);
                        rep.AgentID = AgentID;
                        rep.LocalID = localID;
                        rep.Flags = listType;
                        rep.SequenceID = sequenceno++;
                    }
                    var pad = new ParcelAccessListReply.Data
                    {
                        Flags = listType,
                        ID = pae.Accessor.ID
                    };
                    rep.AccessList.Add(pad);
                }

                SendMessage(rep);
            }
        }

        private void SendParcelAccessList(int localID, ParcelAccessList listType, List<UUID> list)
        {
            var rep = new ParcelAccessListReply();
            int sequenceno = 1;
            if (list.Count == 0)
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

                foreach (var pae in list)
                {
                    if (rep.AccessList.Count == P_MAX_ENTRIES)
                    {
                        SendMessage(rep);
                        rep.AgentID = AgentID;
                        rep.LocalID = localID;
                        rep.Flags = listType;
                        rep.SequenceID = sequenceno++;
                    }
                    var pad = new ParcelAccessListReply.Data
                    {
                        Flags = listType,
                        ID = pae
                    };
                    rep.AccessList.Add(pad);
                }

                SendMessage(rep);
            }
        }

        [PacketHandler(MessageType.ParcelAccessListRequest)]
        public void HandleParcelAccessListRequest(Message m)
        {
            var req = (ParcelAccessListRequest)m;
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
                var allowedexp = new List<UUID>();
                var blockedexp = new List<UUID>();
                foreach (ParcelExperienceEntry entry in Scene.Parcels.Experiences[Scene.ID, pInfo.ID])
                {
                    if(entry.IsAllowed)
                    {
                        allowedexp.Add(entry.ExperienceID);
                    }
                    else
                    {
                        blockedexp.Add(entry.ExperienceID);
                    }
                }
                if ((req.Flags & ParcelAccessList.AllowExperience) != 0)
                {
                    SendParcelAccessList(req.LocalID, ParcelAccessList.AllowExperience, allowedexp);
                }
                if ((req.Flags & ParcelAccessList.BlockExperience) != 0)
                {
                    SendParcelAccessList(req.LocalID, ParcelAccessList.BlockExperience, blockedexp);
                }
            }
        }

        private readonly object m_ParcelAccessListLock = new object();
        private UUID m_ParcelAccessListTransaction = UUID.Zero;
        private readonly Dictionary<int, ParcelAccessListUpdate> m_ParcelAccessListSegments = new Dictionary<int, ParcelAccessListUpdate>();

        private readonly object m_ParcelBanListLock = new object();
        private UUID m_ParcelBanListTransaction = UUID.Zero;
        private readonly Dictionary<int, ParcelAccessListUpdate> m_ParcelBanListSegments = new Dictionary<int, ParcelAccessListUpdate>();

        private readonly object m_ParcelAllowExperienceListLock = new object();
        private UUID m_ParcelAllowExperienceListTransaction = UUID.Zero;
        private readonly Dictionary<int, ParcelAccessListUpdate> m_ParcelAllowExperienceListSegments = new Dictionary<int, ParcelAccessListUpdate>();

        private readonly object m_ParcelBlockExperienceListLock = new object();
        private UUID m_ParcelBlockExperienceListTransaction = UUID.Zero;
        private readonly Dictionary<int, ParcelAccessListUpdate> m_ParcelBlockExperienceListSegments = new Dictionary<int, ParcelAccessListUpdate>();

        private void ParcelAccessListUpdateManage(UUID parcelID, Dictionary<UUID, ParcelAccessListUpdate.Data> entries, IParcelAccessList accessList)
        {
            foreach (var listed in accessList[Scene.ID, parcelID])
            {
                if (!entries.ContainsKey(listed.Accessor.ID))
                {
                    accessList.Remove(Scene.ID, parcelID, listed.Accessor);
                }
            }
            foreach (var upd in entries.Values)
            {
                UUI uui;
                if (Scene.AvatarNameService.TryGetValue(upd.ID, out uui))
                {
                    var pae = new ParcelAccessEntry
                    {
                        RegionID = Scene.ID,
                        Accessor = uui,
                        ParcelID = parcelID
                    };
                    accessList.Store(pae);
                }
            }
        }

        private void ParcelAccessListUpdateManage(UUID parcelID, Dictionary<UUID, ParcelAccessListUpdate.Data> entries, IParcelExperienceList list, bool setallow)
        {
            IEnumerable<ParcelExperienceEntry> reslist = from entry in list[Scene.ID, parcelID] where (entry.IsAllowed && setallow) || (!entry.IsAllowed && !setallow) select entry;
            foreach (var listed in reslist)
            {
                if (!entries.ContainsKey(listed.ExperienceID))
                {
                    list.Remove(Scene.ID, parcelID, listed.ExperienceID);
                }
            }
            foreach (var upd in entries.Values)
            {
                ExperienceInfo expInfo;
                if (Scene.ExperienceService.TryGetValue(upd.ID, out expInfo))
                {
                    var pae = new ParcelExperienceEntry
                    {
                        RegionID = Scene.ID,
                        ExperienceID = upd.ID,
                        IsAllowed = setallow,
                        ParcelID = parcelID
                    };
                    list.Store(pae);
                }
            }
        }

        [PacketHandler(MessageType.ParcelAccessListUpdate)]
        public void HandleParcelAccessListUpdateRequest(Message m)
        {
            var req = (ParcelAccessListUpdate)m;
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
                        var list = new Dictionary<int, ParcelAccessListUpdate>(m_ParcelAccessListSegments);
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

                        var entries = new Dictionary<UUID, ParcelAccessListUpdate.Data>();
                        foreach (var upd in list.Values)
                        {
                            foreach (var d in upd.AccessList)
                            {
                                entries[d.ID] = d;
                            }
                        }

                        ParcelAccessListUpdateManage(pInfo.ID, entries, Scene.Parcels.WhiteList);
                    }
                }
            }

            if ((pInfo.Owner.EqualsGrid(Agent.Owner) ||
                Scene.HasGroupPower(Agent.Owner, pInfo.Group, Types.Groups.GroupPowers.LandManageBanned)) &&
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
                        var list = new Dictionary<int, ParcelAccessListUpdate>(m_ParcelBanListSegments);
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

                        var entries = new Dictionary<UUID, ParcelAccessListUpdate.Data>();
                        foreach (var upd in list.Values)
                        {
                            foreach (var d in upd.AccessList)
                            {
                                entries[d.ID] = d;
                            }
                        }

                        ParcelAccessListUpdateManage(pInfo.ID, entries, Scene.Parcels.WhiteList);
                    }
                }
            }

            if (pInfo.Owner.EqualsGrid(Agent.Owner) &&
                req.Flags == ParcelAccessList.AllowExperience)
            {
                lock (m_ParcelAllowExperienceListLock)
                {
                    if (m_ParcelAllowExperienceListTransaction != req.TransactionID)
                    {
                        m_ParcelAllowExperienceListSegments.Clear();
                        m_ParcelAllowExperienceListTransaction = req.TransactionID;
                    }

                    m_ParcelAllowExperienceListSegments[req.SequenceID] = req;
                    if (req.Sections == 0)
                    {
                        ParcelAccessListUpdateManage(pInfo.ID, new Dictionary<UUID, ParcelAccessListUpdate.Data>(), Scene.Parcels.Experiences, true);
                    }
                    else if (m_ParcelAllowExperienceListSegments.Count == req.Sections)
                    {
                        var list = new Dictionary<int, ParcelAccessListUpdate>(m_ParcelAllowExperienceListSegments);
                        m_ParcelAllowExperienceListSegments.Clear();
                        m_ParcelAllowExperienceListTransaction = UUID.Zero;
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

                        var entries = new Dictionary<UUID, ParcelAccessListUpdate.Data>();
                        foreach (var upd in list.Values)
                        {
                            foreach (var d in upd.AccessList)
                            {
                                entries[d.ID] = d;
                            }
                        }

                        ParcelAccessListUpdateManage(pInfo.ID, entries, Scene.Parcels.Experiences, true);
                    }
                }
            }

            if (pInfo.Owner.EqualsGrid(Agent.Owner) &&
                req.Flags == ParcelAccessList.BlockExperience)
            {
                lock (m_ParcelBlockExperienceListLock)
                {
                    if (m_ParcelBlockExperienceListTransaction != req.TransactionID)
                    {
                        m_ParcelBlockExperienceListSegments.Clear();
                        m_ParcelBlockExperienceListTransaction = req.TransactionID;
                    }

                    m_ParcelBlockExperienceListSegments[req.SequenceID] = req;
                    if (req.Sections == 0)
                    {
                        ParcelAccessListUpdateManage(pInfo.ID, new Dictionary<UUID, ParcelAccessListUpdate.Data>(), Scene.Parcels.Experiences, false);
                    }
                    else if (m_ParcelBlockExperienceListSegments.Count == req.Sections)
                    {
                        var list = new Dictionary<int, ParcelAccessListUpdate>(m_ParcelBlockExperienceListSegments);
                        m_ParcelBlockExperienceListSegments.Clear();
                        m_ParcelBlockExperienceListTransaction = UUID.Zero;
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

                        var entries = new Dictionary<UUID, ParcelAccessListUpdate.Data>();
                        foreach (var upd in list.Values)
                        {
                            foreach (var d in upd.AccessList)
                            {
                                entries[d.ID] = d;
                            }
                        }

                        ParcelAccessListUpdateManage(pInfo.ID, entries, Scene.Parcels.Experiences, false);
                    }
                }
            }
        }
    }
}
