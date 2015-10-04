// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Viewer.Messages.Groups
{
    [UDPMessage(MessageType.UpdateGroupInfo)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class UpdateGroupInfo : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public UUID GroupID;
        public string Charter;
        public bool ShowInList;
        public UUID InsigniaID;
        public int MembershipFee;
        public bool OpenEnrollment;
        public bool AllowPublish;
        public bool MaturePublish;

        public UpdateGroupInfo()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            UpdateGroupInfo m = new UpdateGroupInfo();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            m.Charter = p.ReadStringLen16();
            m.ShowInList = p.ReadBoolean();
            m.InsigniaID = p.ReadUUID();
            m.MembershipFee = p.ReadInt32();
            m.OpenEnrollment = p.ReadBoolean();
            m.AllowPublish = p.ReadBoolean();
            m.MaturePublish = p.ReadBoolean();
            return m;
        }
    }
}
