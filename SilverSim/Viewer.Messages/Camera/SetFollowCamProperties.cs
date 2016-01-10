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
            p.WriteUUID(ObjectID);
            p.WriteUInt8((byte)CameraProperties.Count);
            foreach(CameraProperty d in CameraProperties)
            {
                p.WriteInt32(d.Type);
                p.WriteFloat((float)d.Value);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            SetFollowCamProperties m = new SetFollowCamProperties();
            m.ObjectID = p.ReadUUID();
            uint n = p.ReadUInt8();
            for(uint i = 0; i < n; ++i)
            {
                CameraProperty d = new CameraProperty();
                d.Type = p.ReadInt32();
                d.Value = p.ReadFloat();
                m.CameraProperties.Add(d);
            }
            return m;
        }
    }
}
