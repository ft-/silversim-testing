// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.Presence
{
    [Flags]
    public enum NpcOptions
    {
        None = 0,
        SenseAsAgent = 1,
        Persistent = 2
    }

    public struct NpcPresenceInfo
    {
        public UUI Npc;
        public UUI Owner;
        public UGI Group;
        public NpcOptions Options;
        public UUID RegionID;
        public Vector3 Position;
        public Vector3 LookAt;
        public UUID SittingOnObjectID;
    }

    public abstract class NpcPresenceServiceInterface
    {
        public NpcPresenceServiceInterface()
        {

        }

        public abstract List<NpcPresenceInfo> this[UUID regionID]
        {
            get;
        }

        public abstract bool ContainsKey(UUID npcid);

        public abstract bool TryGetValue(UUID npcid, out NpcPresenceInfo presence);

        public abstract void Store(NpcPresenceInfo presenceInfo);

        public abstract void Remove(UUID scopeID, UUID npcID);
    }
}
