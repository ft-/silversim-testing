// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Estate;
using SilverSim.Types;
using SilverSim.Types.Estate;
using SilverSim.Types.Grid;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Generic;
using SilverSim.Viewer.Messages.Telehub;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        byte[] StringToBytes(string s)
        {
            return (s + "\0").ToUTF8Bytes();
        }

        [PacketHandler(MessageType.EstateOwnerMessage)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleEstateOwnerMessage(Message m)
        {
            EstateOwnerMessage req = (EstateOwnerMessage)m;
            if(req.SessionID != SessionID ||
                req.AgentID != m_AgentID)
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

        [Flags]
        [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
        enum EstateAccessCodes : uint
        {
            AccessOptions = 1,
            AllowedGroups = 2,
            EstateBans = 4,
            EstateManagers = 8
        }

        void SendEstateList(UUID transactionID, UUID invoice, EstateAccessCodes code, List<UUI> data, uint estateID, UUID fromSceneID)
        {
            int i = 0;
            while(i < data.Count)
            {
                int remaining = data.Count - i;
                if(remaining > 50)
                {
                    remaining = 50;
                }

                EstateOwnerMessage msg = new EstateOwnerMessage();
                msg.TransactionID = transactionID;
                msg.Invoice = invoice;
                msg.AgentID = Owner.ID;
                msg.SessionID = SessionID;
                msg.Method = "setaccess";

                msg.ParamList.Add(StringToBytes(estateID.ToString()));
                msg.ParamList.Add(StringToBytes(((uint)code).ToString()));
                msg.ParamList.Add(StringToBytes("0"));
                msg.ParamList.Add(StringToBytes("0"));
                if (code == EstateAccessCodes.EstateBans)
                {
                    msg.ParamList.Add(StringToBytes(remaining.ToString()));
                }
                else
                {
                    msg.ParamList.Add(StringToBytes("0"));
                }
                msg.ParamList.Add(StringToBytes("0"));
                while(remaining-- != 0)
                {
                    msg.ParamList.Add(data[i++].ID.GetBytes());
                }
                SendMessageIfRootAgent(msg, fromSceneID);
            }
        }

        /* this is groups only, so no code check inside */
        void SendEstateList(UUID transactionID, UUID invoice, EstateAccessCodes code, List<UGI> data, uint estateID, UUID fromSceneID)
        {
            int i = 0;
            while(i < data.Count)
            {
                int remaining = data.Count - i;
                if (remaining > 50)
                {
                    remaining = 50;
                }

                EstateOwnerMessage msg = new EstateOwnerMessage();
                msg.TransactionID = transactionID;
                msg.Invoice = invoice;
                msg.AgentID = Owner.ID;
                msg.SessionID = SessionID;
                msg.Method = "setaccess";

                msg.ParamList.Add(StringToBytes(estateID.ToString()));
                msg.ParamList.Add(StringToBytes(((uint)code).ToString()));
                msg.ParamList.Add(StringToBytes("0"));
                msg.ParamList.Add(StringToBytes("0"));
                msg.ParamList.Add(StringToBytes("0"));
                msg.ParamList.Add(StringToBytes("0"));
                while (remaining-- != 0)
                {
                    msg.ParamList.Add(data[i++].ID.GetBytes());
                }
                SendMessageIfRootAgent(msg, fromSceneID);
            }
        }

        void EstateOwner_GetInfo(AgentCircuit circuit, EstateOwnerMessage req)
        {
            SceneInterface scene = circuit.Scene;
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
                EstateAccessCodes.EstateManagers,
                scene.EstateService.EstateManager.All[estateID], 
                estateID, 
                req.CircuitSceneID);
            SendEstateList(
                req.TransactionID,
                req.Invoice, 
                EstateAccessCodes.AccessOptions, 
                scene.EstateService.EstateAccess.All[estateID], 
                estateID, 
                req.CircuitSceneID);
            SendEstateList(
                req.TransactionID,
                req.Invoice,
                EstateAccessCodes.AllowedGroups,
                scene.EstateService.EstateGroup.All[estateID],
                estateID, 
                req.CircuitSceneID);
            SendEstateList(
                req.TransactionID, 
                req.Invoice, 
                EstateAccessCodes.EstateBans, 
                scene.EstateService.EstateBans.All[estateID], 
                estateID, 
                req.CircuitSceneID);

            EstateOwnerMessage msg = new EstateOwnerMessage();
            msg.AgentID = Owner.ID;
            msg.SessionID = SessionID;
            msg.Invoice = req.Invoice;
            msg.TransactionID = req.TransactionID;
            msg.Method = "estateupdateinfo";

            msg.ParamList.Add(StringToBytes(estateInfo.Name));
            msg.ParamList.Add(StringToBytes((string)estateInfo.Owner.ID));
            msg.ParamList.Add(StringToBytes(estateID.ToString()));
            msg.ParamList.Add(StringToBytes(((uint)estateInfo.Flags).ToString()));
            msg.ParamList.Add(StringToBytes(estateInfo.SunPosition.ToString()));
            msg.ParamList.Add(StringToBytes(estateInfo.ParentEstateID.ToString()));
            msg.ParamList.Add(StringToBytes((string)UUID.Zero)); /* covenant */
            msg.ParamList.Add(StringToBytes("0")); /* covenant changed */
            msg.ParamList.Add(StringToBytes("1"));
            msg.ParamList.Add(StringToBytes(estateInfo.AbuseEmail));

            SendMessageIfRootAgent(msg, req.CircuitSceneID);

        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        void EstateOwner_SetRegionInfo(AgentCircuit circuit, EstateOwnerMessage req)
        {
            if(req.ParamList.Count < 9)
            {
                return;
            }

            SceneInterface scene = circuit.Scene;

            scene.RegionSettings.BlockTerraform = ParamStringToBool(req.ParamList[0]);
            scene.RegionSettings.BlockFly = ParamStringToBool(req.ParamList[1]);
            scene.RegionSettings.AllowDamage = ParamStringToBool(req.ParamList[2]);
            scene.RegionSettings.AllowLandResell = ParamStringToBool(req.ParamList[3]);
            scene.RegionSettings.AgentLimit = (int)decimal.Parse(req.ParamList[4].FromUTF8Bytes(), CultureInfo.InvariantCulture);
            scene.RegionSettings.ObjectBonus = float.Parse(req.ParamList[5].FromUTF8Bytes(), CultureInfo.InvariantCulture);
            scene.Access = (RegionAccess)int.Parse(req.ParamList[6].FromUTF8Bytes());
            scene.RegionSettings.RestrictPushing = ParamStringToBool(req.ParamList[7]);
            scene.RegionSettings.AllowLandJoinDivide = ParamStringToBool(req.ParamList[8]);
            scene.ReregisterRegion();
            scene.TriggerRegionSettingsChanged();
        }

        void EstateOwner_TextureDetail(AgentCircuit circuit, EstateOwnerMessage req)
        {
            bool settingsChanged = false;
            
            SceneInterface scene = circuit.Scene;
            try
            {
                foreach (byte[] b in req.ParamList)
                {
                    string s = b.FromUTF8Bytes();
                    string[] splitfield = s.Split(' ');
                    if (splitfield.Length != 2)
                    {
                        continue;
                    }

                    Int16 corner = Int16.Parse(splitfield[0]);
                    UUID textureUUID = UUID.Parse(splitfield[1]);
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

        void EstateOwner_TextureHeights(AgentCircuit circuit, EstateOwnerMessage req)
        {
            SceneInterface scene = circuit.Scene;

            foreach (byte[] b in req.ParamList)
            {
                string s = b.FromUTF8Bytes();
                string[] splitfield = s.Split(' ');
                if (splitfield.Length != 3)
                {
                    continue;
                }

                Int16 corner = Int16.Parse(splitfield[0]);
                float lowValue = float.Parse(splitfield[1], CultureInfo.InvariantCulture);
                float highValue = float.Parse(splitfield[2], CultureInfo.InvariantCulture);

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

        void EstateOwner_TextureCommit(AgentCircuit circuit, EstateOwnerMessage req)
        {
        }

        static bool ParamStringToBool(byte[] b)
        {
            string s = b.FromUTF8Bytes();
            return (s == "1" || s.ToLower() == "y" || s.ToLower() == "yes" || s.ToLower() == "t" || s.ToLower() == "true");
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        void EstateOwner_SetRegionTerrain(AgentCircuit circuit, EstateOwnerMessage req)
        {
            if(req.ParamList.Count != 9)
            {
                return;
            }

            SceneInterface scene = circuit.Scene;

            scene.RegionSettings.WaterHeight = float.Parse(req.ParamList[0].FromUTF8Bytes(), CultureInfo.InvariantCulture);
            scene.RegionSettings.TerrainRaiseLimit = float.Parse(req.ParamList[1].FromUTF8Bytes(), CultureInfo.InvariantCulture);
            scene.RegionSettings.TerrainLowerLimit = float.Parse(req.ParamList[2].FromUTF8Bytes(), CultureInfo.InvariantCulture);

            bool useEstateSun = ParamStringToBool(req.ParamList[3]);
            bool useFixedSun = ParamStringToBool(req.ParamList[4]);
            float sunHour = float.Parse(req.ParamList[5].FromUTF8Bytes(), CultureInfo.InvariantCulture);
            bool useGlobal = ParamStringToBool(req.ParamList[6]);
            bool isEstateFixedSun = ParamStringToBool(req.ParamList[7]);
            float estateSunHour = float.Parse(req.ParamList[8].FromUTF8Bytes(), CultureInfo.InvariantCulture);

            scene.TriggerRegionSettingsChanged();
        }

        void EstateOwner_Restart(AgentCircuit circuit, EstateOwnerMessage req)
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

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        void EstateOwner_EstateChangeCovenantId(AgentCircuit circuit, EstateOwnerMessage req)
        {
            if(req.ParamList.Count < 1)
            {
                return;
            }

            UUID covenantID = UUID.Parse(req.ParamList[0].FromUTF8Bytes());
            EstateInfo estate;
            uint estateID;
            EstateServiceInterface estateService = circuit.Scene.EstateService;
            if(estateService.RegionMap.TryGetValue(circuit.Scene.ID, out estateID) &&
                estateService.TryGetValue(estateID, out estate))
            {
                estate.CovenantID = covenantID;
                estateService[estate.ID] = estate;
                foreach(UUID regionID in estateService.RegionMap[estateID])
                {
                    SceneInterface estateScene;
                    if(SceneManager.Scenes.TryGetValue(regionID, out estateScene))
                    {
                        estateScene.TriggerEstateUpdate();
                    }
                }
            }
        }

        void EstateOwner_EstateAccessDelta(AgentCircuit circuit, EstateOwnerMessage req)
        {
            if(req.ParamList.Count < 3)
            {
                return;
            }

            SceneInterface scene = circuit.Scene;
            EstateAccessDeltaFlags flags = (EstateAccessDeltaFlags)int.Parse(req.ParamList[1].FromUTF8Bytes());
            UUID prey = UUID.Parse(req.ParamList[2].FromUTF8Bytes());
            UUI uui = UUI.Unknown;
            UGI ugi = UGI.Unknown;
            if((null == scene.GroupsNameService || !scene.GroupsNameService.TryGetValue(prey, out ugi)) &&
                (flags & (EstateAccessDeltaFlags.AddGroup | EstateAccessDeltaFlags.RemoveGroup)) != 0)
            {
                circuit.Agent.SendAlertMessage("Changing estate access not possible since group not known", scene.ID);
                return;
            }
            if (!scene.AvatarNameService.TryGetValue(prey, out uui) &&
                (flags & (EstateAccessDeltaFlags.AddUser | EstateAccessDeltaFlags.AddManager | EstateAccessDeltaFlags.AddBan |
                        EstateAccessDeltaFlags.RemoveUser | EstateAccessDeltaFlags.RemoveManager | EstateAccessDeltaFlags.RemoveBan)) != 0)
            {
                circuit.Agent.SendAlertMessage("Changing estate access not possible since agent not known", scene.ID);
                return;
            }

            EstateInfo estate;
            uint estateID;
            List<uint> allEstateIds = new List<uint>();
            EstateServiceInterface estateService = circuit.Scene.EstateService;
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
                circuit.Agent.SendAlertMessage("Changing estate access not possible since no estate found", scene.ID);
                return;
            }

            foreach(uint selectedEstateId in allEstateIds)
            {
                if((flags & EstateAccessDeltaFlags.AddUser) != 0)
                {
                    estateService.EstateAccess[selectedEstateId, uui] = true;
                }
                if((flags & EstateAccessDeltaFlags.RemoveUser) != 0)
                {
                    estateService.EstateAccess[selectedEstateId, uui] = false;
                }
                if((flags & EstateAccessDeltaFlags.AddGroup) != 0)
                {
                    estateService.EstateGroup[selectedEstateId, ugi] = true;
                }
                if((flags & EstateAccessDeltaFlags.RemoveGroup) != 0)
                {
                    estateService.EstateGroup[selectedEstateId, ugi] = false;
                }
                if((flags & EstateAccessDeltaFlags.AddManager) != 0)
                {
                    estateService.EstateManager[selectedEstateId, uui] = true;
                }
                if((flags & EstateAccessDeltaFlags.RemoveManager) != 0)
                {
                    estateService.EstateManager[selectedEstateId, uui] = false;
                }
                if((flags & EstateAccessDeltaFlags.AddBan) != 0)
                {
                    estateService.EstateBans[selectedEstateId, uui] = true;
                }
                if((flags & EstateAccessDeltaFlags.RemoveBan) != 0)
                {
                    estateService.EstateBans[selectedEstateId, uui] = false;
                }
            }
        }

        void EstateOwner_SimulatorMessage(AgentCircuit circuit, EstateOwnerMessage req)
        {
            if(req.ParamList.Count < 5)
            {
                return;
            }

            string message = req.ParamList[4].FromUTF8Bytes();

            foreach (IAgent agent in circuit.Scene.Agents)
            {
                agent.SendRegionNotice(Owner, message, req.CircuitSceneID);
            }
        }

        void EstateOwner_InstantMessage(AgentCircuit circuit, EstateOwnerMessage req)
        {
            if (req.ParamList.Count < 2)
            {
                return;
            }

            string message;

            message = (req.ParamList.Count < 5 ?
                req.ParamList[1] :
                req.ParamList[4]).FromUTF8Bytes();

            SceneInterface scene = circuit.Scene;
            UUID thisRegionId = scene.ID;
            EstateServiceInterface estateService = scene.EstateService;
            uint estateId;
            if(estateService.RegionMap.TryGetValue(thisRegionId, out estateId))
            {
                List<UUID> allRegions = estateService.RegionMap[estateId];
                foreach(UUID regionId in allRegions)
                {
                    if(SceneManager.Scenes.TryGetValue(regionId, out scene))
                    {
                        foreach(IAgent agent in scene.RootAgents)
                        {
                            agent.SendAlertMessage(message, regionId);
                        }
                    }
                }
            }
        }

        void EstateOwner_SetRegionDebug(AgentCircuit circuit, EstateOwnerMessage req)
        {
            if(req.ParamList.Count < 3)
            {
                return;
            }
            SceneInterface scene = circuit.Scene;
            scene.RegionSettings.DisableScripts = !ParamStringToBool(req.ParamList[0]);
            scene.RegionSettings.DisableCollisions = !ParamStringToBool(req.ParamList[1]);
            scene.RegionSettings.DisablePhysics = !ParamStringToBool(req.ParamList[2]);
            scene.TriggerRegionSettingsChanged();
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        void EstateOwner_TeleportHomeUser(AgentCircuit circuit, EstateOwnerMessage req)
        {
            if (req.ParamList.Count < 2)
            {
                return;
            }
            UUID prey = UUID.Parse(req.ParamList[1].FromUTF8Bytes());

            SceneInterface scene = circuit.Scene;

            IAgent targetagent;
            if(scene.RootAgents.TryGetValue(prey, out targetagent) &&
                !targetagent.TeleportHome(scene))
            {
                targetagent.KickUser(this.GetLanguageString(circuit.Agent.CurrentCulture, "YouHaveBeenKickedSinceYouCouldNotBeTeleportedHome", "You have been kicked since you could not be teleported home."));
            }
        }

        void EstateOwner_TeleportHomeAllUsers(AgentCircuit circuit, EstateOwnerMessage req)
        {
            SceneInterface scene = circuit.Scene;

            foreach(IAgent targetagent in scene.RootAgents)
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

        void EstateOwner_Colliders(AgentCircuit circuit, EstateOwnerMessage req)
        {

        }

        void EstateOwner_Scripts(AgentCircuit circuit, EstateOwnerMessage req)
        {

        }

        void EstateOwner_Terrain(AgentCircuit circuit, EstateOwnerMessage req)
        {
            if(req.ParamList.Count < 1)
            {
                return;
            }
            switch (req.ParamList[0].FromUTF8Bytes())
            {
                case "bake":
                    break;

                case "download filename":
                    break;

                case "upload filename":
                    if(req.ParamList.Count > 1)
                    {
                        TerrainUploadTransaction t = new TerrainUploadTransaction(circuit.Scene);
                        t.Filename = req.ParamList[1].FromUTF8Bytes();
                        AddTerrainUploadTransaction(t, req.CircuitSceneID);
                    }
                    break;

                default:
                    break;
            }
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        void EstateOwner_EstateChangeInfo(AgentCircuit circuit, EstateOwnerMessage req)
        {
            if(req.ParamList.Count < 3)
            {
                return;
            }
            SceneInterface scene = circuit.Scene;
            RegionOptionFlags param1 = (RegionOptionFlags)uint.Parse(req.ParamList[1].FromUTF8Bytes());
            uint param2 = uint.Parse(req.ParamList[2].FromUTF8Bytes());

            EstateInfo estate;
            EstateServiceInterface estateService = scene.EstateService;
            uint estateID;
            if (estateService.RegionMap.TryGetValue(circuit.Scene.ID, out estateID) &&
                estateService.TryGetValue(estateID, out estate))
            {
                if (param2 != 0)
                {
                    estate.UseGlobalTime = false;
                    estate.SunPosition = (param2 - 0x1800) / 1024.0;
                }
                else
                {
                    estate.UseGlobalTime = true;
                }

                estate.Flags = param1;
                estateService[estateID] = estate;

                EstateOwnerMessage m = new EstateOwnerMessage();
                m.AgentID = circuit.AgentID;
                m.SessionID = UUID.Zero;
                m.Invoice = req.Invoice;
                m.Method = "estateupdateinfo";
                m.TransactionID = UUID.Zero;
                m.ParamList.Add(estate.Name.ToUTF8Bytes());
                m.ParamList.Add(estate.Owner.ID.ToString().ToUTF8Bytes());
                m.ParamList.Add(estate.ID.ToString().ToUTF8Bytes());
                m.ParamList.Add(((uint)estate.Flags).ToString().ToUTF8Bytes());
                m.ParamList.Add(estate.SunPosition.ToString().ToUTF8Bytes());
                m.ParamList.Add(estate.ParentEstateID.ToString().ToUTF8Bytes());
                m.ParamList.Add(estate.CovenantID.ToString().ToUTF8Bytes());
                m.ParamList.Add(estate.CovenantTimestamp.AsULong.ToString().ToUTF8Bytes());
                m.ParamList.Add("1".ToUTF8Bytes());
                m.ParamList.Add(estate.AbuseEmail.ToUTF8Bytes());
                circuit.SendMessage(m);
                foreach (UUID regionID in estateService.RegionMap[estateID])
                {
                    SceneInterface estateScene;
                    if (SceneManager.Scenes.TryGetValue(regionID, out estateScene))
                    {
                        estateScene.TriggerEstateUpdate();
                    }
                }
            }
        }

        void EstateOwner_Telehub(AgentCircuit circuit, EstateOwnerMessage req)
        {
            UInt32 param = 0;
            if(req.ParamList.Count < 1)
            {
                return;
            }
            string cmd = req.ParamList[0].FromUTF8Bytes();
            if ((cmd != "info ui" && cmd != "delete") &&
                (req.ParamList.Count < 2 ||
                    !UInt32.TryParse(req.ParamList[1].FromUTF8Bytes(), out param)))
            {
                return;
            }

            ObjectPart part;
            SceneInterface scene = circuit.Scene;
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

            TelehubInfo res = new TelehubInfo();
            res.ObjectID = scene.RegionSettings.TelehubObject;
            if (scene.Primitives.TryGetValue(res.ObjectID, out part))
            {
                res.ObjectName = part.Name;
                res.TelehubPos = part.GlobalPosition;
                res.TelehubRot = part.GlobalRotation;
            }

            res.SpawnPoints.AddRange(scene.SpawnPoints);
            SendMessageAlways(res, scene.ID);
        }

        void EstateOwner_KickEstate(AgentCircuit circuit, EstateOwnerMessage req)
        {
            if(req.ParamList.Count < 1)
            {
                return;
            }

            UUID prey = UUID.Parse(req.ParamList[0].FromUTF8Bytes());

            IAgent targetagent;
            SceneInterface scene = circuit.Scene;
            if (scene.RootAgents.TryGetValue(prey, out targetagent))
            {
                targetagent.KickUser(this.GetLanguageString(targetagent.CurrentCulture, "YouHaveBeenKicked", "You have been kicked."));
            }
        }
    }
}
