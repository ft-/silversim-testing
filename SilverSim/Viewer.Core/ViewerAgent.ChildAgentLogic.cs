// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Neighbor;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using SilverSim.Viewer.Messages.Agent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        void ChildUpdateOnPositionChange(IObject own)
        {
            AgentCircuit c;
            if(Circuits.TryGetValue(SceneID, out c))
            {
                SceneInterface scene = c.Scene;
                if(null != scene)
                {
                    foreach (KeyValuePair<UUID, AgentChildInfo> kvp in ActiveChilds)
                    {
                        ChildAgentPositionUpdate m = new ChildAgentPositionUpdate();
                        m.AgentID = ID;
                        m.AgentPosition = GlobalPosition;
                        m.AgentVelocity = Velocity;
                        m.ChangedGrid = false;
                        //m.AtAxis;
                        //m.Center;
                        //m.LeftAxis;
                        m.RegionLocation = scene.RegionData.Location;
                        m.SessionID = SessionID;
                        m.Size = Size;
                        //m.UpAxis;
                        IAgentChildUpdateServiceInterface childUpdater = kvp.Value.ChildAgentUpdateService;
                        if (childUpdater != null)
                        {
                            childUpdater.SendMessage(m);
                        }
                    }
                }
            }
        }

        void ChildUpdateOnParamChange()
        {
            AgentCircuit c;
            if (Circuits.TryGetValue(SceneID, out c))
            {
                SceneInterface scene = c.Scene;
                if (null != scene)
                {
                    foreach (KeyValuePair<UUID, AgentChildInfo> kvp in ActiveChilds)
                    {
                        ChildAgentUpdate m = new ChildAgentUpdate();
                        m.RegionID = scene.ID;
                        m.RegionLocation = scene.RegionData.Location;
                        m.AgentID = m_AgentID;
                        m.SessionID = SessionID;
                        m.AgentPosition = GlobalPosition;
                        m.AgentVelocity = Velocity;
                        //m.Center;
                        m.Size = Size;
                        //m.AtAxis;
                        //m.LeftAxis;
                        //m.UpAxis;
                        m.ChangedGrid = false;
                        m.Far = DrawDistance;
                        m.Aspect = 1;
                        m.Throttles = new byte[9];
                        //m.LocomotionState;
                        m.HeadRotation = HeadRotation;
                        m.BodyRotation = BodyRotation;
                        m.ControlFlags = m_ActiveAgentControlFlags;
                        m.EnergyLevel = 0;
                        m.GodLevel = 0;
                        m.AlwaysRun = m_IsRunning;
                        m.PreyAgent = UUID.Zero;
                        m.AgentAccess = 0;
                        //m.AgentTextures;
                        m.ActiveGroupID = Group.ID;
                        //m.GroupData;
                        //m.AnimationData;
                        //m_AnimationController.
                        //m.GranterBlock;
                        m.VisualParams = VisualParams;
                        //m.AgentAccessList;
                        //m.AgentInfo;
                        IAgentChildUpdateServiceInterface childUpdater = kvp.Value.ChildAgentUpdateService;
                        if (childUpdater != null)
                        {
                            childUpdater.SendMessage(m);
                        }
                    }
                }
            }
        }
    }
}
