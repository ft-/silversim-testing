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

using log4net;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SilverSim.Scene.Agent.Bakery
{
    public class BakeCache : IDisposable
    {
        private static readonly ILog m_Log = LogManager.GetLogger("AGENT BAKING");
        private readonly object m_Lock = new object();
        public AssetServiceInterface AssetService { get; }
        public UUID CurrentOutfitFolderID = UUID.Zero;
        public int AppearanceSerial;
        public readonly UUID[] AvatarTextures = new UUID[AppearanceInfo.AvatarTextureData.TextureCount];
        public readonly Dictionary<WearableType, List<AgentWearables.WearableInfo>> AvatarWearables = new Dictionary<WearableType, List<AgentWearables.WearableInfo>>();

        public BakeCache(AssetServiceInterface assetService)
        {
            AssetService = assetService;
        }

        ~BakeCache()
        {
            Dispose();
        }

        private readonly Dictionary<UUID, AbstractSubBaker> m_SubBakers = new Dictionary<UUID, AbstractSubBaker>();
        private readonly Dictionary<UUID, OutfitItem> m_Items = new Dictionary<UUID, OutfitItem>();

        public List<AbstractSubBaker> SubBakers
        {
            get
            {
                lock (m_Lock)
                {
                    return new List<AbstractSubBaker>(m_SubBakers.Values);
                }
            }
        }

        public List<Wearable> Wearables
        {
            get
            {
                lock(m_Lock)
                {
                    return new List<Wearable>(from item in m_Items select item.Value.Wearable);
                }
            }
        }
        
        public void SetCurrentOutfit(Dictionary<UUID, OutfitItem> outfititems)
        {
            lock (m_Lock)
            {
                List<UUID> removesubbakers = new List<UUID>(from subid in m_SubBakers.Keys where !outfititems.ContainsKey(subid) select subid);
                foreach (UUID itemid in removesubbakers)
                {
                    AbstractSubBaker sub;
                    if (m_SubBakers.TryGetValue(itemid, out sub))
                    {
                        m_SubBakers.Remove(itemid);
                        sub.Dispose();
                    }
                }

                foreach (KeyValuePair<UUID, OutfitItem> kvp in outfititems)
                {
                    AbstractSubBaker subbaker;
                    if (m_SubBakers.TryGetValue(kvp.Key, out subbaker))
                    {
                        subbaker.Ordinal = kvp.Value.Ordinal;
                    }
                    else
                    {
                        subbaker = kvp.Value.Wearable.CreateSubBaker();
                        if (subbaker != null)
                        {
                            subbaker.Ordinal = kvp.Value.Ordinal;
                            m_SubBakers.Add(kvp.Key, subbaker);
                        }
                    }
                }

                foreach (OutfitItem item in outfititems.Values.OrderBy(item => item.Ordinal))
                {
                    foreach (KeyValuePair<AvatarTextureIndex, UUID> tex in item.Wearable.Textures)
                    {
                        AvatarTextures[(int)tex.Key] = tex.Value;
                    }
                }

                m_Items.Clear();
                foreach(KeyValuePair<UUID, OutfitItem> item in outfititems)
                {
                    m_Items.Add(item.Key, item.Value);
                }
            }
        }

        public bool IsBaked
        {
            get
            {
                bool baked = true;
                lock (m_Lock)
                {
                    foreach(AbstractSubBaker subbaker in m_SubBakers.Values)
                    {
                        baked = baked && subbaker.IsBaked;
                    }
                }
                return baked;
            }
        }

        public void Dispose()
        {
            List<AbstractSubBaker> values;
            lock (m_Lock)
            {
                values = new List<AbstractSubBaker>(m_SubBakers.Values);
                SubBakers.Clear();
            }
            foreach(AbstractSubBaker sub in values)
            {
                sub.Dispose();
            }
        }

        #region Load from current outfit
        private static int LinkDescriptionToInt(string desc)
        {
            int res = 0;
            if (desc.StartsWith("@") && int.TryParse(desc.Substring(1), out res))
            {
                return res;
            }
            return 0;
        }

        public void LoadFromCurrentOutfit(UUI principal, InventoryServiceInterface inventoryService, AssetServiceInterface assetService, Action<string> logOutput = null)
        {
            lock (m_Lock)
            {
                if (CurrentOutfitFolderID == UUID.Zero)
                {
                    InventoryFolder folder;
                    if (!inventoryService.Folder.TryGetValue(principal.ID, AssetType.CurrentOutfitFolder, out folder))
                    {
                        throw new BakeErrorException("Outfit folder missing");
                    }
                    CurrentOutfitFolderID = folder.ID;
                }

                InventoryFolderContent folderContent = inventoryService.Folder.Content[principal.ID, CurrentOutfitFolderID];
                AppearanceSerial = folderContent.Version;

                var items = new List<InventoryItem>();
                var itemlinks = new List<UUID>();
                foreach (var item in folderContent.Items)
                {
                    if (item.AssetType == AssetType.Link)
                    {
                        items.Add(item);
                        itemlinks.Add(item.AssetID);
                    }
                }

                var actualItems = inventoryService.Item[principal.ID, itemlinks];
                var actualItemsInDict = new Dictionary<UUID, InventoryItem>();
                foreach (var item in actualItems)
                {
                    actualItemsInDict.Add(item.ID, item);
                }

                var outfitItems = new Dictionary<UUID, OutfitItem>();

                logOutput?.Invoke(string.Format("Processing assets for baking agent {0}", principal.FullName));

                AvatarWearables.Clear();

                foreach (var linkItem in items)
                {
                    InventoryItem actualItem;
                    if (actualItemsInDict.TryGetValue(linkItem.AssetID, out actualItem) &&
                        (actualItem.AssetType == AssetType.Clothing || actualItem.AssetType == AssetType.Bodypart))
                    {
                        AssetData outfitData;
                        Wearable wearableData;
                        if (assetService.TryGetValue(actualItem.AssetID, out outfitData))
                        {
                            try
                            {
                                wearableData = new Wearable(outfitData);
                            }
                            catch (Exception e)
                            {
                                string info = string.Format("Asset {0} for agent {1} ({2}) failed to decode as wearable", actualItem.AssetID, principal.FullName, principal.ID);
                                m_Log.ErrorFormat(info, e);
                                throw new BakeErrorException(info, e);
                            }

                            var oitem = new OutfitItem(LinkDescriptionToInt(linkItem.Description), wearableData);
                            outfitItems.Add(linkItem.AssetID, oitem);
                            List<AgentWearables.WearableInfo> wearables;
                            if (!AvatarWearables.TryGetValue(wearableData.Type, out wearables))
                            {
                                wearables = new List<AgentWearables.WearableInfo>();
                                AvatarWearables.Add(wearableData.Type, wearables);
                            }
                            wearables.Add(new AgentWearables.WearableInfo(linkItem.AssetID, actualItem.AssetID));
                        }
                    }
                }
                SetCurrentOutfit(outfitItems);
            }
        }
        #endregion

        public AppearanceInfo Bake(AssetServiceInterface destService)
        {
            BakeOutput bakes;
            using (var proc = new BakeProcessor())
            {
                bakes = proc.Process(this, AssetService);
            }

            destService.Store(bakes.EyeBake);
            destService.Store(bakes.HeadBake);
            destService.Store(bakes.UpperBake);
            destService.Store(bakes.LowerBake);
            destService.Store(bakes.HairBake);

            AvatarTextures[(int)AvatarTextureIndex.EyesBaked] = bakes.EyeBake.ID;
            AvatarTextures[(int)AvatarTextureIndex.HairBaked] = bakes.HairBake.ID;
            AvatarTextures[(int)AvatarTextureIndex.UpperBaked] = bakes.UpperBake.ID;
            AvatarTextures[(int)AvatarTextureIndex.LowerBaked] = bakes.LowerBake.ID;
            AvatarTextures[(int)AvatarTextureIndex.HeadBaked] = bakes.HeadBake.ID;

            if (bakes.SkirtBake != null)
            {
                destService.Store(bakes.SkirtBake);
                AvatarTextures[(int)AvatarTextureIndex.SkirtBaked] = bakes.SkirtBake.ID;
            }
            else
            {
                AvatarTextures[(int)AvatarTextureIndex.SkirtBaked] = AppearanceInfo.AvatarTextureData.DefaultAvatarTextureID;
            }

            var info = new AppearanceInfo();
            info.Serial = AppearanceSerial;
            info.VisualParams = bakes.VisualParams;
            info.AvatarTextures.All = AvatarTextures;
            info.Wearables.All = AvatarWearables;
            //info.AvatarHeight;
            return info;
        }
    }
}
