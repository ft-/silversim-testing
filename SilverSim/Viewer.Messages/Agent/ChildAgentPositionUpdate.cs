// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

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

        public static Message Decode(UDPPacket p) => new ChildAgentPositionUpdate
        {
            RegionLocation = new GridVector(p.ReadUInt64()),
            ViewerCircuitCode = p.ReadUInt32(),
            AgentID = p.ReadUUID(),
            SessionID = p.ReadUUID(),
            AgentPosition = p.ReadVector3f(),
            AgentVelocity = p.ReadVector3f(),
            Center = p.ReadVector3f(),
            Size = p.ReadVector3f(),
            AtAxis = p.ReadVector3f(),
            LeftAxis = p.ReadVector3f(),
            UpAxis = p.ReadVector3f(),
            ChangedGrid = p.ReadBoolean()
        };

        public override void Serialize(UDPPacket p)
        {
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
