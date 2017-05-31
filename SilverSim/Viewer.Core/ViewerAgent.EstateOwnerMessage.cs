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

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Estate;
using SilverSim.Types.Grid;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Generic;
using SilverSim.Viewer.Messages.Land;
using SilverSim.Viewer.Messages.LayerData;
using SilverSim.Viewer.Messages.Telehub;
using SilverSim.Viewer.Messages.Transfer;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        private byte[] StringToBytes(string s) => (s + "\0").ToUTF8Bytes();

        [PacketHandler(MessageType.EstateOwnerMessage)]
        public void HandleEstateOwnerMessage(Message m)
        {
            var req = (EstateOwnerMessage)m;
            if(req.SessionID != SessionID ||
                req.AgentID != ID)
            {
                return;
            }

            AgentCircuit circuit;
            if(!Circuits.TryGetValue(req.CircuitSceneID, out circuit))
            {
                return;
            }

            if(!circuit.Scene.IsEstateManager(Owner) && circuit.Scene.Owner != Owner)
            {
                /* only RO, EO and EM allowed behind */
                /* some messages later will be limited further */
                return;
            }

            switch(req.Method)
            {
                case "getinfo":
                    EstateOwner_GetInfo(circuit, req);
                    break;

                case "setregioninfo":
                    EstateOwner_SetRegionInfo(circuit, req);
                    break;

#if TEXTUREBASE
                case "texturebase":
                    break;
#endif

                case "texturedetail":
                    EstateOwner_TextureDetail(circuit, req);
                    break;

                case "textureheights":
                    EstateOwner_TextureHeights(circuit, req);
                    break;

                case "texturecommit":
                    EstateOwner_TextureCommit(circuit, req);
                    break;

                case "setregionterrain":
                    EstateOwner_SetRegionTerrain(circuit, req);
                    break;

                case "restart":
                    EstateOwner_Restart(circuit, req);
                    break;

                case "estatechangecovenantid":
                    if(!circuit.Scene.IsEstateManager(Owner))
                    {
                        /* only EO and EM */
                        return;
                    }
                    EstateOwner_EstateChangeCovenantId(circuit, req);
                    break;

                case "estateaccessdelta":
                    if (!circuit.Scene.IsEstateManager(Owner))
                    {
                        /* only EO and EM */
                        return;
                    }
                    EstateOwner_EstateAccessDelta(circuit, req);
                    break;

                case "simulatormessage":
                    EstateOwner_SimulatorMessage(circuit, req);
                    break;

                case "instantmessage":
                    EstateOwner_InstantMessage(circuit, req);
                    break;

                case "setregiondebug":
                    EstateOwner_SetRegionDebug(circuit, req);
                    break;

                case "teleporthomeuser":
                    EstateOwner_TeleportHomeUser(circuit, req);
                    break;

                case "teleporthomeallusers":
                    EstateOwner_TeleportHomeAllUsers(circuit, req);
                    break;

                case "colliders":
                    EstateOwner_Colliders(circuit, req);
                    break;

                case "scripts":
                    EstateOwner_Scripts(circuit, req);
                    break;

                case "terrain":
                    EstateOwner_Terrain(circuit, req);
                    break;

                case "estatechangeinfo":
                    if (!circuit.Scene.IsEstateManager(Owner))
                    {
                        /* only EO and EM */
                        return;
                    }
                    EstateOwner_EstateChangeInfo(circuit, req);
                    break;

                case "telehub":
                    EstateOwner_Telehub(circuit, req);
                    break;

                case "kickestate":
                    EstateOwner_KickEstate(circuit, req);
                    break;

                default:
                    m_Log.DebugFormat("EstateOwnerMessage: Unknown method {0} requested", req.Method);
                    break;
            }
        }

        private static readonly EstateAccessFlags[] m_OrderedAccessFlags = new EstateAccessFlags[4] {
            EstateAccessFlags.AllowedAgents,
            EstateAccessFlags.AllowedGroups,
            EstateAccessFlags.BannedAgents,
            EstateAccessFlags.Managers
        };

        private void SendEstateList(UUID transactionID, UUID invoice, EstateAccessFlags code, List<UUI> data, uint estateID, UUID fromSceneID)
        {
            int i = 0;
            while(i < data.Count)
            {
                int remaining = data.Count - i;
                if(remaining > 50)
                {
                    remaining = 50;
                }

                var msg = new EstateOwnerMessage()
                {
                    TransactionID = transactionID,
                    Invoice = invoice,
                    AgentID = Owner.ID,
                    SessionID = SessionID,
                    Method = "setaccess"
                };
                msg.ParamList.Add(StringToBytes(estateID.ToString()));
                msg.ParamList.Add(StringToBytes(((uint)code).ToString()));
                foreach(var flag in m_OrderedAccessFlags)
                {
                    if(code == flag)
                    {
                        msg.ParamList.Add(StringToBytes(remaining.ToString()));
                    }
                    else
                    {
                        msg.ParamList.Add(StringToBytes("0"));
                    }
                }
                while(remaining-- != 0)
                {
                    msg.ParamList.Add(data[i++].ID.GetBytes());
                }
                SendMessageIfRootAgent(msg, fromSceneID);
            }
        }

        /* this is groups only, so no code check inside */
        private void SendEstateList(UUID transactionID, UUID invoice, List<UGI> data, uint estateID, UUID fromSceneID)
        {
            int i = 0;
            while(i < data.Count)
            {
                int remaining = data.Count - i;
                if (remaining > 50)
                {
                    remaining = 50;
                }

                var msg = new EstateOwnerMessage()
                {
                    TransactionID = transactionID,
                    Invoice = invoice,
                    AgentID = Owner.ID,
                    SessionID = SessionID,
                    Method = "setaccess"
                };
                msg.ParamList.Add(StringToBytes(estateID.ToString()));
                msg.ParamList.Add(StringToBytes(((uint)EstateAccessFlags.AllowedGroups).ToString()));
                msg.ParamList.Add(StringToBytes("0"));
                msg.ParamList.Add(StringToBytes(remaining.ToString()));
                msg.ParamList.Add(StringToBytes("0"));
                msg.ParamList.Add(StringToBytes("0"));
                while (remaining-- != 0)
                {
                    msg.ParamList.Add(data[i++].ID.GetBytes());
                }
                SendMessageIfRootAgent(msg, fromSceneID);
            }
        }

        private void EstateOwner_GetInfo(AgentCircuit circuit, EstateOwnerMessage req)
        {
            var scene = circuit.Scene;
            uint estateID;
            EstateInfo estateInfo;
            if(!scene.EstateService.RegionMap.TryGetValue(scene.ID, out estateID) ||
                !scene.EstateService.TryGetValue(estateID, out estateInfo))
            {
                return;
            }

            SendEstateList(
                req.TransactionID,
                req.Invoice,
                EstateAccessFlags.Managers,
                scene.EstateService.EstateManager.All[estateID],
                estateID,
                req.CircuitSceneID);
            SendEstateList(
                req.TransactionID,
                req.Invoice,
                EstateAccessFlags.AllowedAgents,
                scene.EstateService.EstateAccess.All[estateID],
                estateID,
                req.CircuitSceneID);
            SendEstateList(
                req.TransactionID,
                req.Invoice,
                scene.EstateService.EstateGroup.All[estateID],
                estateID,
                req.CircuitSceneID);
            SendEstateList(
                req.TransactionID,
                req.Invoice,
                EstateAccessFlags.BannedAgents,
                scene.EstateService.EstateBans.All[estateID],
                estateID,
                req.CircuitSceneID);

            SendEstateUpdateInfo(req.Invoice, req.TransactionID, estateInfo, req.CircuitSceneID);
        }

        public override void SendEstateUpdateInfo(UUID invoice, UUID transactionID, EstateInfo estate, UUID fromSceneID, bool sendToAgentOnly = true)
        {
            var msg = new EstateOwnerMessage()
            {
                AgentID = Owner.ID,
                SessionID = SessionID,
                Invoice = invoice,
                TransactionID = transactionID,
                Method = "estateupdateinfo"
            };
            msg.ParamList.Add(StringToBytes(estate.Name));
            msg.ParamList.Add(StringToBytes((string)estate.Owner.ID));
            msg.ParamList.Add(StringToBytes(estate.ID.ToString()));
            msg.ParamList.Add(StringToBytes(((uint)estate.Flags).ToString()));
            if (estate.UseGlobalTime)
            {
                msg.ParamList.Add(StringToBytes("0"));
            }
            else
            {
                msg.ParamList.Add(StringToBytes(((int)((estate.SunPosition + 6) * 1024)).ToString()));
            }
            msg.ParamList.Add(StringToBytes(estate.ParentEstateID.ToString()));
            msg.ParamList.Add(StringToBytes(estate.CovenantID.ToString()));
            msg.ParamList.Add(StringToBytes(estate.CovenantTimestamp.AsULong.ToString()));
            msg.ParamList.Add(StringToBytes(sendToAgentOnly?"1":"0"));
            msg.ParamList.Add(StringToBytes(estate.AbuseEmail));

            SendMessageIfRootAgent(msg, fromSceneID);
        }

        private void EstateOwner_SetRegionInfo(AgentCircuit circuit, EstateOwnerMessage req)
        {
            if(req.ParamList.Count < 9)
            {
                return;
            }

            var scene = circuit.Scene;

            scene.RegionSettings.BlockTerraform = ParamStringToBool(req.ParamList[0]);
            scene.RegionSettings.BlockFly = ParamStringToBool(req.ParamList[1]);
            scene.RegionSettings.AllowDamage = ParamStringToBool(req.ParamList[2]);
            scene.RegionSettings.AllowLandResell = ParamStringToBool(req.ParamList[3]);
            scene.RegionSettings.AgentLimit = (int)decimal.Parse(req.ParamList[4].FromUTF8Bytes(), CultureInfo.InvariantCulture);
            scene.RegionSettings.ObjectBonus = float.Parse(req.ParamList[5].FromUTF8Bytes(), CultureInfo.InvariantCulture);
            scene.Access = (RegionAccess)int.Parse(req.ParamList[6].FromUTF8Bytes());
            scene.RegionSettings.RestrictPushing = ParamStringToBool(req.ParamList[7]);
            scene.RegionSettings.AllowLandJoinDivide = ParamStringToBool(req.ParamList[8]);
#if DEBUG
            m_Log.DebugFormat("RegionFlags={0} Access={1} AgentLimit={2} ObjectBonus={3}",
                scene.RegionSettings.AsFlags.ToString(),
                scene.Access,
                scene.RegionSettings.AgentLimit,
                scene.RegionSettings.ObjectBonus);
#endif

            scene.ReregisterRegion();
            scene.TriggerRegionSettingsChanged();
        }

        private void EstateOwner_TextureDetail(AgentCircuit circuit, EstateOwnerMessage req)
        {
            bool settingsChanged = false;

            var scene = circuit.Scene;
            try
            {
                foreach (var b in req.ParamList)
                {
                    string s = b.FromUTF8Bytes();
                    var splitfield = s.Split(' ');
                    if (splitfield.Length != 2)
                    {
                        continue;
                    }

                    var corner = Int16.Parse(splitfield[0]);
                    var textureUUID = UUID.Parse(splitfield[1]);
                    switch (corner)
                    {
                        case 0:
                            scene.RegionSettings.TerrainTexture1 = textureUUID;
                            settingsChanged = true;
                            break;

                        case 1:
                            scene.RegionSettings.TerrainTexture2 = textureUUID;
                            settingsChanged = true;
                            break;

                        case 2:
                            scene.RegionSettings.TerrainTexture3 = textureUUID;
                            settingsChanged = true;
                            break;

                        case 3:
                            scene.RegionSettings.TerrainTexture4 = textureUUID;
                            settingsChanged = true;
                            break;

                        default:
                            break;
                    }
                }
            }
            finally
            {
                if (settingsChanged)
                {
                    scene.TriggerRegionSettingsChanged();
                }
            }
        }

        private void EstateOwner_TextureHeights(AgentCircuit circuit, EstateOwnerMessage req)
        {
            var scene = circuit.Scene;

            foreach (byte[] b in req.ParamList)
            {
                string s = b.FromUTF8Bytes();
                string[] splitfield = s.Split(' ');
                if (splitfield.Length != 3)
                {
                    continue;
                }

                var corner = Int16.Parse(splitfield[0]);
                var lowValue = float.Parse(splitfield[1], CultureInfo.InvariantCulture);
                var highValue = float.Parse(splitfield[2], CultureInfo.InvariantCulture);

                switch (corner)
                {
                    case 0:
                        scene.RegionSettings.Elevation1SW = lowValue;
                        scene.RegionSettings.Elevation2SW = highValue;
                        break;

                    case 1:
                        scene.RegionSettings.Elevation1NW = lowValue;
                        scene.RegionSettings.Elevation2NW = highValue;
                        break;

                    case 2:
                        scene.RegionSettings.Elevation1SE = lowValue;
                        scene.RegionSettings.Elevation2SE = highValue;
                        break;

                    case 3:
                        scene.RegionSettings.Elevation1NE = lowValue;
                        scene.RegionSettings.Elevation2NE = highValue;
                        break;

                    default:
                        break;
                }
            }

            scene.TriggerRegionSettingsChanged();
        }

        private void EstateOwner_TextureCommit(AgentCircuit circuit, EstateOwnerMessage req)
        {
        }

        private static bool ParamStringToBool(byte[] b)
        {
            string s = b.FromUTF8Bytes().ToLower();
            return s == "1" || s == "y" || s == "yes" || s == "t" || s == "true";
        }

        private void EstateOwner_SetRegionTerrain(AgentCircuit circuit, EstateOwnerMessage req)
        {
            if(req.ParamList.Count != 9)
            {
                return;
            }

            var scene = circuit.Scene;

            scene.RegionSettings.WaterHeight = float.Parse(req.ParamList[0].FromUTF8Bytes(), CultureInfo.InvariantCulture);
            scene.RegionSettings.TerrainRaiseLimit = float.Parse(req.ParamList[1].FromUTF8Bytes(), CultureInfo.InvariantCulture);
            scene.RegionSettings.TerrainLowerLimit = float.Parse(req.ParamList[2].FromUTF8Bytes(), CultureInfo.InvariantCulture);

            scene.RegionSettings.IsSunFixed = ParamStringToBool(req.ParamList[4]);
            scene.RegionSettings.SunPosition = (double.Parse(req.ParamList[5].FromUTF8Bytes(), CultureInfo.InvariantCulture) - 6).Clamp(0, 24);
            scene.RegionSettings.UseEstateSun = ParamStringToBool(req.ParamList[3]);
            /* 6 is bool estate use global time */
            /* 7 is bool for IsEstateFixedSun */
            /* 8 estate sun_hour */

            scene.TriggerRegionSettingsChanged();
        }

        private void EstateOwner_Restart(AgentCircuit circuit, EstateOwnerMessage req)
        {
            if(req.ParamList.Count < 1)
            {
                return;
            }

            float timeToRestart;
            if(!float.TryParse(req.ParamList[0].FromUTF8Bytes(), NumberStyles.Any, CultureInfo.InvariantCulture, out timeToRestart))
            {
                timeToRestart = 120;
            }
            circuit.Scene.RequestRegionRestart((int)timeToRestart);
        }

        private void EstateOwner_EstateChangeCovenantId(AgentCircuit circuit, EstateOwnerMessage req)
        {
            if(req.ParamList.Count < 1)
            {
                return;
            }

            var covenantID = UUID.Parse(req.ParamList[0].FromUTF8Bytes());
            EstateInfo estate;
            uint estateID;
            var estateService = circuit.Scene.EstateService;
            if(estateService.RegionMap.TryGetValue(circuit.Scene.ID, out estateID) &&
                estateService.TryGetValue(estateID, out estate))
            {
                estate.CovenantID = covenantID;
                estate.CovenantTimestamp = Date.Now;
                estateService[estate.ID] = estate;
                foreach(var regionID in estateService.RegionMap[estateID])
                {
                    SceneInterface estateScene;
                    if(m_Scenes.TryGetValue(regionID, out estateScene))
                    {
                        estateScene.TriggerEstateUpdate();
                    }
                }
            }
        }

        private void EstateOwner_EstateAccessDelta(AgentCircuit circuit, EstateOwnerMessage req)
        {
            if(req.ParamList.Count < 3)
            {
                return;
            }

            var scene = circuit.Scene;
            var flags = (EstateAccessDeltaFlags)int.Parse(req.ParamList[1].FromUTF8Bytes());
            var prey = UUID.Parse(req.ParamList[2].FromUTF8Bytes());
            var uui = UUI.Unknown;
            var ugi = UGI.Unknown;
            if((scene.GroupsNameService == null || !scene.GroupsNameService.TryGetValue(prey, out ugi)) &&
                (flags & (EstateAccessDeltaFlags.AddGroup | EstateAccessDeltaFlags.RemoveGroup)) != 0)
            {
                circuit.Agent.SendAlertMessage(this.GetLanguageString(circuit.Agent.CurrentCulture, "ChangingEstateAccessNotPossibleSinceGroupNotKnown", "Changing estate access not possible since group not known"), scene.ID);
                return;
            }
            if (!scene.AvatarNameService.TryGetValue(prey, out uui) &&
                (flags & (EstateAccessDeltaFlags.AddUser | EstateAccessDeltaFlags.AddManager | EstateAccessDeltaFlags.AddBan |
                        EstateAccessDeltaFlags.RemoveUser | EstateAccessDeltaFlags.RemoveManager | EstateAccessDeltaFlags.RemoveBan)) != 0)
            {
                circuit.Agent.SendAlertMessage(this.GetLanguageString(circuit.Agent.CurrentCulture, "ChangingEstateAccessNotPossibleSinceAgentNotKnown", "Changing estate access not possible since agent not known"), scene.ID);
                return;
            }

            EstateInfo estate;
            uint estateID;
            var allEstateIds = new List<uint>();
            var estateService = circuit.Scene.EstateService;
            if (estateService.RegionMap.TryGetValue(circuit.Scene.ID, out estateID) &&
                estateService.TryGetValue(estateID, out estate))
            {
                if((flags & EstateAccessDeltaFlags.AllEstates) != 0)
                {
                    allEstateIds = estateService.EstateOwner[estate.Owner];
                }
                else
                {
                    allEstateIds.Add(estateID);
                }
            }

            if(allEstateIds.Count == 0)
            {
                circuit.Agent.SendAlertMessage(this.GetLanguageString(circuit.Agent.CurrentCulture, "ChangingEstateAccessNotPossibleSinceNoEstateFound", "Changing estate access not possible since no estate found"), scene.ID);
                return;
            }

            var userUpdate = false;
            var groupUpdate = false;
            var banUpdate = false;
            var emUpdate = false;

            foreach(var selectedEstateId in allEstateIds)
            {
                if((flags & EstateAccessDeltaFlags.AddUser) != 0)
                {
                    estateService.EstateAccess[selectedEstateId, uui] = true;
                    userUpdate = true;
                }
                if((flags & EstateAccessDeltaFlags.RemoveUser) != 0)
                {
                    estateService.EstateAccess[selectedEstateId, uui] = false;
                    userUpdate = true;
                }
                if((flags & EstateAccessDeltaFlags.AddGroup) != 0)
                {
                    estateService.EstateGroup[selectedEstateId, ugi] = true;
                    groupUpdate = true;
                }
                if((flags & EstateAccessDeltaFlags.RemoveGroup) != 0)
                {
                    estateService.EstateGroup[selectedEstateId, ugi] = false;
                    groupUpdate = true;
                }
                if ((flags & EstateAccessDeltaFlags.AddManager) != 0)
                {
                    estateService.EstateManager[selectedEstateId, uui] = true;
                    emUpdate = true;
                }
                if((flags & EstateAccessDeltaFlags.RemoveManager) != 0)
                {
                    estateService.EstateManager[selectedEstateId, uui] = false;
                    emUpdate = true;
                }
                if((flags & EstateAccessDeltaFlags.AddBan) != 0)
                {
                    estateService.EstateBans[selectedEstateId, uui] = true;
                    banUpdate = true;
                }
                if((flags & EstateAccessDeltaFlags.RemoveBan) != 0)
                {
                    estateService.EstateBans[selectedEstateId, uui] = false;
                    banUpdate = true;
                }
            }

            if (emUpdate)
            {
                SendEstateList(
                    req.TransactionID,
                    req.Invoice,
                    EstateAccessFlags.Managers,
                    scene.EstateService.EstateManager.All[estateID],
                    estateID,
                    req.CircuitSceneID);
            }

            if (userUpdate)
            {
                SendEstateList(
                    req.TransactionID,
                    req.Invoice,
                    EstateAccessFlags.AllowedAgents,
                    scene.EstateService.EstateAccess.All[estateID],
                    estateID,
                    req.CircuitSceneID);
            }

            if (groupUpdate)
            {
                SendEstateList(
                    req.TransactionID,
                    req.Invoice,
                    scene.EstateService.EstateGroup.All[estateID],
                    estateID,
                    req.CircuitSceneID);
            }

            if (banUpdate)
            {
                SendEstateList(
                    req.TransactionID,
                    req.Invoice,
                    EstateAccessFlags.BannedAgents,
                    scene.EstateService.EstateBans.All[estateID],
                    estateID,
                    req.CircuitSceneID);
            }
        }

        private void EstateOwner_SimulatorMessage(AgentCircuit circuit, EstateOwnerMessage req)
        {
            if(req.ParamList.Count < 5)
            {
                return;
            }

            var message = req.ParamList[4].FromUTF8Bytes();

            foreach (var agent in circuit.Scene.Agents)
            {
                agent.SendRegionNotice(Owner, message, req.CircuitSceneID);
            }
        }

        private void EstateOwner_InstantMessage(AgentCircuit circuit, EstateOwnerMessage req)
        {
            if (req.ParamList.Count < 2)
            {
                return;
            }

            var message = (req.ParamList.Count < 5 ?
                req.ParamList[1] :
                req.ParamList[4]).FromUTF8Bytes();

            var scene = circuit.Scene;
            var thisRegionId = scene.ID;
            var estateService = scene.EstateService;
            uint estateId;
            if(estateService.RegionMap.TryGetValue(thisRegionId, out estateId))
            {
                foreach(var regionId in estateService.RegionMap[estateId])
                {
                    if(m_Scenes.TryGetValue(regionId, out scene))
                    {
                        foreach(var agent in scene.RootAgents)
                        {
                            agent.SendAlertMessage(message, regionId);
                        }
                    }
                }
            }
        }

        private void EstateOwner_SetRegionDebug(AgentCircuit circuit, EstateOwnerMessage req)
        {
            if(req.ParamList.Count < 3)
            {
                return;
            }
            var scene = circuit.Scene;
            scene.RegionSettings.DisableScripts = ParamStringToBool(req.ParamList[0]);
            scene.RegionSettings.DisableCollisions = ParamStringToBool(req.ParamList[1]);
            scene.RegionSettings.DisablePhysics = ParamStringToBool(req.ParamList[2]);
            scene.TriggerRegionSettingsChanged();
        }

        private void EstateOwner_TeleportHomeUser(AgentCircuit circuit, EstateOwnerMessage req)
        {
            if (req.ParamList.Count < 2)
            {
                return;
            }
            var prey = UUID.Parse(req.ParamList[1].FromUTF8Bytes());

            var scene = circuit.Scene;

            IAgent targetagent;
            if(scene.RootAgents.TryGetValue(prey, out targetagent) &&
                !targetagent.TeleportHome(scene))
            {
                targetagent.KickUser(this.GetLanguageString(circuit.Agent.CurrentCulture, "YouHaveBeenKickedSinceYouCouldNotBeTeleportedHome", "You have been kicked since you could not be teleported home."));
            }
        }

        private void EstateOwner_TeleportHomeAllUsers(AgentCircuit circuit, EstateOwnerMessage req)
        {
            var scene = circuit.Scene;

            foreach(var targetagent in scene.RootAgents)
            {
                if(targetagent == circuit.Agent)
                {
                    /* do not do self-kick */
                }
                else if (!targetagent.TeleportHome(scene))
                {
                    targetagent.KickUser(this.GetLanguageString(circuit.Agent.CurrentCulture, "YouHaveBeenKickedSinceYouCouldNotBeTeleportedHome", "You have been kicked since you could not be teleported home."));
                }
            }
        }

        private void EstateOwner_Colliders(AgentCircuit circuit, EstateOwnerMessage req)
        {

        }

        private void EstateOwner_Scripts(AgentCircuit circuit, EstateOwnerMessage req)
        {
            SceneInterface scene = circuit.Scene;
            if(scene == null)
            {
                return;
            }

            RwLockedDictionary<uint, ScriptReportData> execTimes = scene.ScriptThreadPool.GetExecutionTimes();
            var reply = new LandStatReply()
            {
                ReportType = 0,
                RequestFlags = 0,
                TotalObjectCount = (uint)execTimes.Count
            };

            int allocedlength = 0;

            /* make top objects go first */
            foreach (KeyValuePair<uint, ScriptReportData> kvp in execTimes.OrderByDescending(x => x.Value))
            {
                if(reply.ReportData.Count == 100)
                {
                    break;
                }
                ObjectPart p;
                try
                {
                    if (!scene.Primitives.TryGetValue(kvp.Key, out p))
                    {
                        continue;
                    }

                    var entry = new LandStatReply.ReportDataEntry()
                    {
                        Location = p.GlobalPosition,
                        Score = kvp.Value.Score,
                        TaskID = p.ID,
                        TaskLocalID = kvp.Key,
                        TaskName = p.Name,
                        OwnerName = p.Owner.FullName
                    };

                    if(allocedlength + entry.MessageLength > 1400)
                    {
                        circuit.SendMessage(reply);
                        reply = new LandStatReply()
                        {
                            ReportType = 0,
                            RequestFlags = 0,
                            TotalObjectCount = (uint)execTimes.Count
                        };
                    }

                    reply.ReportData.Add(entry);
                    allocedlength += entry.MessageLength;
                }
                catch
                {
                    /* ignore the report */
                }
            }
            circuit.SendMessage(reply);
        }

        private void EstateOwner_Terrain(AgentCircuit circuit, EstateOwnerMessage req)
        {
            if(req.ParamList.Count < 1)
            {
                return;
            }
            SceneInterface scene = circuit.Scene;
            if (scene == null)
            {
                return;
            }
            switch (req.ParamList[0].FromUTF8Bytes())
            {
                case "bake":
                    scene.Terrain.Flush();
                    break;

                case "download filename":
                    if(req.ParamList.Count > 1)
                    {
                        string viewerFilename = req.ParamList[1].FromUTF8Bytes();

                        AddNewFile("terrain.raw", LLRAWData.ToLLRaw(scene.Terrain.AllPatches));
                        circuit.SendMessage(new InitiateDownload
                        {
                            AgentID = ID,
                            SimFilename = "terrain.raw",
                            ViewerFilename = viewerFilename
                        });
                    }
                    break;

                case "upload filename":
                    if(req.ParamList.Count > 1)
                    {
                        var t = new TerrainUploadTransaction(circuit.Scene)
                        {
                            Filename = req.ParamList[1].FromUTF8Bytes()
                        };
                        AddTerrainUploadTransaction(t, req.CircuitSceneID);
                    }
                    break;

                default:
                    break;
            }
        }

        private void EstateOwner_EstateChangeInfo(AgentCircuit circuit, EstateOwnerMessage req)
        {
            if(req.ParamList.Count < 3)
            {
                return;
            }
            var scene = circuit.Scene;
            var param1 = (RegionOptionFlags)uint.Parse(req.ParamList[1].FromUTF8Bytes());
            uint param2 = uint.Parse(req.ParamList[2].FromUTF8Bytes());
            string estateName = req.ParamList[0].FromUTF8Bytes();
#if DEBUG
            m_Log.DebugFormat("Changing Estate Info: Name={0}, Flags={1}, SunPos={2}",
                estateName, param1.ToString(), param2);
#endif

            EstateInfo estate;
            var estateService = scene.EstateService;
            uint estateID;
            if (estateService.RegionMap.TryGetValue(circuit.Scene.ID, out estateID) &&
                estateService.TryGetValue(estateID, out estate))
            {
                if (param2 != 0)
                {
                    estate.UseGlobalTime = false;
                    estate.SunPosition = (param2 / 1024.0 - 6).Clamp(0, 24);
                }
                else
                {
                    estate.UseGlobalTime = true;
                }

                if (estateName.Length != 0)
                {
                    estate.Name = estateName;
                }
                estate.Flags = param1;
                estateService[estateID] = estate;

                SendEstateUpdateInfo(req.Invoice, req.TransactionID, estate, scene.ID);
                foreach (var regionID in estateService.RegionMap[estateID])
                {
                    SceneInterface estateScene;
                    if (m_Scenes.TryGetValue(regionID, out estateScene))
                    {
                        estateScene.TriggerEstateUpdate();
                    }
                }
            }
        }

        private void EstateOwner_Telehub(AgentCircuit circuit, EstateOwnerMessage req)
        {
            UInt32 param = 0;
            if(req.ParamList.Count < 1)
            {
                return;
            }
            var cmd = req.ParamList[0].FromUTF8Bytes();
            if ((cmd != "info ui" && cmd != "delete") &&
                (req.ParamList.Count < 2 ||
                    !UInt32.TryParse(req.ParamList[1].FromUTF8Bytes(), out param)))
            {
                return;
            }

            ObjectPart part;
            var scene = circuit.Scene;
            switch (cmd)
            {
                case "connect":
                    if(scene.Primitives.TryGetValue(param, out part))
                    {
                        scene.SpawnPoints = new List<Vector3>();
                        scene.RegionSettings.TelehubObject = part.ObjectGroup.ID;
                        scene.TriggerRegionSettingsChanged();
                    }
                    break;

                case "delete":
                    scene.SpawnPoints = new List<Vector3>();
                    scene.RegionSettings.TelehubObject = UUID.Zero;
                    scene.TriggerRegionSettingsChanged();
                    break;

                case "spawnpoint add":
                    if (scene.Primitives.TryGetValue(param, out part))
                    {
                        scene.AddSpawnPoint(part.GlobalPosition);
                    }
                    break;

                case "spawnpoint remove":
                    scene.RemoveSpawnPoint(param);
                    break;

                default:
                    break;
            }

            var res = new TelehubInfo()
            {
                ObjectID = scene.RegionSettings.TelehubObject
            };
            if (scene.Primitives.TryGetValue(res.ObjectID, out part))
            {
                res.ObjectName = part.Name;
                res.TelehubPos = part.GlobalPosition;
                res.TelehubRot = part.GlobalRotation;
            }

            res.SpawnPoints.AddRange(scene.SpawnPoints);
            SendMessageAlways(res, scene.ID);
        }

        private void EstateOwner_KickEstate(AgentCircuit circuit, EstateOwnerMessage req)
        {
            if(req.ParamList.Count < 1)
            {
                return;
            }

            var prey = UUID.Parse(req.ParamList[0].FromUTF8Bytes());

            IAgent targetagent;
            var scene = circuit.Scene;
            if (scene.RootAgents.TryGetValue(prey, out targetagent))
            {
                targetagent.KickUser(this.GetLanguageString(targetagent.CurrentCulture, "YouHaveBeenKicked", "You have been kicked."));
            }
        }
    }
}
