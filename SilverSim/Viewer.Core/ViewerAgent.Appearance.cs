// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Inventory;
using SilverSim.Types.Primitive;
using SilverSim.Viewer.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        private byte[] m_TextureEntry = new byte[0];

        [PacketHandler(MessageType.AgentSetAppearance)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleSetAgentAppearance(Message p)
        {
            Messages.Appearance.AgentSetAppearance m = (Messages.Appearance.AgentSetAppearance)p;
            if (m.AgentID != ID || m.SessionID != m.CircuitSessionID)
            {
                return;
            }
#if DEBUG
            m_Log.DebugFormat("Processing SetAgentAppearance for {0}", Owner.FullName);
#endif

            foreach(Messages.Appearance.AgentSetAppearance.WearableDataEntry d in m.WearableData)
            {
                TextureHashes[d.TextureIndex] = d.CacheID;
            }

            TextureEntry te = new TextureEntry(m.ObjectData);
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

            m_TextureEntry = m.ObjectData;
            
            VisualParams = m.VisualParams;
            Messages.Appearance.AvatarAppearance res = new Messages.Appearance.AvatarAppearance();
            res.Sender = ID;
            res.IsTrial = false;
            res.VisualParams = VisualParams;
            res.TextureEntry = m_TextureEntry;
            Messages.Appearance.AvatarAppearance.AppearanceDataEntry appearanceData = new Messages.Appearance.AvatarAppearance.AppearanceDataEntry();
            appearanceData.CofVersion = 0;
            appearanceData.AppearanceVersion = 1;
            res.AppearanceData.Add(appearanceData);

            SendMessageAlways(res, SceneID);
        }

        [PacketHandler(MessageType.AgentWearablesRequest)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleAgentWearablesRequest(Message p)
        {
            Messages.Appearance.AgentWearablesRequest m = (Messages.Appearance.AgentWearablesRequest)p;
            if(m.AgentID != ID || m.SessionID != m.CircuitSessionID)
            {
                return;
            }

            Messages.Appearance.AgentWearablesUpdate awu = new Messages.Appearance.AgentWearablesUpdate();
            awu.AgentID = m.AgentID;
            awu.SessionID = m.SessionID;
            awu.SerialNum = Serial;
            Dictionary<WearableType, List<AgentWearables.WearableInfo>> wearables = Wearables.All;
            foreach(KeyValuePair<WearableType, List<AgentWearables.WearableInfo>> kvp in wearables)
            {
                foreach (AgentWearables.WearableInfo wi in kvp.Value)
                {
                    Messages.Appearance.AgentWearablesUpdate.WearableDataEntry d = new Messages.Appearance.AgentWearablesUpdate.WearableDataEntry();
                    d.ItemID = wi.ItemID;
                    d.AssetID = wi.AssetID;
                    d.WearableType = kvp.Key;
                    awu.WearableData.Add(d);
                }
            }
            SendMessageAlways(awu, m.CircuitSceneID);
        }

        [PacketHandler(MessageType.AgentIsNowWearing)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleAgentIsNowWearing(Message p)
        {
            Messages.Appearance.AgentIsNowWearing m = (Messages.Appearance.AgentIsNowWearing)p;
            if (m.AgentID != ID || m.SessionID != m.CircuitSessionID)
            {
                return;
            }

            Dictionary<WearableType, List<AgentWearables.WearableInfo>> wearables = new Dictionary<WearableType,List<AgentWearables.WearableInfo>>();
            for(WearableType c = WearableType.Shape; c < WearableType.NumWearables; ++c)
            {
                wearables[c] = new List<AgentWearables.WearableInfo>();
            }
            foreach(Messages.Appearance.AgentIsNowWearing.WearableDataEntry d in m.WearableData)
            {
                try
                {
                    InventoryItem item = InventoryService.Item[ID, d.ItemID];
                    wearables[d.WearableType].Add(new AgentWearables.WearableInfo(d.ItemID, item.AssetID));
                }
                catch
                {

                }
            }

            Wearables.All = wearables;
        }

        [PacketHandler(MessageType.AgentCachedTexture)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleAgentCachedTexture(Message p)
        {
            Messages.Appearance.AgentCachedTexture m = (Messages.Appearance.AgentCachedTexture)p;
            if (m.AgentID != ID || m.SessionID != m.CircuitSessionID)
            {
                return;
            }

            Messages.Appearance.AgentCachedTextureResponse res = new Messages.Appearance.AgentCachedTextureResponse();
            res.AgentID = m.AgentID;
            res.SessionID = m.SessionID;
            res.SerialNum = ++Serial;
            res.WearableData = new List<Messages.Appearance.AgentCachedTextureResponse.WearableDataEntry>((int)WearableType.NumWearables);

            /* respond with no caching at all for now */
            foreach(Messages.Appearance.AgentCachedTexture.WearableDataEntry wde in m.WearableData)
            {
                res.WearableData.Add(new Messages.Appearance.AgentCachedTextureResponse.WearableDataEntry(wde.TextureIndex, UUID.Zero));
            }

            SendMessageAlways(res, m.CircuitSceneID);
        }
    }
}
