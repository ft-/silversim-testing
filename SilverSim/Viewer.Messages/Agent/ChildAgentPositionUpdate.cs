// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Agent
{
    [UDPMessage(MessageType.ChildAgentPositionUpdate)]
    [Trusted]
    public class ChildAgentPositionUpdate : Message
    {
        public GridVector RegionLocation;
        public UInt32 ViewerCircuitCode;
        public UUID AgentID;
        public UUID SessionID;
        public Vector3 AgentPosition;
        public Vector3 AgentVelocity;
        public Vector3 Center;
        public Vector3 Size;
        public Vector3 AtAxis;
        public Vector3 LeftAxis;
        public Vector3 UpAxis;
        public bool ChangedGrid;

        public ChildAgentPositionUpdate()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ChildAgentPositionUpdate m = new ChildAgentPositionUpdate();
            m.RegionLocation.RegionHandle = p.ReadUInt64();
            m.ViewerCircuitCode = p.ReadUInt32();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.AgentPosition = p.ReadVector3f();
            m.AgentVelocity = p.ReadVector3f();
            m.Center = p.ReadVector3f();
            m.Size = p.ReadVector3f();
            m.AtAxis = p.ReadVector3f();
            m.LeftAxis = p.ReadVector3f();
            m.UpAxis = p.ReadVector3f();
            m.ChangedGrid = p.ReadBoolean();
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(MessageType.ChildAgentPositionUpdate);
            p.WriteUInt64(RegionLocation.RegionHandle);
            p.WriteUInt32(ViewerCircuitCode);
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteVector3f(AgentPosition);
            p.WriteVector3f(AgentVelocity);
            p.WriteVector3f(Center);
            p.WriteVector3f(Size);
            p.WriteVector3f(AtAxis);
            p.WriteVector3f(LeftAxis);
            p.WriteVector3f(UpAxis);
            p.WriteBoolean(ChangedGrid);
        }
    }
}
