// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.LL.Messages.Avatar
{
    [UDPMessage(MessageType.AvatarSitResponse)]
    [Reliable]
    [Trusted]
    public class AvatarSitResponse : Message
    {
        public UUID SitObject = UUID.Zero;
        public bool IsAutoPilot = false;
        public Vector3 SitPosition = Vector3.Zero;
        public Quaternion SitRotation = Quaternion.Identity;
        public Vector3 CameraEyeOffset = Vector3.Zero;
        public Vector3 CameraAtOffset = Vector3.Zero;
        public bool ForceMouselook = false;

        public AvatarSitResponse()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(SitObject);
            p.WriteBoolean(IsAutoPilot);
            p.WriteVector3f(SitPosition);
            p.WriteLLQuaternion(SitRotation);
            p.WriteVector3f(CameraEyeOffset);
            p.WriteVector3f(CameraAtOffset);
            p.WriteBoolean(ForceMouselook);
        }
    }
}
