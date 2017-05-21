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
        public abstract List<NpcPresenceInfo> this[UUID regionID] { get; }

        public abstract bool TryGetValue(UUID regionID, string firstname, string lastname, out NpcPresenceInfo info);

        public abstract bool ContainsKey(UUID npcid);

        public abstract bool TryGetValue(UUID npcid, out NpcPresenceInfo presence);

        public abstract void Store(NpcPresenceInfo presenceInfo);

        public abstract void Remove(UUID scopeID, UUID npcID);
    }
}
