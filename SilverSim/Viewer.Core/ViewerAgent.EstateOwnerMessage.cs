// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Generic;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using SilverSim.Types.Estate;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using SilverSim.ServiceInterfaces.Estate;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        byte[] StringToBytes(string s)
        {
            return UTF8NoBOM.GetBytes(s + "\0");
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
                /* only EO and EM allowed behind */
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
                    EstateOwner_EstateChangeCovenantId(circuit, req);
                    break;

                case "estateaccessdelta":
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
            EstateOwnerMessage msg = new EstateOwnerMessage();
            msg.AgentID = Owner.ID;
            msg.SessionID = SessionID;
            msg.Invoice = req.Invoice;
            msg.TransactionID = req.TransactionID;
            msg.Method = "estateupdateinfo";
            SceneInterface scene = circuit.Scene;

            uint estateID = scene.EstateService.RegionMap[scene.ID];
            EstateInfo estateInfo = scene.EstateService[estateID];
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
            scene.RegionSettings.AgentLimit = (int)decimal.Parse(UTF8NoBOM.GetString(req.ParamList[4]), CultureInfo.InvariantCulture);
            scene.RegionSettings.ObjectBonus = float.Parse(UTF8NoBOM.GetString(req.ParamList[5]), CultureInfo.InvariantCulture);
#warning TODO: adjust for correct values
            //scene.RegionSettings.Maturity = int.Parse(req.ParamList[6]);
            scene.RegionSettings.RestrictPushing = ParamStringToBool(req.ParamList[7]);
            scene.RegionSettings.AllowLandJoinDivide = ParamStringToBool(req.ParamList[8]);

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
                    string s = UTF8NoBOM.GetString(b);
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
                string s = UTF8NoBOM.GetString(b);
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
            string s = UTF8NoBOM.GetString(b);
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

            scene.RegionSettings.WaterHeight = float.Parse(UTF8NoBOM.GetString(req.ParamList[0]), CultureInfo.InvariantCulture);
            scene.RegionSettings.TerrainRaiseLimit = float.Parse(UTF8NoBOM.GetString(req.ParamList[1]), CultureInfo.InvariantCulture);
            scene.RegionSettings.TerrainLowerLimit = float.Parse(UTF8NoBOM.GetString(req.ParamList[2]), CultureInfo.InvariantCulture);

            bool useEstateSun = ParamStringToBool(req.ParamList[3]);
            bool useFixedSun = ParamStringToBool(req.ParamList[4]);
            float sunHour = float.Parse(UTF8NoBOM.GetString(req.ParamList[5]), CultureInfo.InvariantCulture);
            bool useGlobal = ParamStringToBool(req.ParamList[6]);
            bool isEstateFixedSun = ParamStringToBool(req.ParamList[7]);
            float estateSunHour = float.Parse(UTF8NoBOM.GetString(req.ParamList[8]), CultureInfo.InvariantCulture);

            scene.TriggerRegionSettingsChanged();
        }

        void EstateOwner_Restart(AgentCircuit circuit, EstateOwnerMessage req)
        {
            if(req.ParamList.Count < 1)
            {
                return;
            }

            float timeToRestart;
            if(!float.TryParse(UTF8Encoding.UTF8.GetString(req.ParamList[0]), NumberStyles.Any, CultureInfo.InvariantCulture, out timeToRestart))
            {
                timeToRestart = 120;
            }
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        void EstateOwner_EstateChangeCovenantId(AgentCircuit circuit, EstateOwnerMessage req)
        {
            if(req.ParamList.Count < 1)
            {
                return;
            }

            UUID covenantID = UUID.Parse(UTF8Encoding.UTF8.GetString(req.ParamList[0]));
            EstateInfo estate;
            uint estateID;
            EstateServiceInterface estateService = circuit.Scene.EstateService;
            if(estateService.RegionMap.TryGetValue(circuit.Scene.ID, out estateID) &&
                estateService.TryGetValue(estateID, out estate))
            {
                estate.CovenantID = covenantID;
                estateService[estate.ID] = estate;
            }
        }

        void EstateOwner_EstateAccessDelta(AgentCircuit circuit, EstateOwnerMessage req)
        {
            if(req.ParamList.Count < 3)
            {
                return;
            }

            SceneInterface scene = circuit.Scene;
            EstateAccessDeltaFlags flags = (EstateAccessDeltaFlags)int.Parse(UTF8NoBOM.GetString(req.ParamList[1]));
            UUID prey = UUID.Parse(UTF8NoBOM.GetString(req.ParamList[2]));
            UUI uui;
            UGI ugi;
            if(!scene.GroupsNameService.TryGetValue(prey, out ugi) &&
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
            UUID invoice = req.Invoice;
            string message = UTF8NoBOM.GetString(req.ParamList[4]);

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
            UUID invoice = req.Invoice;
            string message;

            message = req.ParamList.Count < 5 ?
                UTF8NoBOM.GetString(req.ParamList[1]) :
                UTF8NoBOM.GetString(req.ParamList[4]);
        }

        void EstateOwner_SetRegionDebug(AgentCircuit circuit, EstateOwnerMessage req)
        {
            if(req.ParamList.Count < 3)
            {
                return;
            }
            UUID invoice = req.Invoice;
            UUID SenderID = Owner.ID;
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
            UUID invoice = req.Invoice;
            UUID senderID = Owner.ID;
            UUID prey = UUID.Parse(UTF8NoBOM.GetString(req.ParamList[1]));

            SceneInterface scene = circuit.Scene;

            IAgent targetagent;
            if(scene.RootAgents.TryGetValue(prey, out targetagent) &&
                !targetagent.TeleportHome(scene))
            {
                targetagent.KickUser("You were teleported home by the region owner. Because of failing TP, you have been logged out.");
            }
        }

        void EstateOwner_TeleportHomeAllUsers(AgentCircuit circuit, EstateOwnerMessage req)
        {
            UUID invoice = req.Invoice;
            UUID senderID = Owner.ID;

            SceneInterface scene = circuit.Scene;

            foreach(IAgent targetagent in scene.RootAgents)
            {
                if(targetagent == circuit.Agent)
                {
                    /* do not do self-kick */
                }
                else if (!targetagent.TeleportHome(scene))
                {
                    targetagent.KickUser("You were teleported home by the region owner. Because of failing TP, you have been logged out.");
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
            switch (UTF8NoBOM.GetString(req.ParamList[0]))
            {
                case "bake":
                    break;

                case "download filename":
                    break;

                case "upload filename":
                    if(req.ParamList.Count > 1)
                    {
                        TerrainUploadTransaction t = new TerrainUploadTransaction();
                        t.Filename = UTF8NoBOM.GetString(req.ParamList[1]);
                        AddTerrainUploadTransaction(t, req.CircuitSceneID);
                    }
                    break;

                default:
                    break;
            }
        }

        [Flags]
        public enum EstateChangeInfoFlags : uint
        {
            FixedSun = 0x00000010,
            PublicAccess = 0x00008000,
            AllowVoice = 0x10000000,
            AllowDirectTeleport = 0x00100000,
            DenyAnonymous = 0x00800000,
            DenyIdentified = 0x01000000,
            DenyTransacted = 0x02000000,
            DenyMinors = 0x40000000
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        void EstateOwner_EstateChangeInfo(AgentCircuit circuit, EstateOwnerMessage req)
        {
            if(req.ParamList.Count < 3)
            {
                return;
            }
            UUID invoice = req.Invoice;
            UUID senderID = req.AgentID;
            SceneInterface scene = circuit.Scene;
            EstateChangeInfoFlags param1 = (EstateChangeInfoFlags)UInt32.Parse(UTF8NoBOM.GetString(req.ParamList[1]));
            UInt32 param2 = UInt32.Parse(UTF8NoBOM.GetString(req.ParamList[2]));

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

                RegionOptionFlags flags = estate.Flags;

                flags = ((param1 & EstateChangeInfoFlags.FixedSun) != 0) ?
                    flags | RegionOptionFlags.SunFixed :
                    flags & (~RegionOptionFlags.SunFixed);

                flags = ((param1 & EstateChangeInfoFlags.PublicAccess) != 0) ?
                    flags | RegionOptionFlags.PublicAllowed :
                    flags & (~RegionOptionFlags.PublicAllowed);

                flags = ((param1 & EstateChangeInfoFlags.AllowVoice) != 0) ?
                    flags | RegionOptionFlags.AllowVoice :
                    flags & (~RegionOptionFlags.AllowVoice);

                flags = ((param1 & EstateChangeInfoFlags.AllowDirectTeleport) != 0) ?
                    flags | RegionOptionFlags.AllowDirectTeleport :
                    flags & (~RegionOptionFlags.AllowDirectTeleport);

                flags = ((param1 & EstateChangeInfoFlags.DenyAnonymous) != 0) ?
                    flags | RegionOptionFlags.DenyAnonymous :
                    flags & (~RegionOptionFlags.DenyAnonymous);

                flags = ((param1 & EstateChangeInfoFlags.DenyIdentified) != 0) ?
                    flags | RegionOptionFlags.DenyIdentified :
                    flags & (~RegionOptionFlags.DenyIdentified);

                flags = ((param1 & EstateChangeInfoFlags.DenyTransacted) != 0) ?
                    flags | RegionOptionFlags.DenyTransacted :
                    flags & (~RegionOptionFlags.DenyTransacted);

                flags = ((param1 & EstateChangeInfoFlags.DenyMinors) != 0) ?
                    flags | RegionOptionFlags.DenyAgeUnverified :
                    flags & (~RegionOptionFlags.DenyAgeUnverified);
                estate.Flags = flags;
                estateService[estateID] = estate;

                EstateOwnerMessage m = new EstateOwnerMessage();
                m.AgentID = circuit.AgentID;
                m.SessionID = UUID.Zero;
                m.Invoice = invoice;
                m.Method = "estateupdateinfo";
                m.TransactionID = UUID.Zero;
                m.ParamList.Add(UTF8NoBOM.GetBytes(estate.Name));
                m.ParamList.Add(UTF8NoBOM.GetBytes(estate.Owner.ID.ToString()));
                m.ParamList.Add(UTF8NoBOM.GetBytes(estate.ID.ToString()));
                m.ParamList.Add(UTF8NoBOM.GetBytes(((uint)estate.Flags).ToString()));
                m.ParamList.Add(UTF8NoBOM.GetBytes(estate.SunPosition.ToString()));
                m.ParamList.Add(UTF8NoBOM.GetBytes(estate.ParentEstateID.ToString()));
                m.ParamList.Add(UTF8NoBOM.GetBytes(estate.CovenantID.ToString()));
                m.ParamList.Add(UTF8NoBOM.GetBytes(estate.CovenantTimestamp.AsULong.ToString()));
                m.ParamList.Add(UTF8NoBOM.GetBytes("1"));
                m.ParamList.Add(UTF8NoBOM.GetBytes(estate.AbuseEmail));
                circuit.SendMessage(m);
            }
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        void EstateOwner_Telehub(AgentCircuit circuit, EstateOwnerMessage req)
        {
            UUID invoice = req.Invoice;
            UUID senderID = Owner.ID;
            UInt32 param = 0;
            if(req.ParamList.Count < 1)
            {
                return;
            }
            string cmd = UTF8NoBOM.GetString(req.ParamList[0]);
            if(cmd != "info ui")
            {
                if (req.ParamList.Count < 2)
                {
                    return;
                }
                param = UInt32.Parse(UTF8NoBOM.GetString(req.ParamList[1]));
            }
        }

        void EstateOwner_KickEstate(AgentCircuit circuit, EstateOwnerMessage req)
        {
            if(req.ParamList.Count < 1)
            {
                return;
            }

            UUID invoice = req.Invoice;
            UUID senderID = Owner.ID;
            UUID prey = UUID.Parse(UTF8NoBOM.GetString(req.ParamList[0]));

            IAgent targetagent;
            SceneInterface scene = circuit.Scene;
            if (scene.RootAgents.TryGetValue(prey, out targetagent))
            {
                targetagent.KickUser("You were kicked by the region owner.");
            }
        }

        static readonly UTF8Encoding UTF8NoBOM = new UTF8Encoding(false);
    }
}
