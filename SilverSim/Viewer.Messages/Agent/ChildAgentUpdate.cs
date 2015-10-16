// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Groups;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Agent
{
    [Trusted]
    public class ChildAgentUpdate : Message
    {
        #region Extra Fields not communicated in LL message
        public UUID RegionID;
        #endregion

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

        public double Far;
        public double Aspect;
        public byte[] Throttles;
        public UInt32 LocomotionState;
        public Quaternion HeadRotation;
        public Quaternion BodyRotation;
        public ControlFlags ControlFlags;
        public double EnergyLevel;
        public byte GodLevel;
        public bool AlwaysRun;
        public UUID PreyAgent;
        public byte AgentAccess;
        public List<UUID> AgentTextures = new List<UUID>();
        public UUID ActiveGroupID;

        public struct GroupDataEntry
        {
            public UUID GroupID;
            public GroupPowers GroupPowers;
            public bool AcceptNotices;
        }

        public List<GroupDataEntry> GroupData = new List<GroupDataEntry>();

        public struct AnimationDataEntry
        {
            public UUID Animation;
            public UUID ObjectID;
        }

        public List<AnimationDataEntry> AnimationData = new List<AnimationDataEntry>();

        public List<UUID> GranterBlock = new List<UUID>();

        public byte[] VisualParams = new byte[0];

        public struct AgentAccessEntry
        {
            public byte AgentLegacyAccess;
            public byte AgentMaxAccess;
        }

        public List<AgentAccessEntry> AgentAccessList = new List<AgentAccessEntry>();

        public List<UInt32> AgentInfo = new List<UInt32>();

        public ChildAgentUpdate()
        {

        }

        public override MessageType Number
        {
            get
            {
                return MessageType.ChildAgentUpdate;
            }
        }
    }
}
