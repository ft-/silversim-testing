﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.LL.Messages.Profile
{
    [UDPMessage(MessageType.UpdateUserInfo)]
    [Reliable]
    [NotTrusted]
    public class UpdateUserInfo : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public bool IMViaEmail;
        public string DirectoryVisibility;

        public UpdateUserInfo()
        {

        }

        public static UpdateUserInfo Decode(UDPPacket p)
        {
            UpdateUserInfo m = new UpdateUserInfo();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.IMViaEmail = p.ReadBoolean();
            m.DirectoryVisibility = p.ReadStringLen8();
            return m;
        }
    }
}
