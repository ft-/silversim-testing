// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;
using SilverSim.Types.Account;
using SilverSim.Types.Grid;
using SilverSim.Types.Agent;
using System.IO;
using SilverSim.StructuredData.JSON;
using SilverSim.Types.Asset.Format;
using ThreadedClasses;

namespace SilverSim.StructuredData.Agent
{
    public class PostData
    {
        public DestinationInfo Destination = new DestinationInfo();
        public ClientInfo Client = new ClientInfo();
        public SessionInfo Session = new SessionInfo();
        public CircuitInfo Circuit = new CircuitInfo();
        public AppearanceInfo Appearance = new AppearanceInfo();
        public UserAccount Account = new UserAccount();

        public class InvalidAgentPostSerialization : Exception
        {
            public InvalidAgentPostSerialization(string message)
                : base(message)
            {

            }
        }

        public PostData()
        {

        }

        public static PostData Deserialize(Stream input)
        {
            PostData agentparams = new PostData();
            IValue json = JSON.JSON.Deserialize(input);
            if (!(json is Map))
            {
                throw new InvalidAgentPostSerialization("Invalid JSON AgentPostData");
            }
            Map parms = (Map)json;

            /*-----------------------------------------------------------------*/
            /* SessionInfo */
            if (parms.ContainsKey("service_session_id"))
            {
                agentparams.Session.ServiceSessionID = parms["service_session_id"].ToString();
            }
            agentparams.Session.SessionID = parms["session_id"].AsUUID;
            agentparams.Session.SecureSessionID = parms["secure_session_id"].AsUUID;

            /*-----------------------------------------------------------------*/
            /* Circuit Info */
            agentparams.Circuit.CircuitCode = parms["circuit_code"].AsUInt;
            agentparams.Circuit.CapsPath = parms["caps_path"].ToString();
            if (parms.ContainsKey("child"))
            {
                agentparams.Circuit.IsChild = parms["child"].AsBoolean;
            }
            else
            {
                agentparams.Circuit.IsChild = false;
            }
            if(parms.ContainsKey("children_seeds"))
            {
                AnArray children_seeds = (AnArray)parms["children_seeds"];
                foreach(IValue seedv in children_seeds)
                {
                    Map seed = (Map)seedv;
                    agentparams.Circuit.ChildrenCapSeeds[UInt64.Parse(seed["handle"].ToString())] = seed["seed"].ToString();
                }
            }

            /*-----------------------------------------------------------------*/
            /* Destination */
            agentparams.Destination = new DestinationInfo();
            agentparams.Destination.ID = parms["destination_uuid"].AsUUID;
            agentparams.Destination.Location.X = parms["destination_x"].AsUInt;
            agentparams.Destination.Location.Y = parms["destination_y"].AsUInt;
            agentparams.Destination.Name = "HG Destination";
            agentparams.Destination.Position = parms["start_pos"].AsVector3;
            if (parms.ContainsKey("teleport_flags"))
            {
                uint tpflags = parms["teleport_flags"].AsUInt;
                agentparams.Destination.TeleportFlags = (TeleportFlags)(tpflags);
            }
            else
            {
                agentparams.Destination.TeleportFlags = TeleportFlags.None;
            }
            if (parms.ContainsKey("gatekeeper_serveruri"))
            {
                agentparams.Destination.ServerURI = parms["destination_serveruri"].ToString();
                agentparams.Destination.GatekeeperURI = parms["gatekeeper_serveruri"].ToString();
                agentparams.Destination.LocalToGrid = false;
            }
            else
            {
                agentparams.Destination.GatekeeperURI = string.Empty;
                agentparams.Destination.LocalToGrid = true;
            }

            /*-----------------------------------------------------------------*/
            /* Account */
            agentparams.Account = new UserAccount();
            agentparams.Account.Principal.ID = parms["agent_id"].AsUUID;
            agentparams.Account.Principal.FirstName = parms["first_name"].ToString();
            agentparams.Account.Principal.LastName = parms["last_name"].ToString();
            agentparams.Account.IsLocalToGrid = false;
            agentparams.Account.UserLevel = 0;

            /*-----------------------------------------------------------------*/
            /* Client Info */
            agentparams.Client = new ClientInfo();
            agentparams.Client.ClientIP = parms["client_ip"].ToString();
            agentparams.Client.ClientVersion = parms["viewer"].ToString();
            agentparams.Client.Channel = parms["channel"].ToString();
            agentparams.Client.Mac = parms["mac"].ToString();
            agentparams.Client.ID0 = parms["id0"].ToString();

            /*-----------------------------------------------------------------*/
            /* Service URLs */
            if (parms.ContainsKey("serviceurls") && parms["serviceurls"] is Map)
            {
                foreach (KeyValuePair<string, IValue> kvp in (Map)(parms["serviceurls"]))
                {
                    agentparams.Account.ServiceURLs.Add(kvp.Key, kvp.Value.ToString());
                }
            }
            else if (parms.ContainsKey("service_urls") && parms["service_urls"] is AnArray)
            {
                AnArray array = (AnArray)parms["service_urls"];
                if (array.Count % 2 != 0)
                {
                    throw new InvalidAgentPostSerialization("Invalid service_urls block in AgentPostData");
                }
                int i;
                for (i = 0; i < array.Count; i += 2)
                {
                    agentparams.Account.ServiceURLs.Add(array[i].ToString(), array[i + 1].ToString());
                }
            }

            if (agentparams.Account.ServiceURLs.ContainsKey("GatekeeperURI"))
            {
                if (agentparams.Account.ServiceURLs["GatekeeperURI"] == "" || agentparams.Account.ServiceURLs["GatekeeperURI"] == "/")
                {
                    agentparams.Account.ServiceURLs["GatekeeperURI"] = agentparams.Account.ServiceURLs["HomeURI"];
                }
            }

            if(agentparams.Account.ServiceURLs.ContainsKey("HomeURI"))
            {
                try
                {
                    agentparams.Account.Principal.HomeURI = new Uri(agentparams.Account.ServiceURLs["HomeURI"]);
                }
                catch
                {

                }
            }

            /*-----------------------------------------------------------------*/
            /* Appearance */
            Map appearancePack = (Map)parms["packed_appearance"];
            agentparams.Appearance.AvatarHeight = appearancePack["height"].AsReal;
            //agentparams.Appearance.Serial = appearancePack["serial"].AsInt;

            {
                AnArray vParams = (AnArray)appearancePack["visualparams"];
                byte[] visualParams = new byte[vParams.Count];

                int i;
                for (i = 0; i < vParams.Count; ++i)
                {
                    visualParams[i] = (byte)vParams[i].AsUInt;
                }
                agentparams.Appearance.VisualParams = visualParams;
            }

            {
                AnArray texArray = (AnArray)appearancePack["textures"];
                int i;
                for (i = 0; i < AppearanceInfo.AvatarTextureData.TextureCount; ++i)
                {
                    agentparams.Appearance.AvatarTextures[i] = texArray[i].AsUUID;
                }
            }

            {
                int i;
                uint n;
                AnArray wearables = (AnArray)appearancePack["wearables"];
                for (i = 0; i < (int)WearableType.NumWearables; ++i)
                {
                    AnArray ar;
                    try
                    {
                        ar = (AnArray)wearables[i];
                    }
                    catch
                    {
                        continue;
                    }
                    n = 0;
                    foreach (IValue val in ar)
                    {
                        AgentWearables.WearableInfo wi = new AgentWearables.WearableInfo();
                        Map wp = (Map)val;
                        wi.ItemID = wp["item"].AsUUID;
                        if (wp.ContainsKey("asset"))
                        {
                            wi.AssetID = wp["asset"].AsUUID;
                        }
                        else
                        {
                            wi.AssetID = UUID.Zero;
                        }
                        WearableType type = (WearableType)i;
                        agentparams.Appearance.Wearables[type, n++] = wi;
                    }
                }
            }

            {
                foreach (IValue apv in (AnArray)appearancePack["attachments"])
                {
                    Map ap = (Map)apv;
                    agentparams.Appearance.Attachments[(AttachmentPoint)uint.Parse(ap["point"].ToString())][ap["item"].AsUUID] = UUID.Zero;
                }
            }

            return agentparams;
        }

        private void WriteJSONString(TextWriter w, string name, string value)
        {
            w.Write(string.Format("\"{0}\":\"{1}\"", JSON.JSON.SerializeString(name), JSON.JSON.SerializeString(value)));
        }
        private void WriteJSONString(TextWriter w, string name, UUID value)
        {
            w.Write(string.Format("\"{0}\":\"{1}\"", JSON.JSON.SerializeString(name), JSON.JSON.SerializeString((string)value)));
        }
        private void WriteJSONValue(TextWriter w, string name, uint value)
        {
            w.Write(string.Format("\"{0}\":{1}", JSON.JSON.SerializeString(name), value));
        }
        private void WriteJSONValue(TextWriter w, string name, int value)
        {
            w.Write(string.Format("\"{0}\":{1}", JSON.JSON.SerializeString(name), value));
        }
        private void WriteJSONValue(TextWriter w, string name, double value)
        {
            w.Write(string.Format("\"{0}\":{1}", JSON.JSON.SerializeString(name), value));
        }
        private void WriteJSONValue(TextWriter w, string name, bool value)
        {
            if (value)
            {
                w.Write(string.Format("\"{0}\":true", JSON.JSON.SerializeString(name)));
            }
            else
            {
                w.Write(string.Format("\"{0}\":false", JSON.JSON.SerializeString(name)));
            }
        }

        public void Serialize(Stream output)
        {
            string prefix;
            TextWriter w = new StreamWriter(output);
            w.Write("{");
            /*-----------------------------------------------------------------*/
            /* SessionInfo */
            WriteJSONString(w, "service_session_id", Session.ServiceSessionID); w.Write(",");
            WriteJSONString(w, "session_id", Session.SessionID); w.Write(",");
            WriteJSONString(w, "secure_session_id", Session.SecureSessionID); w.Write(",");

            /*-----------------------------------------------------------------*/
            /* Circuit Info */
            WriteJSONString(w, "circuit_code", Circuit.CircuitCode.ToString()); w.Write(",");
            WriteJSONString(w, "caps_path", Circuit.CapsPath.ToString()); w.Write(",");
            WriteJSONValue(w, "child", Circuit.IsChild); w.Write(",");
            w.Write("\"children_seeds\":[");
            prefix = "";
            foreach(KeyValuePair<UInt64, string> kvp in Circuit.ChildrenCapSeeds)
            {
                w.Write(prefix);
                w.Write("{");
                WriteJSONString(w, "handle", kvp.Key.ToString()); w.Write(",");
                WriteJSONString(w, "seed", kvp.Value.ToString());
                w.Write("}");
                prefix = ",";
            }
            w.Write("],");

            /*-----------------------------------------------------------------*/
            /* Destination */
            WriteJSONString(w, "destination_uuid", Destination.ID); w.Write(",");
            WriteJSONString(w, "destination_x", Destination.Location.X.ToString()); w.Write(",");
            WriteJSONString(w, "destination_y", Destination.Location.Y.ToString()); w.Write(",");
            WriteJSONString(w, "start_pos", Destination.Position.ToString()); w.Write(",");
            WriteJSONString(w, "teleport_flags", ((uint)Destination.TeleportFlags).ToString());
            if(!Destination.LocalToGrid)
            {
                WriteJSONString(w, "destination_serveruri", Destination.ServerURI); w.Write(",");
                WriteJSONString(w, "gatekeeper_serveruri", Destination.GatekeeperURI); w.Write(",");
            }
            else
            {

            }

            /*-----------------------------------------------------------------*/
            /* Account */
            WriteJSONString(w, "agent_id", Account.Principal.ID); w.Write(",");
            WriteJSONString(w, "first_name", Account.Principal.FirstName); w.Write(",");
            WriteJSONString(w, "last_name", Account.Principal.LastName); w.Write(",");

            /*-----------------------------------------------------------------*/
            /* Client Info */
            WriteJSONString(w, "client_ip", Client.ClientIP); w.Write(",");
            WriteJSONString(w, "viewer", Client.ClientVersion); w.Write(",");
            WriteJSONString(w, "channel", Client.Channel); w.Write(",");
            WriteJSONString(w, "mac", Client.Mac); w.Write(",");
            WriteJSONString(w, "id0", Client.ID0); w.Write(",");

            /*-----------------------------------------------------------------*/
            /* Service URLs */
            w.Write("serviceurls:{");
            prefix = "";
            foreach (KeyValuePair<string, string> kvp in Account.ServiceURLs)
            {
                w.Write(prefix);
                WriteJSONString(w, kvp.Key, kvp.Value);
                prefix = ",";
            }
            w.Write("},");
            w.Write("service_urls:[");
            prefix = "";
            foreach (KeyValuePair<string, string> kvp in Account.ServiceURLs)
            {
                w.Write(prefix);
                w.Write(string.Format("\"{0}\", \"{1}\"", JSON.JSON.SerializeString(kvp.Key), JSON.JSON.SerializeString(kvp.Value)));
                prefix = ",";
            }
            w.Write("],");

            w.Write("\"packed_appearance\":{");
            WriteJSONValue(w, "height", Appearance.AvatarHeight); w.Write(",");
            WriteJSONValue(w, "serial", Appearance.Serial); w.Write(",");
            string vParams = string.Empty;
            foreach(byte v in Appearance.VisualParams)
            {
                if(vParams != string.Empty)
                {
                    vParams += ",";
                }
                vParams += v.ToString();
            }
            WriteJSONString(w, "visualparams", vParams); w.Write(",");
            w.Write("\"textures\":[");
            prefix = "";
            {
                int i;
                for (i = 0; i < AppearanceInfo.AvatarTextureData.TextureCount; ++i)
                { 
                    w.Write(prefix);
                    w.Write(string.Format("\"{0}\"", Appearance.AvatarTextures[i]));
                    prefix = ",";
                }
            }
            w.Write("],\"wearables\":[");
            {
                uint i;
                for (i = 0; i < (uint)WearableType.NumWearables; ++i)
                {
                    if(i != 0)
                    {
                        w.Write(",");
                    }
                    List<AgentWearables.WearableInfo> wearables = Appearance.Wearables[(WearableType)i];
                    w.Write("[");
                    prefix = "";
                    foreach (AgentWearables.WearableInfo wi in wearables)
                    {
                        w.Write(prefix);
                        w.Write("{");
                        WriteJSONString(w, "item", wi.ItemID);
                        if(wi.AssetID != UUID.Zero)
                        {
                            w.Write(",");
                            WriteJSONString(w, "asset", wi.AssetID);
                        }
                        w.Write("}");
                        prefix=",";
                    }
                    w.Write("]");
                }
            }
            w.Write("], \"attachments\":[");
            {
                prefix = "";
                foreach(KeyValuePair<AttachmentPoint, RwLockedDictionary<UUID, UUID>> kvp in Appearance.Attachments)
                {
                    foreach (KeyValuePair<UUID, UUID> kvpInner in kvp.Value)
                    {
                        w.Write(prefix);
                        w.Write("{");
                        WriteJSONValue(w, "point", (uint)kvp.Key);
                        WriteJSONString(w, "item", kvpInner.Key);
                        if (kvpInner.Value != UUID.Zero)
                        {
                            WriteJSONString(w, "asset", kvpInner.Value);
                        }
                        w.Write("}");
                        prefix = ",";
                    }
                }
            }
            w.Write("]");

            w.Write("}"); /* packed_appearance */
            w.Write("}");
        }
    }
}
