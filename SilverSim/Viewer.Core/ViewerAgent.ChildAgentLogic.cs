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

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Neighbor;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using SilverSim.Viewer.Messages.Agent;
using System.Collections.Generic;

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
                        m.AtAxis = CameraAtAxis;
                        m.Center = CameraPosition;
                        m.LeftAxis = CameraLeftAxis;
                        m.RegionLocation = scene.GridPosition;
                        m.SessionID = SessionID;
                        m.Size = Size;
                        m.UpAxis = CameraUpAxis;
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
                        m.RegionLocation = scene.GridPosition;
                        m.AgentID = ID;
                        m.SessionID = SessionID;
                        m.AgentPosition = GlobalPosition;
                        m.AgentVelocity = Velocity;
                        m.Center = CameraPosition;
                        m.Size = Size;
                        m.AtAxis = CameraAtAxis;
                        m.LeftAxis = CameraLeftAxis;
                        m.UpAxis = CameraUpAxis;
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
