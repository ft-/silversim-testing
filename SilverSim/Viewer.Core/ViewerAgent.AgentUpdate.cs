// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Viewer.Messages;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using SilverSim.Types.Agent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        #region Agent Controls Field
        ControlFlags m_TakenControls;
        ControlFlags m_IgnoredControls;
        Quaternion m_HeadRotation = Quaternion.Identity;
        Quaternion m_BodyRotation = Quaternion.Identity;
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
                    m_ScriptControls[instance] = new ScriptControlData(value);
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

        public Quaternion HeadRotation
        {
            get
            {
                lock(this)
                {
                    return m_HeadRotation;
                }
            }
            set
            {
                lock (this)
                {
                    m_HeadRotation = value;
                }
            }
        }

        public Quaternion BodyRotation
        {
            get
            {
                lock(this)
                {
                    return m_BodyRotation;
                }
            }
            set
            {
                lock(this)
                {
                    m_BodyRotation = value;
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
            ((IAgentPhysicsObject)PhysicsActor).ControlTargetVelocity = agentMovementDirection;
        }

        [PacketHandler(MessageType.SetAlwaysRun)]
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

            }

            if (m_ActiveAgentControlFlags.HasSitOnGround())
            {

            }

            HeadRotation = au.HeadRotation;
            BodyRotation = au.BodyRotation;
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
