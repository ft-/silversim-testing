// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Friend
{
    [UDPMessage(MessageType.GrantUserRights)]
    [Reliable]
    [NotTrusted]
    public class GrantUserRights : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public struct RightsEntry
        {
            public UUID AgentRelated;
            public Int32 RelatedRights;
        }

        public List<RightsEntry> Rights = new List<RightsEntry>();

        public GrantUserRights()
        {

        }

        public static GrantUserRights Decode(UDPPacket p)
        {
            GrantUserRights m = new GrantUserRights();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            uint c = p.ReadUInt8();
            for(uint i = 0; i < c; ++i)
            {
                RightsEntry d = new RightsEntry();
                d.AgentRelated = p.ReadUUID();
                d.RelatedRights = p.ReadInt32();
                m.Rights.Add(d);
            }

            return m;
        }
    }
}
