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

using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using SilverSim.Types.Grid;
using SilverSim.Viewer.Core;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Teleport;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Viewer.Teleport
{
    [Description("Viewer Teleport Handler")]
    public class ViewerTeleport : IPlugin, IPacketHandlerExtender
    {
        private static readonly ILog m_Log = LogManager.GetLogger("VIEWER TELEPORT");
        private SceneList m_Scenes;

        public void Startup(ConfigurationLoader loader)
        {
            m_Scenes = loader.Scenes;
        }

        private bool TryGetAgentCircuit(Message m, out ViewerAgent vagent, out AgentCircuit circuit)
        {
            SceneInterface scene;
            IAgent agent;

            vagent = default(ViewerAgent);
            circuit = default(AgentCircuit);

            if(m_Scenes.TryGetValue(m.CircuitSceneID, out scene) &&
                scene.Agents.TryGetValue(m.CircuitAgentID, out agent))
            {
                vagent = agent as ViewerAgent;
                if(vagent == null)
                {
                    return false;
                }

                return vagent.Circuits.TryGetValue(scene.ID, out circuit);
            }

            return false;
        }

        [PacketHandler(MessageType.StartLure)]
        public void HandleStartLure(Message m)
        {
            var req = (StartLure)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            m_Log.Warn("Implement StartLure");
        }

        [PacketHandler(MessageType.TeleportLureRequest)]
        public void HandleTeleportLureRequest(Message m)
        {
            var req = (TeleportLureRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            m_Log.Warn("Implement TeleportLureRequest");
        }

        [PacketHandler(MessageType.TeleportCancel)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public void HandleTeleportCancel(Message m)
        {
            var req = (TeleportCancel)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            m_Log.Warn("Implement TeleportCancel");
        }

        [PacketHandler(MessageType.TeleportLandmarkRequest)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public void HandleTeleportLandmarkRequest(Message m)
        {
            var req = (TeleportLandmarkRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            m_Log.Warn("Implement TeleportLandmarkRequest");
        }

        [PacketHandler(MessageType.TeleportLocationRequest)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public void HandleTeleportLocationRequest(Message m)
        {
            var req = (TeleportLocationRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            AgentCircuit circuit;
            ViewerAgent agent;

            if(TryGetAgentCircuit(m, out agent, out circuit))
            {
                RegionInfo hgRegionInfo;

                /* check whether HG destination is addressed */
                if (agent.TryGetDestination(req.GridPosition, out hgRegionInfo))
                {
                    if (!agent.TeleportTo(circuit.Scene, hgRegionInfo.GridURI, hgRegionInfo.ID, req.Position, req.LookAt, TeleportFlags.ViaLocation))
                    {
                        var failedmsg = new TeleportFailed()
                        {
                            AgentID = agent.ID,
                            Reason = agent.GetLanguageString(agent.CurrentCulture, "TeleportNotPossibleToRegion", "Teleport to region not possible")
                        };
                        agent.SendMessageAlways(failedmsg, req.CircuitSceneID);
                    }
                }
                else if (!agent.TeleportTo(circuit.Scene, req.GridPosition, req.Position, req.LookAt, TeleportFlags.ViaLocation))
                {
                    var failedmsg = new TeleportFailed()
                    {
                        AgentID = agent.ID,
                        Reason = this.GetLanguageString(agent.CurrentCulture, "TeleportNotPossibleToRegion", "Teleport to region not possible")
                    };
                    agent.SendMessageAlways(failedmsg, req.CircuitSceneID);
                }
            }
        }

        [PacketHandler(MessageType.TeleportRequest)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public void HandleTeleportRequest(Message m)
        {
            var req = (TeleportRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            AgentCircuit circuit;
            ViewerAgent agent;

            if (TryGetAgentCircuit(m, out agent, out circuit))
            {
                /* TODO: we need the specific local list for HG destinations */
                if (!agent.TeleportTo(circuit.Scene, req.RegionID, req.Position, req.LookAt, TeleportFlags.ViaLocation))
                {
                    var failedmsg = new TeleportFailed()
                    {
                        AgentID = agent.ID,
                        Reason = agent.GetLanguageString(agent.CurrentCulture, "TeleportNotPossibleToRegion", "Teleport to region not possible")
                    };
                    agent.SendMessageAlways(failedmsg, req.CircuitSceneID);
                }
            }
        }
    }

    [PluginName("ViewerTeleport")]
    public class Factory : IPluginFactory
    {
        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection) =>
            new ViewerTeleport();
    }
}
