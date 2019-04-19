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

using SilverSim.Main.Common;
using SilverSim.Types;
using SilverSim.Types.Grid;
using SilverSim.Types.Parcel;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Agent;
using SilverSim.Viewer.Messages.Circuit;
using SilverSim.Viewer.Messages.God;
using SilverSim.Viewer.Messages.Parcel;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        [PacketHandler(MessageType.RegionHandshakeReply)]
        public void HandleRegionHandshakeReply(Message m)
        {
            var rhr = (Messages.Region.RegionHandshakeReply)m;
            AgentCircuit circuit;
            if (Circuits.TryGetValue(rhr.CircuitSceneID, out circuit))
            {
                var scene = circuit.Scene;
                /* Add our agent to scene */
                scene.SendAllParcelOverlaysTo(this);
                scene.Terrain.UpdateTerrainDataToSingleClient(this);
                scene.Environment.UpdateWindDataToSingleClient(this);
                scene.SendAgentObjectToAllAgents(this);
                scene.SendRegionInfo(this);
                ParcelInfo pinfo;
                if (scene.Parcels.TryGetValue(GlobalPosition, out pinfo))
                {
                    var props = scene.ParcelInfo2ParcelProperties(Owner.ID, pinfo, NextParcelSequenceId, ParcelProperties.RequestResultType.Single);
                    circuit.SendMessage(props);
                }
                circuit.ScheduleFirstUpdate();
            }
        }

        [PacketHandler(MessageType.CompleteAgentMovement)]
        public void HandleCompleteAgentMovement(Message m)
        {
            var cam = (CompleteAgentMovement)m;
            AgentCircuit circuit;
            if (cam.SessionID != cam.CircuitSessionID ||
                cam.AgentID != cam.CircuitAgentID)
            {
                m_Log.InfoFormat("Unexpected CompleteAgentMovement with invalid details");
            }
            else if (Circuits.TryGetValue(cam.CircuitSceneID, out circuit))
            {
                var scene = circuit.Scene;
                if (scene == null)
                {
                    return;
                }

                /* switch agent region */
                if (m_IsActiveGod && !scene.IsPossibleGod(Owner))
                {
                    /* revoke god powers when changing region and new region has a different owner */
                    var gm = new GrantGodlikePowers
                    {
                        AgentID = ID,
                        SessionID = circuit.SessionID,
                        GodLevel = 0,
                        Token = UUID.Zero
                    };
                    SendMessageIfRootAgent(gm, SceneID);
                    m_IsActiveGod = false;
                }
                SceneID = scene.ID;
                scene.TriggerAgentChangedScene(this);

                if (circuit.LastTeleportFlags.NeedsInitialPosition())
                {
                    try
                    {
                        scene.DetermineInitialAgentLocation(this, circuit.LastTeleportFlags, GlobalPosition, LookAt);
                    }
                    catch
                    {
                        /* TODO: how to do this? */
                        return;
                    }
                }

                var amc = new AgentMovementComplete
                {
                    AgentID = cam.AgentID,
                    ChannelVersion = VersionInfo.SimulatorVersion,
                    LookAt = circuit.Agent.LookAt,
                    Position = GlobalPosition,
                    SessionID = cam.SessionID,
                    GridPosition = circuit.Scene.GridPosition,
                    Timestamp = (uint)Date.GetUnixTime()
                };

                amc.OnSendCompletion += (bool success) =>
                {
                    if (success)
                    {
                        HandleChildAgentChanges(circuit);
                    }
                };
                circuit.SendMessage(amc);

                SendAgentDataUpdate(circuit);
                scene.SendAgentObjectToAllAgents(this);

                var clu = new CoarseLocationUpdate
                {
                    You = 0,
                    Prey = -1
                };
                var ad = new CoarseLocationUpdate.AgentDataEntry
                {
                    X = (byte)(uint)GlobalPosition.X,
                    Y = (byte)(uint)GlobalPosition.Y,
                    Z = (byte)(uint)GlobalPosition.Z,
                    AgentID = ID
                };
                clu.AgentData.Add(ad);
                circuit.SendMessage(clu);

                scene.Environment.UpdateWindlightProfileToClientNoReset(this);
                scene.Environment.SendSimulatorTimeMessageToClient(this);

                foreach (var action in circuit.m_TriggerOnRootAgentActions)
                {
                    action.TriggerOnRootAgent(ID, scene);
                }
            }
        }
    }
}
