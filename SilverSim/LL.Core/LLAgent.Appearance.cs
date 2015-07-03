/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using log4net;
using SilverSim.LL.Messages;
using SilverSim.Main.Common;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Economy;
using SilverSim.ServiceInterfaces.Friends;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.GridUser;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.ServiceInterfaces.Profile;
using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.Types;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Agent;
using SilverSim.Types.Grid;
using SilverSim.Types.IM;
using SilverSim.Types.Inventory;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;
using ThreadedClasses;
using System.Threading;

namespace SilverSim.LL.Core
{
    public partial class LLAgent
    {
        private readonly AgentAttachments m_Attachments = new AgentAttachments();
        private readonly AgentWearables m_Wearables = new AgentWearables();

        public Vector3 Size
        {
            get
            {
                lock (this)
                {
                    return new Vector3(0.3, 0.3, AvatarHeight);
                }
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public AgentAttachments Attachments
        {
            get
            {
                return m_Attachments;
            }
        }

        public AgentWearables Wearables
        {
            get
            {
                return m_Wearables;
            }
            set
            {
                m_Wearables.All = value;
            }
        }

        private readonly ReaderWriterLock m_VisualParamsLock = new ReaderWriterLock();
        private byte[] m_VisualParams = new byte[] { 33, 61, 85, 23, 58, 127, 63, 85, 63, 42, 0, 85, 63, 36, 85, 95, 153, 63, 34, 0, 63, 109, 88, 132, 63, 136, 81, 85, 103, 136, 127, 0, 150, 150, 150, 127, 0, 0, 0, 0, 0, 127, 0, 0, 255, 127, 114, 127, 99, 63, 127, 140, 127, 127, 0, 0, 0, 191, 0, 104, 0, 0, 0, 0, 0, 0, 0, 0, 0, 145, 216, 133, 0, 127, 0, 127, 170, 0, 0, 127, 127, 109, 85, 127, 127, 63, 85, 42, 150, 150, 150, 150, 150, 150, 150, 25, 150, 150, 150, 0, 127, 0, 0, 144, 85, 127, 132, 127, 85, 0, 127, 127, 127, 127, 127, 127, 59, 127, 85, 127, 127, 106, 47, 79, 127, 127, 204, 2, 141, 66, 0, 0, 127, 127, 0, 0, 0, 0, 127, 0, 159, 0, 0, 178, 127, 36, 85, 131, 127, 127, 127, 153, 95, 0, 140, 75, 27, 127, 127, 0, 150, 150, 198, 0, 0, 63, 30, 127, 165, 209, 198, 127, 127, 153, 204, 51, 51, 255, 255, 255, 204, 0, 255, 150, 150, 150, 150, 150, 150, 150, 150, 150, 150, 0, 150, 150, 150, 150, 150, 0, 127, 127, 150, 150, 150, 150, 150, 150, 150, 150, 0, 0, 150, 51, 132, 150, 150, 150 };
        private readonly AppearanceInfo.AvatarTextureData m_TextureHashes = new AppearanceInfo.AvatarTextureData();
        private readonly AppearanceInfo.AvatarTextureData m_Textures = new AppearanceInfo.AvatarTextureData();
        public double AvatarHeight;
        public UInt32 Serial = 1;
        public readonly static int MaxVisualParams = 260;
        private const int NUM_AVATAR_TEXTURES = 21;
        private byte[] m_TextureEntry = new byte[0];

        public AppearanceInfo.AvatarTextureData Textures
        {
            get
            {
                return m_Textures;
            }
            set
            {
                m_Textures.All = value.All;
            }
        }

        public AppearanceInfo.AvatarTextureData TextureHashes
        {
            get
            {
                return m_TextureHashes;
            }
            set
            {
                m_TextureHashes.All = value.All;
            }
        }

        public byte[] VisualParams
        {
            get
            {
                m_VisualParamsLock.AcquireReaderLock(-1);
                try
                {
                    byte[] res = new byte[m_VisualParams.Length];
                    Buffer.BlockCopy(m_VisualParams, 0, res, 0, m_VisualParams.Length);
                    return res;
                }
                finally
                {
                    m_VisualParamsLock.ReleaseReaderLock();
                }
            }
            set
            {
                m_VisualParamsLock.AcquireWriterLock(-1);
                try
                {
                    int VisualParamCount = MaxVisualParams < value.Length ? MaxVisualParams : value.Length;
                    m_VisualParams = new byte[VisualParamCount];
                    Buffer.BlockCopy(value, 0, m_VisualParams, 0, VisualParamCount);
                }
                finally
                {
                    m_VisualParamsLock.ReleaseWriterLock();
                }
            }
        }

        private object m_AppearanceUpdateLock = new object();
        public AppearanceInfo Appearance
        {
            get
            {
                AppearanceInfo ai = new AppearanceInfo();
                ai.Wearables = Wearables;
                ai.VisualParams = VisualParams;
                ai.AvatarHeight = AvatarHeight;
                //ai.Attachments = Attachments;
                ai.Serial = Serial;
                ai.AvatarTextures.All = Textures.All;
                return ai;
            }

            set
            {
                /* check for assets being valid */
                Dictionary<WearableType, List<AgentWearables.WearableInfo>> aw = value.Wearables;
                foreach(KeyValuePair<WearableType, List<AgentWearables.WearableInfo>> kvp in aw)
                {
                    List<AgentWearables.WearableInfo> lwi = kvp.Value;
                    for (int c = 0; c < kvp.Value.Count;)
                    {
                        if (lwi[c].AssetID.Equals(UUID.Zero))
                        {
                            try
                            {
                                InventoryItem item = InventoryService.Item[ID, lwi[c].ItemID];
                                AgentWearables.WearableInfo wi = lwi[c];
                                wi.AssetID = item.AssetID;
                                lwi[c++] = wi;
                            }
                            catch
                            {
                                lwi.RemoveAt(c);
                            }
                        }
                        else
                        {
                            ++c;
                        }
                    }
                }
                lock (m_AppearanceUpdateLock)
                {
                    Wearables.All = aw;
                    VisualParams = value.VisualParams;
                    Serial = value.Serial;
                    AvatarHeight = value.AvatarHeight;
                    Textures.All = value.AvatarTextures.All;
                    //value.Attachments;
                }
            }
        }

        [PacketHandler(MessageType.AgentSetAppearance)]
        void HandleSetAgentAppearance(Message p)
        {
            Messages.Appearance.AgentSetAppearance m = (Messages.Appearance.AgentSetAppearance)p;
            if (m.AgentID != ID || m.SessionID != m.CircuitSessionID)
            {
                return;
            }

            foreach(Messages.Appearance.AgentSetAppearance.WearableDataEntry d in m.WearableData)
            {
                TextureHashes[d.TextureIndex] = d.CacheID;
            }

            TextureEntry te = new TextureEntry(m.ObjectData);
            int tidx;
            if (te.DefaultTexture != null)
            {
                for (tidx = 0; tidx < Math.Min(te.FaceTextures.Length, NUM_AVATAR_TEXTURES); ++tidx)
                {
                    if (te.FaceTextures[tidx] != null)
                    {
                        Textures[tidx] = te.FaceTextures[tidx].TextureID;
                    }
                    else
                    {
                        Textures[tidx] = te.DefaultTexture.TextureID;
                    }
                }
            }

            m_TextureEntry = m.ObjectData;
            
            VisualParams = m.VisualParams;
            Messages.Appearance.AvatarAppearance res = new Messages.Appearance.AvatarAppearance();
            res.Sender = ID;
            res.IsTrial = false;
            res.VisualParams = VisualParams;
            res.TextureEntry = m_TextureEntry;

            SendMessageAlways(res, SceneID);
        }

        [PacketHandler(MessageType.AgentWearablesRequest)]
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
