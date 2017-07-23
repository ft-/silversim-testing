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
using System.Drawing;
using System.Runtime.Serialization;

namespace SilverSim.Scene.Types.Agent
{
    public static partial class AgentBakeAppearance
    {
        private static readonly ILog m_BakeLog = LogManager.GetLogger("AVATAR BAKING");
        private static readonly UUID IMG_INVISIBLE = new UUID("3a367d1c-bef1-6d43-7595-e88c1e3aadb3");

        private enum BakeType
        {
            Head,
            UpperBody,
            LowerBody,
            Eyes,
            Skirt,
            Hair
        }

        private class OutfitItem
        {
            public InventoryItem ActualItem;
            public Wearable WearableData;

            public OutfitItem(InventoryItem linkItem)
            {
                ActualItem = linkItem;
            }
        }

        private class BakeStatus : IDisposable
        {
            public readonly Dictionary<UUID, OutfitItem> OutfitItems = new Dictionary<UUID, OutfitItem>();
            public readonly Dictionary<UUID, Image> Textures = new Dictionary<UUID, Image>();
            public readonly Dictionary<UUID, Image> TexturesResized128 = new Dictionary<UUID, Image>();
            public readonly Dictionary<UUID, Image> TexturesResized512 = new Dictionary<UUID, Image>();
            public SilverSim.Types.Color SkinColor = new SilverSim.Types.Color(1, 1, 1);

            public bool TryGetTexture(BakeType bakeType, UUID textureID, out Image img)
            {
                int targetDimension;
                Dictionary<UUID, Image> resizeCache;
                if (bakeType == BakeType.Eyes)
                {
                    resizeCache = TexturesResized128;
                    targetDimension = 128;
                }
                else
                {
                    resizeCache = TexturesResized512;
                    targetDimension = 512;
                }

                /* do not redo the hard work of rescaling images unnecessarily */
                if (resizeCache.TryGetValue(textureID, out img))
                {
                    return true;
                }

                if (Textures.TryGetValue(textureID, out img))
                {
                    if (img.Width != targetDimension || img.Height != targetDimension)
                    {
                        img = new Bitmap(img, targetDimension, targetDimension);
                        resizeCache.Add(textureID, img);
                    }
                    return true;
                }

                img = null;
                return false;
            }

            public void Dispose()
            {
                foreach (var img in Textures.Values)
                {
                    img.Dispose();
                }
                foreach (var img in TexturesResized128.Values)
                {
                    img.Dispose();
                }
                foreach (var img in TexturesResized512.Values)
                {
                    img.Dispose();
                }
            }
        }

        [Serializable]
        public class BakingErrorException : Exception
        {
            public BakingErrorException()
            {
            }

            public BakingErrorException(string message)
            : base(message)
            {
            }

            protected BakingErrorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
            {
            }

            public BakingErrorException(string message, Exception innerException)
            : base(message, innerException)
            {
            }
        }

        #region Get Current Outfit
        private static int LinkDescriptionToInt(string desc)
        {
            int res = 0;
            if(desc.StartsWith("@") && int.TryParse(desc.Substring(1), out res))
            {
                return res;
            }
            return 0;
        }

        private static byte DoubleToByte(double val, double min, double max)
        {
            if(val < min)
            {
                return 0;
            }
            if(val > max)
            {
                return 255;
            }
            return (byte)Math.Floor((val - min) * 255 / (max - min));
        }

        public sealed class AgentInfo
        {
            public readonly UUI Owner;
            public UUID CurrentOutfitFolderID;
            public readonly AssetServiceInterface AssetService;
            public readonly InventoryServiceInterface InventoryService;
            public InventoryFolderContent CurrentOutfit;

            public AgentInfo(IAgent agent)
            {
                Owner = agent.Owner;
                CurrentOutfitFolderID = agent.CurrentOutfitFolder;
                AssetService = agent.AssetService;
                InventoryService = agent.InventoryService;
            }

            public AgentInfo(UUI owner, InventoryServiceInterface inventoryService, AssetServiceInterface assetService, UUID currentOutfitFolderID)
            {
                Owner = owner;
                InventoryService = inventoryService;
                AssetService = assetService;
                CurrentOutfitFolderID = currentOutfitFolderID;
            }
        }

        public static void LoadAppearanceFromCurrentOutfit(this IAgent agent, AssetServiceInterface sceneAssetService, bool rebake = false, Action<string> logOutput = null)
        {
            if (agent.CurrentOutfitFolder == UUID.Zero)
            {
                var currentOutfitFolder = agent.InventoryService.Folder[agent.Owner.ID, AssetType.CurrentOutfitFolder];
                agent.CurrentOutfitFolder = currentOutfitFolder.ID;
                logOutput?.Invoke(string.Format("Retrieved current outfit folder for agent {0}", agent.Owner.FullName));
            }

            AgentInfo agentInfo = new AgentInfo(agent);
            agentInfo.CurrentOutfit = agent.InventoryService.Folder.Content[agent.Owner.ID, agent.CurrentOutfitFolder];
            if (agentInfo.CurrentOutfit.Version == agent.Appearance.Serial && !rebake)
            {
                logOutput?.Invoke(string.Format("No baking required for agent {0}", agent.Owner.FullName));
                return;
            }
            agent.Appearance = LoadAppearanceFromCurrentOutfit(new AgentInfo(agent), sceneAssetService, logOutput);
        }

        public static AppearanceInfo LoadAppearanceFromCurrentOutfit(AgentInfo agentInfo, AssetServiceInterface sceneAssetService, Action<string> logOutput = null)
        {
            logOutput?.Invoke(string.Format("Baking agent {0}", agentInfo.Owner.FullName));

            if (agentInfo.CurrentOutfitFolderID == UUID.Zero)
            {
                var currentOutfitFolder = agentInfo.InventoryService.Folder[agentInfo.Owner.ID, AssetType.CurrentOutfitFolder];
                agentInfo.CurrentOutfitFolderID = currentOutfitFolder.ID;
                logOutput?.Invoke(string.Format("Retrieved current outfit folder for agent {0}", agentInfo.Owner.FullName));
            }

            if (agentInfo.CurrentOutfit == null)
            {
                agentInfo.CurrentOutfit = agentInfo.InventoryService.Folder.Content[agentInfo.Owner.ID, agentInfo.CurrentOutfitFolderID];
            }

            /* the ordering of clothing layering is placed into the description of the link */

            var items = new List<InventoryItem>();
            var itemlinks = new List<UUID>();
            foreach (var item in agentInfo.CurrentOutfit.Items)
            {
                if (item.AssetType == AssetType.Link)
                {
                    items.Add(item);
                    itemlinks.Add(item.AssetID);
                }
            }
            items.Sort((item1, item2) => LinkDescriptionToInt(item1.Description).CompareTo(LinkDescriptionToInt(item2.Description)));

            var wearables = new Dictionary<WearableType, List<AgentWearables.WearableInfo>>();

            var actualItems = agentInfo.InventoryService.Item[agentInfo.Owner.ID, itemlinks];
            var actualItemsInDict = new Dictionary<UUID, InventoryItem>();
            foreach (var item in actualItems)
            {
                actualItemsInDict.Add(item.ID, item);
            }

            logOutput?.Invoke(string.Format("Processing assets for baking agent {0}", agentInfo.Owner.FullName));

            foreach (var linkItem in items)
            {
                InventoryItem actualItem;
                if (actualItemsInDict.TryGetValue(linkItem.AssetID, out actualItem) &&
                    (actualItem.AssetType == AssetType.Clothing || actualItem.AssetType == AssetType.Bodypart))
                {
                    AssetData outfitData;
                    Wearable wearableData;
                    if (agentInfo.AssetService.TryGetValue(actualItem.AssetID, out outfitData))
                    {
                        try
                        {
                            wearableData = new Wearable(outfitData);
                        }
                        catch (Exception e)
                        {
                            string info = string.Format("Asset {0} for agent {1} ({2}) failed to decode as wearable", actualItem.AssetID, agentInfo.Owner.FullName, agentInfo.Owner.ID);
                            m_BakeLog.ErrorFormat(info, e);
                            throw new BakingErrorException(info, e);
                        }

                        if (!wearables.ContainsKey(wearableData.Type))
                        {
                            wearables.Add(wearableData.Type, new List<AgentWearables.WearableInfo>());
                        }

                        var wearableInfo = new AgentWearables.WearableInfo()
                        {
                            ItemID = actualItem.ID,
                            AssetID = actualItem.AssetID
                        };
                        wearables[wearableData.Type].Add(wearableInfo);
                    }
                }
            }

            logOutput?.Invoke(string.Format("Processing baking for agent {0}", agentInfo.Owner.FullName));

            AppearanceInfo appearance = new AppearanceInfo();
            appearance.Wearables.All = wearables;
            appearance.Serial = agentInfo.CurrentOutfit.Version;
            agentInfo.BakeAppearanceFromWearablesInfo(appearance, sceneAssetService, logOutput);

            logOutput?.Invoke(string.Format("Baking agent {0} completed", agentInfo.Owner.FullName));
            return appearance;
        }
        #endregion


        public static void BakeAppearanceFromWearablesInfo(this AgentInfo agent, AppearanceInfo appearance, AssetServiceInterface sceneAssetService, Action<string> logOutput = null)
        {
            var agentOwner = agent.Owner;
            var inventoryService = agent.InventoryService;
            var assetService = agent.AssetService;
            var visualParamInputs = new Dictionary<uint, double>();

            Dictionary<WearableType, List<AgentWearables.WearableInfo>> wearables = appearance.Wearables.All;
            using (var bakeStatus = new BakeStatus())
            {
                var wearablesItemIds = new List<UUID>();
                for (int wearableIndex = 0; wearableIndex < MAX_WEARABLES_PER_TYPE; ++wearableIndex)
                {
                    for (int wearableType = 0; wearableType < (int)WearableType.NumWearables; ++wearableType)
                    {
                        List<AgentWearables.WearableInfo> wearablesList;
                        if (wearables.TryGetValue((WearableType)wearableType, out wearablesList) &&
                            wearablesList.Count > wearableIndex)
                        {
                            wearablesItemIds.Add(wearablesList[wearableIndex].ItemID);
                        }
                    }
                }

                var actualItems = inventoryService.Item[agentOwner.ID, new List<UUID>(wearablesItemIds)];
                var actualItemsInDict = new Dictionary<UUID, InventoryItem>();
                foreach (var item in actualItems)
                {
                    actualItemsInDict.Add(item.ID, item);
                }

                int numberParams = 218;

                foreach (var itemId in wearablesItemIds)
                {
                    OutfitItem outfitItem;
                    AssetData outfitData;
                    InventoryItem inventoryItem;
                    if (actualItemsInDict.TryGetValue(itemId, out inventoryItem))
                    {
                        outfitItem = new OutfitItem(inventoryItem)
                        {
                            ActualItem = inventoryItem
                        };
                        switch (inventoryItem.AssetType)
                        {
                            case AssetType.Bodypart:
                            case AssetType.Clothing:
                                if (assetService.TryGetValue(inventoryItem.AssetID, out outfitData))
                                {
                                    try
                                    {
                                        outfitItem.WearableData = new Wearable(outfitData);
                                    }
                                    catch (Exception e)
                                    {
                                        string info = string.Format("Asset {0} for agent {1} ({2}) failed to decode as wearable", inventoryItem.AssetID, agent.Owner.FullName, agent.Owner.ID);
                                        m_BakeLog.ErrorFormat(info, e);
                                        throw new BakingErrorException(info, e);
                                    }

                                    /* load visual params */
                                    foreach (var kvp in outfitItem.WearableData.Params)
                                    {
                                        visualParamInputs[kvp.Key] = kvp.Value;
                                    }

                                    if (outfitItem.WearableData.Type == WearableType.Physics)
                                    {
                                        numberParams = 251;
                                    }

                                    /* load textures beforehand and do not load unnecessarily */
                                    foreach (var textureID in outfitItem.WearableData.Textures.Values)
                                    {
                                        if (bakeStatus.Textures.ContainsKey(textureID) ||
                                            textureID == AppearanceInfo.AvatarTextureData.DefaultAvatarTextureID)
                                        {
                                            /* skip we already got that one */
                                            continue;
                                        }
                                        AssetData textureData;
                                        if (!assetService.TryGetValue(textureID, out textureData) &&
                                            !sceneAssetService.TryGetValue(textureID, out textureData))
                                        {
                                            string info = string.Format("Asset {0} referenced by {1} for agent {2} ({3}) failed to be retrieved", textureID, inventoryItem.AssetID, agent.Owner.FullName, agent.Owner.ID);
                                            m_BakeLog.ErrorFormat(info);
                                            throw new BakingErrorException(info);
                                        }

                                        if (textureData.Type != AssetType.Texture)
                                        {
                                            string info = string.Format("Asset {0} for agent {1} ({2}) is not a texture (got {3})", inventoryItem.AssetID, agent.Owner.FullName, agent.Owner.ID, textureData.Type.ToString());
                                            m_BakeLog.ErrorFormat(info);
                                            throw new BakingErrorException(info);
                                        }

                                        try
                                        {
                                            bakeStatus.Textures.Add(textureData.ID, CSJ2K.J2kImage.FromStream(textureData.InputStream));
                                        }
                                        catch (Exception e)
                                        {
                                            string info = string.Format("Asset {0} for agent {1} ({2}) failed to decode as a texture", inventoryItem.AssetID, agentOwner.FullName, agentOwner.ID);
                                            m_BakeLog.ErrorFormat(info, e);
                                            throw new BakingErrorException(info, e);
                                        }
                                    }
                                }
                                break;

                            default:
                                break;
                        }
                        bakeStatus.OutfitItems.Add(outfitItem.ActualItem.ID, outfitItem);
                    }
                }

                /* update visual params */
                var visualParams = new byte[numberParams];
                for (int p = 0; p < numberParams; ++p)
                {
                    double val;
                    var map = m_VisualParamMapping[p];
                    if (!visualParamInputs.TryGetValue((uint)map.ValueId, out val))
                    {
                        val = map.DefValue;
                    }
                    visualParams[p] = DoubleToByte(val, map.MinValue, map.MaxValue);
                }
                appearance.VisualParams = visualParams;

                appearance.CoreBakeLogic(bakeStatus, sceneAssetService);
            }
        }
    }
}