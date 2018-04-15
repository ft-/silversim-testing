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

using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Agent;
using SilverSim.Viewer.Messages.Camera;
using SilverSim.Viewer.Messages.Script;
using System.Collections.Generic;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        #region Agent Controls Field
        private ControlFlags m_TakenControls;
        private ControlFlags m_IgnoredControls;
        private ControlFlags m_ActiveAgentControlFlags;
        private bool m_IsRunning;
        private bool m_IsAway;

        public override bool IsAway => m_IsAway;

        public class ScriptControlData
        {
            public ControlFlags Taken;
            public ControlFlags Ignored;

            public ScriptControlData()
            {
            }

            public ScriptControlData(ScriptControlData data)
            {
                Taken = data.Taken;
                Ignored = data.Ignored;
            }
        }

        private readonly Dictionary<ScriptInstance, ScriptControlData> m_ScriptControls = new Dictionary<ScriptInstance, ScriptControlData>();
        #endregion

        public override void TakeControls(ScriptInstance instance, int controls, int accept, int pass_on)
        {
            this[instance] = new ScriptControlData
            {
                Taken = accept != 0 ? (ControlFlags)controls : ControlFlags.None,
                Ignored = pass_on != 0 ? (ControlFlags)controls : ControlFlags.None
            };
        }

        public override void ReleaseControls(ScriptInstance instance)
        {
            this[instance] = null;
        }

        public ScriptControlData this[ScriptInstance instance]
        {
            get
            {
                ScriptControlData data;
                lock (m_ScriptControls)
                {
                    if(m_ScriptControls.TryGetValue(instance, out data))
                    {
                        return new ScriptControlData(data);
                    }
                }
                throw new KeyNotFoundException();
            }
            set
            {
                lock(m_ScriptControls)
                {
                    if (value != null)
                    {
                        m_ScriptControls[instance] = new ScriptControlData(value);
                    }
                    else
                    {
                        m_ScriptControls.Remove(instance);
                    }
                    m_TakenControls = ControlFlags.None;
                    m_IgnoredControls = ControlFlags.None;
                    foreach(var sc in m_ScriptControls.Values)
                    {
                        m_TakenControls |= sc.Taken;
                        m_IgnoredControls |= sc.Ignored;
                    }
                }
                ControlFlags taken = m_TakenControls;
                ControlFlags ignored = m_IgnoredControls;

                var msg = new ScriptControlChange();
                if(taken != ignored)
                {
                    msg.Data.Add(new ScriptControlChange.DataEntry
                    {
                        Controls = taken,
                        PassToAgent = true,
                        TakeControls = true
                    });
                    msg.Data.Add(new ScriptControlChange.DataEntry
                    {
                        Controls = ignored,
                        PassToAgent = false,
                        TakeControls = true
                    });
                }
                else if(taken != ControlFlags.None)
                {
                    msg.Data.Add(new ScriptControlChange.DataEntry
                    {
                        Controls = taken,
                        PassToAgent = true,
                        TakeControls = true
                    });
                }
                else
                {
                    msg.Data.Add(new ScriptControlChange.DataEntry
                    {
                        Controls = ControlFlags.None,
                        PassToAgent = true,
                        TakeControls = false
                    });
                }
                SendMessageAlways(msg, SceneID);
            }
        }

        #region Script Controls
        public ControlFlags TakenControls => m_TakenControls;

        public ControlFlags IgnoredControls => m_IgnoredControls;
        #endregion

        [PacketHandler(MessageType.ForceScriptControlRelease)]
        public void HandleForceScriptControlRelease(Message m)
        {
            var req = (ForceScriptControlRelease)m;
            if(req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            lock (m_ScriptControls)
            {
                m_ScriptControls.Clear();
                m_TakenControls = ControlFlags.None;
                m_IgnoredControls = ControlFlags.None;
            }
        }

        private void ProcessAgentControls()
        {
            var agentControlFlags = m_ActiveAgentControlFlags & (~IgnoredControls);
            var agentMovementDirection = Vector3.Zero;

            m_IsFlying = agentControlFlags.HasFly() && SittingOnObject == null;
            m_IsAway = agentControlFlags.HasAway();

            if (SittingOnObject != null)
            {
                /* agent is sitting on object */
            }
            else if (agentControlFlags.HasStop())
            {
            }
            else
            {
                if (agentControlFlags.HasForward())
                {
                    agentMovementDirection += Vector3.UnitX;
                }
                if (agentControlFlags.HasBack())
                {
                    agentMovementDirection -= Vector3.UnitX;
                }
                if (agentControlFlags.HasUp())
                {
                    agentMovementDirection += Vector3.UnitZ;
                }
                if (agentControlFlags.HasDown())
                {
                    agentMovementDirection -= Vector3.UnitZ;
                }
                if (agentControlFlags.HasForwardNudge())
                {
                    agentMovementDirection += Vector3.UnitX * 0.5;
                }
                if (agentControlFlags.HasBackwardNudge())
                {
                    agentMovementDirection -= Vector3.UnitX * 0.5;
                }
                if (agentControlFlags.HasLeft())
                {
                    agentMovementDirection += Vector3.UnitY;
                }
                if (agentControlFlags.HasRight())
                {
                    agentMovementDirection -= Vector3.UnitY;
                }
                if (agentControlFlags.HasLeftNudge())
                {
                    agentMovementDirection += Vector3.UnitY * 0.5;
                }
                if (agentControlFlags.HasRightNudge())
                {
                    agentMovementDirection -= Vector3.UnitY * 0.5;
                }
                if (agentControlFlags.HasUpNudge())
                {
                    agentMovementDirection += Vector3.UnitY * 0.5;
                }
                if (agentControlFlags.HasDownNudge())
                {
                    agentMovementDirection -= Vector3.UnitY * 0.5;
                }
            }

            agentMovementDirection *= BodyRotation; /* adjust directional vector */
            if(IsFlying)
            {
                agentMovementDirection *= 5f;
            }
            else if (m_IsRunning)
            {
                agentMovementDirection *= 1.6f;
            }
            if(IsAvatarFreezed)
            {
                agentMovementDirection = Vector3.Zero;
            }
            ((IAgentPhysicsObject)PhysicsActor).SetControlDirectionalInput(agentMovementDirection);
            ((IAgentPhysicsObject)PhysicsActor).SetControlFlags(agentControlFlags);
        }

        [PacketHandler(MessageType.SetAlwaysRun)]
        public void HandleSetAlwaysRun(Message m)
        {
            var sar = (SetAlwaysRun)m;

            if (sar.AgentID != sar.CircuitAgentID ||
                sar.SessionID != sar.CircuitSessionID)
            {
                return;
            }

            if (sar.CircuitSceneID != SceneID)
            {
                return;
            }

            m_IsRunning = sar.AlwaysRun;

            ProcessAgentControls();
        }

        public override bool IsRunning => m_IsRunning;

        private bool m_IsFlying;
        public override bool IsFlying => m_IsFlying;

        [PacketHandler(MessageType.AgentUpdate)]
        public void HandleAgentUpdateMessage(Message m)
        {
            /* only AgentUpdate is passed here */
            var au = (AgentUpdate)m;

            if(au.AgentID != au.CircuitAgentID ||
                au.SessionID != au.CircuitSessionID)
            {
                return;
            }

            if (au.CircuitSceneID != SceneID)
            {
                return;
            }

            /* this is for the root agent */
            ControlFlags knownScriptControls = TakenControls;
            ControlFlags edge = m_ActiveAgentControlFlags ^ au.ControlFlags;
            m_ActiveAgentControlFlags = au.ControlFlags;
            DrawDistance = au.Far;

            if (m_ActiveAgentControlFlags.HasStandUp() &&
                SittingOnObject != null && AllowUnsit)
            {
                UnSit();
            }

            if (m_ActiveAgentControlFlags.HasSitOnGround())
            {

            }

            HeadRotation = au.HeadRotation;
            BodyRotation = au.BodyRotation;
            CameraPosition = au.CameraCenter;
            CameraAtAxis = au.CameraAtAxis;
            CameraLeftAxis = au.CameraLeftAxis;
            CameraUpAxis = au.CameraUpAxis;
            ProcessAgentControls();

            Vector4 camCollisionPlane;
            TryGetCameraConstraints(CameraPosition, out camCollisionPlane);
            SendMessageIfRootAgent(new CameraConstraint { CameraCollidePlane = camCollisionPlane }, au.SessionID);

            if (knownScriptControls != ControlFlags.None)
            {
                Dictionary<ScriptInstance, ScriptControlData> copy;
                lock(m_ScriptControls)
                {
                    copy = new Dictionary<ScriptInstance, ScriptControlData>(m_ScriptControls);
                }

                foreach(var kvp in copy)
                {
                    var ce = new ControlEvent
                    {
                        Level = (int)(m_ActiveAgentControlFlags & kvp.Value.Taken),
                        Flags = (int)(edge & kvp.Value.Taken)
                    };
                    kvp.Key.PostEvent(ce);
                }
            }
        }
    }
}
