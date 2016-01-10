// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Friend
{
    [UDPMessage(MessageType.ChangeUserRights)]
    [Reliable]
    [Trusted]
    public class ChangeUserRights : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public struct RightsEntry
        {
            public UUID AgentRelated;
            public Int32 RelatedRights;
        }

        public List<RightsEntry> Rights = new List<RightsEntry>();

        public ChangeUserRights()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUInt8((byte)Rights.Count);
            foreach(RightsEntry d in Rights)
            {
                p.WriteUUID(d.AgentRelated);
                p.WriteInt32(d.RelatedRights);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            ChangeUserRights m = new ChangeUserRights();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            uint n = p.ReadUInt8();
            for (uint i = 0; i < n; ++i)
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
