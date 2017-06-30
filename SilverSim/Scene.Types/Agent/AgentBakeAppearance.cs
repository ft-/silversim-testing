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
using OpenJp2.Net;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace SilverSim.Scene.Types.Agent
{
    public static partial class AgentBakeAppearance
    {
        private static readonly ILog m_BakeLog = LogManager.GetLogger("AVATAR BAKING");

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
            public readonly Dictionary<UUID, BakeImage> Textures = new Dictionary<UUID, BakeImage>();
            public readonly Dictionary<UUID, BakeImage> TexturesResized128 = new Dictionary<UUID, BakeImage>();
            public readonly Dictionary<UUID, BakeImage> TexturesResized512 = new Dictionary<UUID, BakeImage>();
            public UUID Layer0TextureID = UUID.Zero;

            public bool TryGetTexture(BakeType bakeType, UUID textureID, out BakeImage img)
            {
                int targetDimension;
                Dictionary<UUID, BakeImage> resizeCache;
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
                        img = new BakeImage(img, targetDimension, targetDimension);
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

        public static void LoadAppearanceFromCurrentOutfit(this IAgent agent, AssetServiceInterface sceneAssetService, bool rebake = false, Action<string> logOutput = null)
        {
            var agentOwner = agent.Owner;
            var inventoryService = agent.InventoryService;
            var assetService = agent.AssetService;

            logOutput?.Invoke(string.Format("Baking agent {0}", agent.Owner.FullName));
            if (agent.CurrentOutfitFolder == UUID.Zero)
            {
                var currentOutfitFolder = inventoryService.Folder[agentOwner.ID, AssetType.CurrentOutfitFolder];
                agent.CurrentOutfitFolder = currentOutfitFolder.ID;
                logOutput?.Invoke(string.Format("Retrieved current outfit folder for agent {0}", agent.Owner.FullName));
            }

            var currentOutfit = inventoryService.Folder.Content[agentOwner.ID, agent.CurrentOutfitFolder];
            if (currentOutfit.Version == agent.Appearance.Serial && !rebake)
            {
                logOutput?.Invoke(string.Format("No baking required for agent {0}", agent.Owner.FullName));
                return;
            }

            /* the ordering of clothing layering is placed into the description of the link */

            var items = new List<InventoryItem>();
            var itemlinks = new List<UUID>();
            foreach (var item in currentOutfit.Items)
            {
                if (item.AssetType == AssetType.Link)
                {
                    items.Add(item);
                    itemlinks.Add(item.AssetID);
                }
            }
            items.Sort((item1, item2) => LinkDescriptionToInt(item1.Description).CompareTo(LinkDescriptionToInt(item2.Description)));

            var wearables = new Dictionary<WearableType, List<AgentWearables.WearableInfo>>();

            var actualItems = inventoryService.Item[agentOwner.ID, itemlinks];
            var actualItemsInDict = new Dictionary<UUID, InventoryItem>();
            foreach (var item in actualItems)
            {
                actualItemsInDict.Add(item.ID, item);
            }

            logOutput?.Invoke(string.Format("Processing assets for baking agent {0}", agent.Owner.FullName));

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
                            string info = string.Format("Asset {0} for agent {1} ({2}) failed to decode as wearable", actualItem.AssetID, agent.Owner.FullName, agent.Owner.ID);
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

            agent.Wearables.All = wearables;
            agent.Appearance.Serial = currentOutfit.Version;

            logOutput?.Invoke(string.Format("Processing baking for agent {0}", agent.Owner.FullName));

            agent.BakeAppearanceFromWearablesInfo(sceneAssetService, logOutput);

            logOutput?.Invoke(string.Format("Baking agent {0} completed", agent.Owner.FullName));
        }
        #endregion

        #region Actual Baking Code
        private const int MAX_WEARABLES_PER_TYPE = 5;

        public class BakeImage : IDisposable
        {
            private Bitmap m_Bitmap;
            private byte[] m_ArgbImage;

            public BakeImage(Stream s)
            {
                m_Bitmap = new Bitmap(s);
            }

            public BakeImage(int width, int height)
            {
                m_Bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            }

            public BakeImage(BakeImage img, int newWidth, int newHeight)
            {
                m_Bitmap = new Bitmap(img.m_Bitmap, newWidth, newHeight);
            }

            public BakeImage(Image i)
            {
                m_Bitmap = new Bitmap(i);
            }

            public int Width => m_Bitmap.Width;
            public int Height => m_Bitmap.Height;

            public static implicit operator Bitmap(BakeImage img)
            {
                return img.m_Bitmap;
            }

            public byte[] ArgbImage
            {
                get
                {
                    Bitmap bmp = m_Bitmap;
                    if(m_ArgbImage == null && (bmp.PixelFormat == PixelFormat.Format32bppArgb || bmp.PixelFormat == PixelFormat.Format24bppRgb))
                    {
                        BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                            ImageLockMode.ReadOnly,
                            bmp.PixelFormat);
                        if (bmp.PixelFormat == PixelFormat.Format32bppArgb)
                        {
                            int bytes = Math.Abs(bmpData.Stride) * bmpData.Height;
                            m_ArgbImage = new byte[bytes];
                            Marshal.Copy(bmpData.Scan0, m_ArgbImage, 0, bytes);
                        }
                        else
                        {
                            int actbytes = Math.Abs(bmpData.Stride) * bmp.Height;
                            int finalbytes = bmpData.Width * bmpData.Height * 4;
                            m_ArgbImage = new byte[finalbytes];
                            Marshal.Copy(bmpData.Scan0, m_ArgbImage, 0, actbytes);

                            /* make it argb format */
                            for(int i = bmpData.Width * bmpData.Height; i-- != 0; )
                            {
                                int targetidx = i * 4;
                                int sourceidx = i * 3;
                                m_ArgbImage[targetidx + 3] = 255;
                                m_ArgbImage[targetidx + 2] = m_ArgbImage[sourceidx + 2];
                                m_ArgbImage[targetidx + 1] = m_ArgbImage[sourceidx + 1];
                                m_ArgbImage[targetidx + 0] = m_ArgbImage[sourceidx + 0];
                            }
                        }
                        bmp.UnlockBits(bmpData);
                    }
                    return m_ArgbImage;
                }
            }

            public void Update()
            {
                if (m_Bitmap.PixelFormat == PixelFormat.Format32bppArgb)
                {
                    BitmapData bmpData = m_Bitmap.LockBits(new Rectangle(0, 0, m_Bitmap.Width, m_Bitmap.Height),
                        ImageLockMode.ReadWrite,
                        m_Bitmap.PixelFormat);
                    Marshal.Copy(m_ArgbImage, 0, bmpData.Scan0, m_ArgbImage.Length);
                    m_Bitmap.UnlockBits(bmpData);
                }
                else
                {
                    Bitmap bmp = m_Bitmap;

                    /* make a new image with Argb format */
                    m_Bitmap = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format32bppArgb);

                    BitmapData bmpData = m_Bitmap.LockBits(new Rectangle(0, 0, m_Bitmap.Width, m_Bitmap.Height),
                        ImageLockMode.ReadWrite,
                        bmp.PixelFormat);
                    Marshal.Copy(m_ArgbImage, 0, bmpData.Scan0, m_ArgbImage.Length);
                    m_Bitmap.UnlockBits(bmpData);
                    bmp.Dispose();
                }
            }

            public void Dispose()
            {
                m_Bitmap?.Dispose();
            }
        }

        public static void BakeAppearanceFromWearablesInfo(this IAgent agent, AssetServiceInterface sceneAssetService, Action<string> logOutput = null)
        {
            var agentOwner = agent.Owner;
            var inventoryService = agent.InventoryService;
            var assetService = agent.AssetService;
            var visualParamInputs = new Dictionary<uint, double>();

            Dictionary<WearableType, List<AgentWearables.WearableInfo>> wearables = agent.Wearables.All;
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
                                    foreach(var kvp in outfitItem.WearableData.Params)
                                    {
                                        visualParamInputs[kvp.Key] = kvp.Value;
                                    }

                                    if(outfitItem.WearableData.Type == WearableType.Physics)
                                    {
                                        numberParams = 251;
                                    }

                                    /* load textures beforehand and do not load unnecessarily */
                                    foreach (var textureID in outfitItem.WearableData.Textures.Values)
                                    {
                                        if (bakeStatus.Textures.ContainsKey(textureID))
                                        {
                                            /* skip we already got that one */
                                            continue;
                                        }
                                        AssetData textureData;
                                        if (!assetService.TryGetValue(textureID, out textureData) &&
                                            !sceneAssetService.TryGetValue(textureID, out textureData))
                                        {
                                            string info = string.Format("Asset {0} referenced by {1} for agent {2} ({3}) failed to be retrieved", textureID,inventoryItem.AssetID, agent.Owner.FullName, agent.Owner.ID);
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
                                            using (Image img = CSJ2K.J2kImage.FromStream(textureData.InputStream))
                                            {
                                                bakeStatus.Textures.Add(textureData.ID, new BakeImage(img));
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            string info = string.Format("Asset {0} for agent {1} ({2}) failed to decode as a texture", inventoryItem.AssetID, agentOwner.FullName, agentOwner.ID);
                                            m_BakeLog.ErrorFormat(info, e);
                                            throw new BakingErrorException(info, e);
                                        }
                                    }

                                    if (bakeStatus.Layer0TextureID == UUID.Zero &&
                                        !outfitItem.WearableData.Textures.TryGetValue(AvatarTextureIndex.HeadBodypaint, out bakeStatus.Layer0TextureID))
                                    {
                                        bakeStatus.Layer0TextureID = UUID.Zero;
                                    }
                                }
                                break;

                            default:
                                break;
                        }
                    }
                }

                /* update visual params */
                var visualParams = new byte[numberParams];
                for(int p = 0; p < numberParams; ++p)
                {
                    double val;
                    var map = m_VisualParamMapping[p];
                    if (!visualParamInputs.TryGetValue((uint)map.ValueId, out val))
                    {
                        val = map.DefValue;
                    }
                    visualParams[p] = DoubleToByte(val, map.MinValue, map.MaxValue);
                }
                agent.VisualParams = visualParams;

                agent.CoreBakeLogic(bakeStatus, sceneAssetService);
            }
        }

        private static void AddAlpha(BakeImage bmp, BakeImage inp)
        {
            byte[] target = bmp.ArgbImage;
            byte[] source = inp.ArgbImage;
            BakeImage bakeIntermediate = null;
            if (bmp.Width != inp.Width || bmp.Height != inp.Height)
            {
                bakeIntermediate = new BakeImage(inp, bmp.Width, bmp.Height);
                inp = bakeIntermediate;
            }

            try
            {
                for (int i = bmp.Width * bmp.Height * 4; i-- != 0;)
                {
                    target[i] = Math.Min(target[i], source[i]);

                    /* skip RGB */
                    i += 3;
                }

                bmp.Update();
            }
            finally
            {
                bakeIntermediate?.Dispose();
            }
        }

        private static void MultiplyLayerFromAlpha(BakeImage bmp, BakeImage inp)
        {
            byte[] target = bmp.ArgbImage;
            byte[] source = inp.ArgbImage;
            BakeImage bakeIntermediate = null;
            if(bmp.Width != inp.Width || bmp.Height != inp.Height)
            {
                bakeIntermediate = new BakeImage(inp, bmp.Width, bmp.Height);
                inp = bakeIntermediate;
            }
            try
            {
                for (int i = bmp.Width * bmp.Height * 4; i-- != 0;)
                {
                    /* skip A */
                    --i;
                    target[i] = (byte)(target[i] * source[i] / 255);
                    --i;
                    target[i] = (byte)(target[i] * source[i] / 255);
                    --i;
                    target[i] = (byte)(target[i] * source[i] / 255);
                }

                bmp.Update();
            }
            finally
            {
                bakeIntermediate?.Dispose();
            }
        }

        private static System.Drawing.Color GetTint(Wearable w, BakeType bType)
        {
            var wColor = new SilverSim.Types.Color(1, 1, 1);
            double val;
            switch (w.Type)
            {
                case WearableType.Tattoo:
                    if (w.Params.TryGetValue(1071, out val))
                    {
                        wColor.R = val.Clamp(0, 1);
                    }
                    if (w.Params.TryGetValue(1072, out val))
                    {
                        wColor.G = val.Clamp(0, 1);
                    }
                    if (w.Params.TryGetValue(1073, out val))
                    {
                        wColor.B = val.Clamp(0, 1);
                    }
                    switch (bType)
                    {
                        case BakeType.Head:
                            if (w.Params.TryGetValue(1062, out val))
                            {
                                wColor.R = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(1063, out val))
                            {
                                wColor.G = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(1064, out val))
                            {
                                wColor.B = val.Clamp(0, 1);
                            }
                            break;
                        case BakeType.UpperBody:
                            if (w.Params.TryGetValue(1065, out val))
                            {
                                wColor.R = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(1066, out val))
                            {
                                wColor.G = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(1067, out val))
                            {
                                wColor.B = val.Clamp(0, 1);
                            }
                            break;
                        case BakeType.LowerBody:
                            if (w.Params.TryGetValue(1068, out val))
                            {
                                wColor.R = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(1069, out val))
                            {
                                wColor.G = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(1070, out val))
                            {
                                wColor.B = val.Clamp(0, 1);
                            }
                            break;

                        default:
                            break;
                    }
                    break;

                case WearableType.Jacket:
                    if (w.Params.TryGetValue(834, out val))
                    {
                        wColor.R = val.Clamp(0, 1);
                    }
                    if (w.Params.TryGetValue(835, out val))
                    {
                        wColor.G = val.Clamp(0, 1);
                    }
                    if (w.Params.TryGetValue(836, out val))
                    {
                        wColor.B = val.Clamp(0, 1);
                    }
                    switch (bType)
                    {
                        case BakeType.UpperBody:
                            if (w.Params.TryGetValue(831, out val))
                            {
                                wColor.R = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(832, out val))
                            {
                                wColor.G = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(833, out val))
                            {
                                wColor.B = val.Clamp(0, 1);
                            }
                            break;
                        case BakeType.LowerBody:
                            if (w.Params.TryGetValue(809, out val))
                            {
                                wColor.R = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(810, out val))
                            {
                                wColor.G = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(811, out val))
                            {
                                wColor.B = val.Clamp(0, 1);
                            }
                            break;

                        default:
                            break;
                    }
                    break;

                default:
                    wColor = w.GetTint();
                    break;
            }

            return System.Drawing.Color.FromArgb(wColor.R_AsByte, wColor.G_AsByte, wColor.B_AsByte);
        }

        private static void ApplyTint(BakeImage bmp, SilverSim.Types.Color col)
        {
            int x;
            int y;
            byte[] argb = bmp.ArgbImage;
            for(int i = bmp.Width * bmp.Height * 4; i-- != 0;)
            {
                /* skip A */
                --i;
                argb[i] = (byte)(argb[i] * col.R).Clamp(0, 255);
                --i;
                argb[i] = (byte)(argb[i] * col.G).Clamp(0, 255);
                --i;
                argb[i] = (byte)(argb[i] * col.B).Clamp(0, 255);
            }
            bmp.Update();
        }

        private static AssetData BakeTexture(BakeStatus status, BakeType bake)
        {
            int bakeDimensions = (bake == BakeType.Eyes) ? 128 : 512;
            BakeImage srcimg;
            var data = new AssetData()
            {
                ID = UUID.RandomFixedFirst(0xFFFFFFFF),
                Type = AssetType.Texture,
                Local = true,
                Temporary = true,
                Flags = AssetFlags.Collectable | AssetFlags.Rewritable
            };
            AvatarTextureIndex[] bakeProcessTable;
            switch (bake)
            {
                case BakeType.Head:
                    bakeProcessTable = IndexesForBakeHead;
                    data.Name = "Baked Head Texture";
                    break;

                case BakeType.Eyes:
                    bakeProcessTable = IndexesForBakeEyes;
                    data.Name = "Baked Eyes Texture";
                    break;

                case BakeType.Hair:
                    bakeProcessTable = IndexesForBakeHair;
                    data.Name = "Baked Hair Texture";
                    break;

                case BakeType.LowerBody:
                    bakeProcessTable = IndexesForBakeLowerBody;
                    data.Name = "Baked Lower Body Texture";
                    break;

                case BakeType.UpperBody:
                    bakeProcessTable = IndexesForBakeUpperBody;
                    data.Name = "Baked Upper Body Texture";
                    break;

                case BakeType.Skirt:
                    bakeProcessTable = IndexesForBakeSkirt;
                    data.Name = "Baked Skirt Texture";
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(bake));
            }

            using (var bitmap = new BakeImage(bakeDimensions, bakeDimensions))
            {
                using (var gfx = Graphics.FromImage(bitmap))
                {
                    if (bake == BakeType.Eyes)
                    {
                        /* eyes have white base texture */
                        using (var brush = new SolidBrush(System.Drawing.Color.White))
                        {
                            gfx.FillRectangle(brush, new Rectangle(0, 0, 128, 128));
                        }
                    }
                    else if (status.Layer0TextureID != UUID.Zero &&
                        status.TryGetTexture(bake, status.Layer0TextureID, out srcimg))
                    {
                        /* all others are inited from layer 0 */
                        gfx.DrawImage(srcimg, 0, 0, 512, 512);
                    }
                    else
                    {
                        switch (bake)
                        {
                            case BakeType.Head:
                                gfx.DrawImage(BaseBakes.HeadColor, 0, 0, 512, 512);
                                AddAlpha(bitmap, BaseBakes.HeadAlpha);
                                MultiplyLayerFromAlpha(bitmap, BaseBakes.HeadSkinGrain);
                                break;

                            case BakeType.UpperBody:
                                gfx.DrawImage(BaseBakes.UpperBodyColor, 0, 0, 512, 512);
                                break;

                            case BakeType.LowerBody:
                                gfx.DrawImage(BaseBakes.LowerBodyColor, 0, 0, 512, 512);
                                break;

                            default:
                                break;
                        }
                    }

                    /* alpha blending is enabled by changing the compositing mode of the graphics object */
                    gfx.CompositingMode = CompositingMode.SourceOver;

                    foreach (var texIndex in bakeProcessTable)
                    {
                        foreach (var item in status.OutfitItems.Values)
                        {
                            UUID texture;
                            BakeImage img;
                            if ((item.WearableData != null && item.WearableData.Textures.TryGetValue(texIndex, out texture)) &&
                                status.TryGetTexture(bake, texture, out img))
                            {
                                switch (texIndex)
                                {
                                    case AvatarTextureIndex.HeadBodypaint:
                                    case AvatarTextureIndex.UpperBodypaint:
                                    case AvatarTextureIndex.LowerBodypaint:
                                        /* no tinting here */
                                        gfx.DrawImage(img, 0, 0, bakeDimensions, bakeDimensions);
                                        AddAlpha(bitmap, img);
                                        break;

                                    default:
                                        using (BakeImage img2 = new BakeImage(img))
                                        {
                                            gfx.DrawImage(img, 0, 0, bakeDimensions, bakeDimensions);
                                            ApplyTint(img2, item.WearableData.GetTint());
                                            AddAlpha(bitmap, img2);
                                        }
                                        break;
                                }

                            }
                        }
                    }
                }

                data.Data = J2cEncoder.Encode(bitmap, true);
            }

            return data;
        }

        private static readonly AvatarTextureIndex[] IndexesForBakeHead = new AvatarTextureIndex[]
        {
            AvatarTextureIndex.HeadAlpha,
            AvatarTextureIndex.HeadBodypaint,
            AvatarTextureIndex.HeadTattoo
        };

        private static readonly AvatarTextureIndex[] IndexesForBakeUpperBody = new AvatarTextureIndex[]
        {
            AvatarTextureIndex.UpperBodypaint,
            AvatarTextureIndex.UpperGloves,
            AvatarTextureIndex.UpperUndershirt,
            AvatarTextureIndex.UpperShirt,
            AvatarTextureIndex.UpperJacket,
            AvatarTextureIndex.UpperAlpha
        };

        private static readonly AvatarTextureIndex[] IndexesForBakeLowerBody = new AvatarTextureIndex[]
        {
            AvatarTextureIndex.LowerBodypaint,
            AvatarTextureIndex.LowerUnderpants,
            AvatarTextureIndex.LowerSocks,
            AvatarTextureIndex.LowerShoes,
            AvatarTextureIndex.LowerPants,
            AvatarTextureIndex.LowerJacket,
            AvatarTextureIndex.LowerAlpha
        };

        private static readonly AvatarTextureIndex[] IndexesForBakeEyes = new AvatarTextureIndex[]
        {
            AvatarTextureIndex.EyesIris,
            AvatarTextureIndex.EyesAlpha
        };

        private static readonly AvatarTextureIndex[] IndexesForBakeHair = new AvatarTextureIndex[]
        {
            AvatarTextureIndex.Hair,
            AvatarTextureIndex.HairAlpha
        };

        private static readonly AvatarTextureIndex[] IndexesForBakeSkirt = new AvatarTextureIndex[]
        {
            AvatarTextureIndex.Skirt
        };

        public static object Owner { get; }

        private static void CoreBakeLogic(this AppearanceInfo.AvatarTextureData agentTextures, BakeStatus bakeStatus, AssetServiceInterface sceneAssetService)
        {
            for(int idx = 0; idx < AppearanceInfo.AvatarTextureData.TextureCount; ++idx)
            {
                agentTextures[idx] = AppearanceInfo.AvatarTextureData.DefaultAvatarTextureID;
            }
            foreach (OutfitItem item in bakeStatus.OutfitItems.Values)
            {
                foreach(KeyValuePair<AvatarTextureIndex, UUID> tex in item.WearableData.Textures)
                {
                    agentTextures[(int)tex.Key] = tex.Value;
                }
            }


            var bakeHead = BakeTexture(bakeStatus, BakeType.Head);
            var bakeUpperBody = BakeTexture(bakeStatus, BakeType.UpperBody);
            var bakeLowerBody = BakeTexture(bakeStatus, BakeType.LowerBody);
            var bakeEyes = BakeTexture(bakeStatus, BakeType.Eyes);
            var bakeHair = BakeTexture(bakeStatus, BakeType.Hair);
            AssetData bakeSkirt = null;

            var haveSkirt = false;
            foreach (var item in bakeStatus.OutfitItems.Values)
            {
                if (item.WearableData?.Type == WearableType.Skirt)
                {
                    haveSkirt = true;
                    break;
                }
            }

            if (haveSkirt)
            {
                bakeSkirt = BakeTexture(bakeStatus, BakeType.Skirt);
            }

            sceneAssetService.Store(bakeEyes);
            sceneAssetService.Store(bakeHead);
            sceneAssetService.Store(bakeUpperBody);
            sceneAssetService.Store(bakeLowerBody);
            sceneAssetService.Store(bakeHair);
            if (bakeSkirt != null)
            {
                sceneAssetService.Store(bakeSkirt);
            }

            agentTextures[(int)AvatarTextureIndex.EyesBaked] = bakeEyes.ID;
            agentTextures[(int)AvatarTextureIndex.HeadBaked] = bakeHead.ID;
            agentTextures[(int)AvatarTextureIndex.UpperBaked] = bakeUpperBody.ID;
            agentTextures[(int)AvatarTextureIndex.LowerBaked] = bakeLowerBody.ID;
            agentTextures[(int)AvatarTextureIndex.HairBaked] = bakeHair.ID;
            agentTextures[(int)AvatarTextureIndex.Skirt] = bakeSkirt != null ? bakeSkirt.ID : UUID.Zero;
        }

        private static void CoreBakeLogic(this IAgent agent, BakeStatus bakeStatus, AssetServiceInterface sceneAssetService)
        {
            CoreBakeLogic(agent.Textures, bakeStatus, sceneAssetService);
        }

        #endregion

        #region Base Bake textures
        private static class BaseBakes
        {
            public static readonly BakeImage HeadAlpha;
            public static readonly BakeImage HeadColor;
            public static readonly BakeImage HeadHair;
            public static readonly BakeImage HeadSkinGrain;
            public static readonly BakeImage LowerBodyColor;
            public static readonly BakeImage UpperBodyColor;

            static BaseBakes()
            {
                HeadAlpha = LoadResourceImage("head_alpha.tga.gz");
                HeadColor = LoadResourceImage("head_color.tga.gz");
                HeadHair = LoadResourceImage("head_hair.tga.gz");
                HeadSkinGrain = LoadResourceImage("head_skingrain.tga.gz");
                LowerBodyColor = LoadResourceImage("lowerbody_color.tga.gz");
                UpperBodyColor = LoadResourceImage("upperbody_color.tga.gz");
            }

            private static BakeImage LoadResourceImage(string name)
            {
                var assembly = typeof(BaseBakes).Assembly;
                using (var resource = assembly.GetManifestResourceStream(assembly.GetName().Name + ".Resources." + name))
                {
                    using (var gz = new GZipStream(resource, CompressionMode.Decompress))
                    {
                        using (var ms = new MemoryStream())
                        {
                            var buf = new byte[10240];
                            int bytesRead;
                            for (bytesRead = gz.Read(buf, 0, buf.Length);
                                bytesRead > 0;
                                bytesRead = gz.Read(buf, 0, buf.Length))
                            {
                                ms.Write(buf, 0, bytesRead);
                            }
                            ms.Seek(0, SeekOrigin.Begin);
                            return new BakeImage(Paloma.TargaImage.LoadTargaImage(ms));
                        }
                    }
                }
            }
        }
        #endregion
    }
}