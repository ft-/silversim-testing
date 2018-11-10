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

#pragma warning disable IDE0018
#pragma warning disable RCS1029

using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using SilverSim.Viewer.Messages.Agent;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        private void ChildUpdateOnPositionChange(IObject own)
        {
            AgentCircuit c;
            if(Circuits.TryGetValue(SceneID, out c))
            {
                var scene = c.Scene;
                if(scene != null)
                {
                    foreach (var kvp in ActiveChilds)
                    {
                        var m = new ChildAgentPositionUpdate
                        {
                            AgentID = ID,
                            AgentPosition = GlobalPosition,
                            AgentVelocity = Velocity,
                            ChangedGrid = false,
                            AtAxis = CameraAtAxis,
                            Center = CameraPosition,
                            LeftAxis = CameraLeftAxis,
                            RegionLocation = scene.GridPosition,
                            SessionID = SessionID,
                            Size = Size,
                            UpAxis = CameraUpAxis
                        };
                        var childUpdater = kvp.Value.ChildAgentUpdateService;
                        childUpdater?.SendMessage(m);
                    }
                }
            }
        }

        private void ChildUpdateOnParamChange()
        {
            AgentCircuit c;
            if (Circuits.TryGetValue(SceneID, out c))
            {
                var scene = c.Scene;
                if (scene != null)
                {
                    foreach (var kvp in ActiveChilds)
                    {
                        var m = new ChildAgentUpdate
                        {
                            RegionID = scene.ID,
                            RegionLocation = scene.GridPosition,
                            AgentID = ID,
                            SessionID = SessionID,
                            AgentPosition = GlobalPosition,
                            AgentVelocity = Velocity,
                            Center = CameraPosition,
                            Size = Size,
                            AtAxis = CameraAtAxis,
                            LeftAxis = CameraLeftAxis,
                            UpAxis = CameraUpAxis,
                            ChangedGrid = false,
                            Far = m_DrawDistance,
                            Aspect = 1,
                            Throttles = new byte[9],
                            //m.LocomotionState;
                            HeadRotation = HeadRotation,
                            BodyRotation = BodyRotation,
                            ControlFlags = m_ActiveAgentControlFlags,
                            EnergyLevel = 0,
                            GodLevel = 0,
                            AlwaysRun = m_IsRunning,
                            PreyAgent = UUID.Zero,
                            AgentAccess = 0,
                            //m.AgentTextures;
                            ActiveGroupID = Group.ID,
                            //m.GroupData;
                            //m.AnimationData;
                            //m_AnimationController.
                            //m.GranterBlock;
                            VisualParams = VisualParams
                        };
                        //m.AgentAccessList;
                        //m.AgentInfo;
                        var childUpdater = kvp.Value.ChildAgentUpdateService;
                        childUpdater?.SendMessage(m);
                    }
                }
            }
        }
    }
}
