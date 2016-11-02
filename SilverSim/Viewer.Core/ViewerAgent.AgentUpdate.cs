// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Viewer.Messages;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        #region Agent Controls Field
        ControlFlags m_TakenControls;
        ControlFlags m_IgnoredControls;
        ControlFlags m_ActiveAgentControlFlags;
        bool m_IsRunning;
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
        readonly Dictionary<ScriptInstance, ScriptControlData> m_ScriptControls = new Dictionary<ScriptInstance, ScriptControlData>();
        #endregion

        public override void TakeControls(ScriptInstance instance, int controls, int accept, int pass_on)
        {
            ScriptControlData data = new ScriptControlData();
            data.Taken = accept != 0 ? (ControlFlags)controls : ControlFlags.None;
            data.Ignored = pass_on != 0 ? (ControlFlags)controls : ControlFlags.None;
            this[instance] = data;
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
                    if (null != value)
                    {
                        m_ScriptControls[instance] = new ScriptControlData(value);
                    }
                    else
                    {
                        m_ScriptControls.Remove(instance);
                    }
                    m_TakenControls = ControlFlags.None;
                    m_IgnoredControls = ControlFlags.None;
                    foreach(ScriptControlData sc in m_ScriptControls.Values)
                    {
                        m_TakenControls |= sc.Taken;
                        m_IgnoredControls |= sc.Ignored;
                    }
                }
            }
        }

        #region Script Controls
        public ControlFlags TakenControls
        {
            get
            {
                return m_TakenControls;
            }
        }

        public ControlFlags IgnoredControls
        {
            get
            {
                return m_IgnoredControls;
            }
        }
        #endregion

        void ProcessAgentControls()
        {
            ControlFlags agentControlFlags = m_ActiveAgentControlFlags & (~IgnoredControls);
            Vector3 agentMovementDirection = Vector3.Zero;

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
            if (m_IsRunning)
            {
                agentMovementDirection *= 1.5f;
            }
            ((IAgentPhysicsObject)PhysicsActor).SetControlTargetVelocity(agentMovementDirection);
        }

        [PacketHandler(MessageType.SetAlwaysRun)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleSetAlwaysRun(Message m)
        {
            Messages.Agent.SetAlwaysRun sar = (Messages.Agent.SetAlwaysRun)m;

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

        [PacketHandler(MessageType.AgentUpdate)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleAgentUpdateMessage(Message m)
        {
            /* only AgentUpdate is passed here */
            Messages.Agent.AgentUpdate au = (Messages.Agent.AgentUpdate)m;

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

            if (m_ActiveAgentControlFlags.HasStandUp())
            {
                if (SittingOnObject != null && AllowUnsit)
                {
                    UnSit();
                }
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

            if (knownScriptControls != ControlFlags.None)
            {
                Dictionary<ScriptInstance, ScriptControlData> copy;
                lock(m_ScriptControls)
                {
                    copy = new Dictionary<ScriptInstance, ScriptControlData>(m_ScriptControls);
                }

                foreach(KeyValuePair<ScriptInstance, ScriptControlData> kvp in copy)
                {
                    ControlEvent ce = new ControlEvent();
                    ce.Level = (int)(m_ActiveAgentControlFlags & kvp.Value.Taken);
                    ce.Flags = (int)(edge & kvp.Value.Taken);
                    kvp.Key.PostEvent(ce);
                }
            }
        }
    }
}
