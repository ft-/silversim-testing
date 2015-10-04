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

namespace SilverSim.Viewer.Core
{
    public partial class LLAgent
    {
        byte[] StringToBytes(string s)
        {
            return UTF8NoBOM.GetBytes(s + "\0");
        }

        [PacketHandler(MessageType.EstateOwnerMessage)]
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
                    EstateOwner_GetInfo(req);
                    break;

                case "setregioninfo":
                    EstateOwner_SetRegionInfo(req);
                    break;

#if TEXTUREBASE
                case "texturebase":
                    break;
#endif

                case "texturedetail":
                    EstateOwner_TextureDetail(req);
                    break;

                case "textureheights":
                    EstateOwner_TextureHeights(req);
                    break;

                case "texturecommit":
                    EstateOwner_TextureCommit(req);
                    break;

                case "setregionterrain":
                    EstateOwner_SetRegionTerrain(req);
                    break;

                case "restart":
                    EstateOwner_Restart(req);
                    break;

                case "estatechangecovenantid":
                    EstateOwner_EstateChangeCovenantId(req);
                    break;

                case "estateaccessdelta":
                    EstateOwner_EstateAccessDelta(req);
                    break;

                case "simulatormessage":
                    EstateOwner_SimulatorMessage(req);
                    break;

                case "instantmessage":
                    EstateOwner_InstantMessage(req);
                    break;

                case "setregiondebug":
                    EstateOwner_SetRegionDebug(req);
                    break;

                case "teleporthomeuser":
                    EstateOwner_TeleportHomeUser(req);
                    break;

                case "teleporthomeallusers":
                    EstateOwner_TeleportHomeAllUsers(req);
                    break;

                case "colliders":
                    EstateOwner_Colliders(req);
                    break;

                case "scripts":
                    EstateOwner_Scripts(req);
                    break;

                case "terrain":
                    EstateOwner_Terrain(req);
                    break;

                case "estatechangeinfo":
                    EstateOwner_EstateChangeInfo(req);
                    break;

                case "telehub":
                    EstateOwner_Telehub(req);
                    break;

                case "kickestate":
                    EstateOwner_KickEstate(req);
                    break;

                default:
                    m_Log.DebugFormat("EstateOwnerMessage: Unknown method {0} requested", req.Method);
                    break;
            }
        }

        enum EstateAccessCodes : uint
        {
            AccessOptions = 1,
            AllowedGroups = 2,
            EstateBans = 4,
            EstateManagers = 8
        }

        void sendEstateList(UUID transactionID, UUID invoice, EstateAccessCodes code, List<UUI> data, uint estateID, UUID fromSceneID)
        {
            int i;
            for(i = 0; i < data.Count;)
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
        void sendEstateList(UUID transactionID, UUID invoice, EstateAccessCodes code, List<UGI> data, uint estateID, UUID fromSceneID)
        {
            int i;
            for (i = 0; i < data.Count; )
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

        void EstateOwner_GetInfo(EstateOwnerMessage req)
        {
            EstateOwnerMessage msg = new EstateOwnerMessage();
            msg.AgentID = Owner.ID;
            msg.SessionID = SessionID;
            msg.Invoice = req.Invoice;
            msg.TransactionID = req.TransactionID;
            msg.Method = "estateupdateinfo";
            AgentCircuit circuit;
            if (Circuits.TryGetValue(req.CircuitSceneID, out circuit))
            {
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

                sendEstateList(
                    req.TransactionID,
                    req.Invoice,
                    EstateAccessCodes.EstateManagers,
                    scene.EstateService.EstateManager.All[estateID], 
                    estateID, 
                    req.CircuitSceneID);
                sendEstateList(
                    req.TransactionID,
                    req.Invoice, 
                    EstateAccessCodes.AccessOptions, 
                    scene.EstateService.EstateAccess.All[estateID], 
                    estateID, 
                    req.CircuitSceneID);
                sendEstateList(
                    req.TransactionID,
                    req.Invoice,
                    EstateAccessCodes.AllowedGroups,
                    scene.EstateService.EstateGroup.All[estateID],
                    estateID, 
                    req.CircuitSceneID);
                //sendEstateList(req.TransactionID, req.Invoice, EstateAccessCodes.EstateBans, scene.EstateService.EstateBans.All[estateID], estateID, req.CircuitSceneID);
            }
        }

        void EstateOwner_SetRegionInfo(EstateOwnerMessage req)
        {
            if(req.ParamList.Count < 9)
            {
                return;
            }

            AgentCircuit circuit;
            if (Circuits.TryGetValue(req.CircuitSceneID, out circuit))
            {
                SceneInterface scene = circuit.Scene;

                scene.RegionSettings.BlockTerraform = ParamStringToBool(req.ParamList[0]);
                scene.RegionSettings.BlockFly = ParamStringToBool(req.ParamList[1]);
                scene.RegionSettings.AllowDamage = ParamStringToBool(req.ParamList[2]);
                scene.RegionSettings.AllowLandResell = ParamStringToBool(req.ParamList[3]);
                scene.RegionSettings.AgentLimit = int.Parse(UTF8NoBOM.GetString(req.ParamList[4]));
                scene.RegionSettings.ObjectBonus = float.Parse(UTF8NoBOM.GetString(req.ParamList[5]), CultureInfo.InvariantCulture);
#warning TODO: adjust for correct values
                //scene.RegionSettings.Maturity = int.Parse(req.ParamList[6]);
                scene.RegionSettings.RestrictPushing = ParamStringToBool(req.ParamList[7]);
                scene.RegionSettings.AllowLandJoinDivide = ParamStringToBool(req.ParamList[8]);
            }
        }

        void EstateOwner_TextureDetail(EstateOwnerMessage req)
        {
            foreach(byte[] b in req.ParamList)
            {
                string s = UTF8NoBOM.GetString(b);
                string[] splitfield = s.Split(' ');
                if(splitfield.Length != 2)
                {
                    continue;
                }

                Int16 corner = Int16.Parse(splitfield[0]);
                UUID textureUUID = UUID.Parse(splitfield[1]);
            }
        }

        void EstateOwner_TextureHeights(EstateOwnerMessage req)
        {
            AgentCircuit circuit;
            if (Circuits.TryGetValue(req.CircuitSceneID, out circuit))
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
                    }
                }

                scene.TriggerRegionSettingsChanged();
            }
        }

        void EstateOwner_TextureCommit(EstateOwnerMessage req)
        {

        }

        static bool ParamStringToBool(byte[] b)
        {
            string s = UTF8NoBOM.GetString(b);
            return (s == "1" || s.ToLower() == "y" || s.ToLower() == "yes" || s.ToLower() == "t" || s.ToLower() == "true");
        }

        void EstateOwner_SetRegionTerrain(EstateOwnerMessage req)
        {
            if(req.ParamList.Count != 9)
            {
                return;
            }

            AgentCircuit circuit;
            if (Circuits.TryGetValue(req.CircuitSceneID, out circuit))
            {
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
        }

        void EstateOwner_Restart(EstateOwnerMessage req)
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

        void EstateOwner_EstateChangeCovenantId(EstateOwnerMessage req)
        {
            if(req.ParamList.Count < 1)
            {
                return;
            }

            UUID covenantID = UUID.Parse(UTF8Encoding.UTF8.GetString(req.ParamList[0]));
        }

        void EstateOwner_EstateAccessDelta(EstateOwnerMessage req)
        {
            if(req.ParamList.Count < 3)
            {
                return;
            }
            /*
                        int estateAccessType = Convert.ToInt16(Utils.BytesToString(messagePacket.ParamList[1].Parameter));
                        OnUpdateEstateAccessDeltaRequest(this, messagePacket.MethodData.Invoice, estateAccessType, new UUID(Utils.BytesToString(messagePacket.ParamList[2].Parameter)));
             */
        }

        void EstateOwner_SimulatorMessage(EstateOwnerMessage req)
        {
            if(req.ParamList.Count < 5)
            {
                return;
            }
            UUID invoice = req.Invoice;
            string message = UTF8NoBOM.GetString(req.ParamList[4]);

            AgentCircuit circuit;

            if (Circuits.TryGetValue(req.CircuitSceneID, out circuit))
            {
                foreach (IAgent agent in circuit.Scene.Agents)
                {
                    agent.SendRegionNotice(Owner, message, req.CircuitSceneID);
                }
            }
        }

        void EstateOwner_InstantMessage(EstateOwnerMessage req)
        {
            if (req.ParamList.Count < 2)
            {
                return;
            }
            UUID invoice = req.Invoice;
            string message;

            if(req.ParamList.Count < 5)
            {
                message = UTF8NoBOM.GetString(req.ParamList[1]);
            }
            else
            {
                message = UTF8NoBOM.GetString(req.ParamList[4]);
            }
        }

        void EstateOwner_SetRegionDebug(EstateOwnerMessage req)
        {
            if(req.ParamList.Count < 3)
            {
                return;
            }
            UUID invoice = req.Invoice;
            UUID SenderID = Owner.ID;
            AgentCircuit circuit;

            if (Circuits.TryGetValue(req.CircuitSceneID, out circuit))
            {
                SceneInterface scene = circuit.Scene;
                scene.RegionSettings.DisableScripts = !ParamStringToBool(req.ParamList[0]);
                scene.RegionSettings.DisableCollisions = !ParamStringToBool(req.ParamList[1]);
                scene.RegionSettings.DisablePhysics = !ParamStringToBool(req.ParamList[2]);
                scene.TriggerRegionSettingsChanged();
            }
        }

        void EstateOwner_TeleportHomeUser(EstateOwnerMessage req)
        {
            if(req.ParamList.Count < 2)
            {
                return;
            }
            UUID invoice = req.Invoice;
            UUID senderID = Owner.ID;
            UUID prey = UUID.Parse(UTF8NoBOM.GetString(req.ParamList[1]));
        }

        void EstateOwner_TeleportHomeAllUsers(EstateOwnerMessage req)
        {
            UUID invoice = req.Invoice;
            UUID senderID = Owner.ID;
        }

        void EstateOwner_Colliders(EstateOwnerMessage req)
        {

        }

        void EstateOwner_Scripts(EstateOwnerMessage req)
        {

        }

        void EstateOwner_Terrain(EstateOwnerMessage req)
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
            }
        }

        void EstateOwner_EstateChangeInfo(EstateOwnerMessage req)
        {
            if(req.ParamList.Count < 3)
            {
                return;
            }
            UUID invoice = req.Invoice;
            UUID senderID = req.AgentID;
            UInt32 param1 = UInt32.Parse(UTF8NoBOM.GetString(req.ParamList[1]));
            UInt32 param2 = UInt32.Parse(UTF8NoBOM.GetString(req.ParamList[2]));
        }

        void EstateOwner_Telehub(EstateOwnerMessage req)
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

        void EstateOwner_KickEstate(EstateOwnerMessage req)
        {
            if(req.ParamList.Count < 1)
            {
                return;
            }

            UUID invoice = req.Invoice;
            UUID senderID = Owner.ID;
            UUID prey = UUID.Parse(UTF8NoBOM.GetString(req.ParamList[0]));
        }

        static UTF8Encoding UTF8NoBOM = new UTF8Encoding(false);
    }
}
