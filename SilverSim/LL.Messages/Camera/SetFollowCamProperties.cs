// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Camera
{
    [UDPMessage(MessageType.SetFollowCamProperties)]
    [Reliable]
    [Trusted]
    public class SetFollowCamProperties : Message
    {
        public UUID ObjectID = UUID.Zero;

        public struct CameraProperty
        {
            public Int32 Type;
            public double Value;
        }

        public List<CameraProperty> CameraProperties = new List<CameraProperty>();

        public SetFollowCamProperties()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(ObjectID);
            p.WriteUInt8((byte)CameraProperties.Count);
            foreach(CameraProperty d in CameraProperties)
            {
                p.WriteInt32(d.Type);
                p.WriteFloat((float)d.Value);
            }
        }
    }
}
