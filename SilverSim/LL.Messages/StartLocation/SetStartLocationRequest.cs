// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;

namespace SilverSim.LL.Messages.StartLocation
{
    [UDPMessage(MessageType.SetStartLocationRequest)]
    [Reliable]
    [NotTrusted]
    public class SetStartLocationRequest : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public string SimName;
        public UInt32 LocationID;
        public Vector3 LocationPos;
        public Vector3 LocationLookAt;

        public SetStartLocationRequest()
        {

        }

        public static SetStartLocationRequest Decode(UDPPacket p)
        {
            SetStartLocationRequest m = new SetStartLocationRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            m.SimName = p.ReadStringLen8();
            m.LocationID = p.ReadUInt32();
            m.LocationPos = p.ReadVector3f();
            m.LocationLookAt = p.ReadVector3f();

            return m;
        }
    }
}
