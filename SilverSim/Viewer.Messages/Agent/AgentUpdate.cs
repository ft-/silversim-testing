﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Agent;

namespace SilverSim.Viewer.Messages.Agent
{
    [UDPMessage(MessageType.AgentUpdate)]
    [NotTrusted]
    public class AgentUpdate : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public Quaternion BodyRotation;
        public Quaternion HeadRotation;
        public AgentState State;
        public Vector3 CameraCenter;
        public Vector3 CameraAtAxis;
        public Vector3 CameraLeftAxis;
        public Vector3 CameraUpAxis;
        public double Far;
        public ControlFlags ControlFlags;
        public byte Flags;

        public AgentUpdate()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            AgentUpdate m = new AgentUpdate();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.BodyRotation = p.ReadLLQuaternion();
            m.HeadRotation = p.ReadLLQuaternion();
            m.State = (AgentState)p.ReadUInt8();
            m.CameraCenter = p.ReadVector3f();
            m.CameraAtAxis = p.ReadVector3f();
            m.CameraLeftAxis = p.ReadVector3f();
            m.CameraUpAxis = p.ReadVector3f();
            m.Far = p.ReadFloat();
            m.ControlFlags = (ControlFlags)p.ReadUInt32();
            m.Flags = p.ReadUInt8();
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteLLQuaternion(BodyRotation);
            p.WriteLLQuaternion(HeadRotation);
            p.WriteUInt8((byte)State);
            p.WriteVector3f(CameraCenter);
            p.WriteVector3f(CameraAtAxis);
            p.WriteVector3f(CameraLeftAxis);
            p.WriteVector3f(CameraUpAxis);
            p.WriteFloat((float)Far);
            p.WriteUInt32((uint)ControlFlags);
            p.WriteUInt8((byte)Flags);
        }
    }
}
