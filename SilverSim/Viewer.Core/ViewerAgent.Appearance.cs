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

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Primitive;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Appearance;
using SilverSim.Viewer.Messages.Generic;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        [GenericMessageHandler("avatartexturesrequest")]
        public void HandleAvatarTexturesRequest(Message p)
        {
            var gm = (GenericMessage)p;
            if (gm.AgentID != ID || gm.SessionID != gm.CircuitSessionID || gm.ParamList.Count < 1)
            {
                return;
            }

            UUID avatarId;
            if(!UUID.TryParse(gm.ParamList[0].FromUTF8Bytes(), out avatarId))
            {
                return;
            }

            IAgent agent;
            AgentCircuit circuit;
            SceneInterface scene;
            if(!Circuits.TryGetValue(gm.CircuitSceneID, out circuit))
            {
                return;
            }

            scene = circuit.Scene;

            if(scene == null)
            {
                return;
            }

            if(scene.RootAgents.TryGetValue(avatarId, out agent))
            {
                SendMessageAlways(agent.GetAvatarAppearanceMsg(), SceneID);
            }
        }

        [PacketHandler(MessageType.AgentSetAppearance)]
        public void HandleSetAgentAppearance(Message p)
        {
            var m = (AgentSetAppearance)p;
            if (m.AgentID != ID || m.SessionID != m.CircuitSessionID)
            {
                return;
            }

            foreach(var d in m.WearableData)
            {
                TextureHashes[d.TextureIndex] = d.CacheID;
            }

            var te = new TextureEntry(m.ObjectData);
            int tidx;
            if (te.DefaultTexture != null)
            {
                for (tidx = 0; tidx < Math.Min(TextureEntry.MAX_TEXTURE_FACES, NUM_AVATAR_TEXTURES); ++tidx)
                {
                    TextureEntryFace face;
                    Textures[tidx] = te.TryGetValue((uint)tidx, out face) ?
                        face.TextureID :
                        te.DefaultTexture.TextureID;
                }
            }

            SetTextureEntryBytes(m.ObjectData);

            VisualParams = m.VisualParams;
            Size = m.Size;

            HandleAppearanceUpdate(this);
        }

        [PacketHandler(MessageType.AgentWearablesRequest)]
        public void HandleAgentWearablesRequest(Message p)
        {
            var m = (AgentWearablesRequest)p;
            if(m.AgentID != ID || m.SessionID != m.CircuitSessionID)
            {
                return;
            }

            var awu = new AgentWearablesUpdate()
            {
                AgentID = m.AgentID,
                SessionID = m.SessionID,
                SerialNum = Serial
            };
            foreach(var kvp in Wearables.All)
            {
                foreach (var wi in kvp.Value)
                {
                    var d = new AgentWearablesUpdate.WearableDataEntry()
                    {
                        ItemID = wi.ItemID,
                        AssetID = wi.AssetID,
                        WearableType = kvp.Key
                    };
                    awu.WearableData.Add(d);
                }
            }
            SendMessageAlways(awu, m.CircuitSceneID);
        }

        [PacketHandler(MessageType.AgentIsNowWearing)]
        public void HandleAgentIsNowWearing(Message p)
        {
            var m = (AgentIsNowWearing)p;
            if (m.AgentID != ID || m.SessionID != m.CircuitSessionID)
            {
                return;
            }

            var wearables = new Dictionary<WearableType,List<AgentWearables.WearableInfo>>();
            for(var c = WearableType.Shape; c < WearableType.NumWearables; ++c)
            {
                wearables[c] = new List<AgentWearables.WearableInfo>();
            }
            foreach(var d in m.WearableData)
            {
                try
                {
                    var item = InventoryService.Item[ID, d.ItemID];
                    wearables[d.WearableType].Add(new AgentWearables.WearableInfo(d.ItemID, item.AssetID));
                }
                catch
                {
                    /* prevent us from bailing out if should there be an inventory item error */
                }
            }

            Wearables.All = wearables;
        }

        [PacketHandler(MessageType.AgentCachedTexture)]
        public void HandleAgentCachedTexture(Message p)
        {
            var m = (AgentCachedTexture)p;
            if (m.AgentID != ID || m.SessionID != m.CircuitSessionID)
            {
                return;
            }

            var res = new AgentCachedTextureResponse()
            {
                AgentID = m.AgentID,
                SessionID = m.SessionID,
                SerialNum = ++Serial,
                WearableData = new List<AgentCachedTextureResponse.WearableDataEntry>((int)WearableType.NumWearables)
            };

            /* respond with no caching at all for now */
            var textures = Textures.All;
            foreach(var wde in m.WearableData)
            {
                res.WearableData.Add(new AgentCachedTextureResponse.WearableDataEntry(wde.TextureIndex, textures[wde.TextureIndex]));
            }

            SendMessageAlways(res, m.CircuitSceneID);
        }

        private void HandleAppearanceUpdate(IAgent agent)
        {
            AgentCircuit circuit;
            if (Circuits.TryGetValue(m_CurrentSceneID, out circuit))
            {
                circuit.Scene.SendAgentAppearanceToAllAgents(this);
            }
        }
    }
}
