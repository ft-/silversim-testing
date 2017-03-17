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
