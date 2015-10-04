// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Groups
{
    [UDPMessage(MessageType.JoinGroupRequest)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class JoinGroupRequest : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID GroupID;

        public JoinGroupRequest()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            JoinGroupRequest m = new JoinGroupRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.GroupID = p.ReadUUID();

            return m;
        }
    }
}
