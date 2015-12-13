// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Generic
{
    [UDPMessage(MessageType.EstateOwnerMessage)]
    [Reliable]
    [NotTrusted]
    public class EstateOwnerMessage : GenericMessageFormat
    {
        public EstateOwnerMessage()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            return Decode(p, new EstateOwnerMessage());
        }
    }

    [Flags]
    public enum EstateAccessDeltaFlags
    {
        AllEstates = 1 << 0,
        AddUser = 1 << 2,
        RemoveUser = 1 << 3,
        AddGroup = 1 << 4,
        RemoveGroup = 1 << 5,
        AddBan = 1 << 6,
        RemoveBan = 1 << 7,
        AddManager = 1 << 8,
        RemoveManager = 1 << 9,
    }
}
