// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.LL.Messages.Agent
{
    [UDPMessage(MessageType.ChildAgentPositionUpdate)]
    [Trusted]
    public class ChildAgentPositionUpdate : Message
    {
        public UInt64 RegionHandle;
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
    }
}
