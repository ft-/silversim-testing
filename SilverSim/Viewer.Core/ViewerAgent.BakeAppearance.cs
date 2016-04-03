using log4net;
using SilverSim.Types;
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
using System.Reflection;
using System.Runtime.Serialization;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        private static readonly ILog m_BakeLog = LogManager.GetLogger("AVATAR BAKING");

        UUID m_CurrentOutfitFolder = UUID.Zero;

        enum BakeType
        {
            Head,
            UpperBody,
            LowerBody,
            Eyes,
            Skirt,
            Hair
        }

        class OutfitItem
        {
            public InventoryItem LinkItem;
            public InventoryItem ActualItem;
            public Wearable WearableData;

            public OutfitItem(InventoryItem linkItem)
            {
                LinkItem = linkItem;
            }
        }

        class TextureLayer
        {
            public UUID TextureID;
            public int TextureIndex;

            public TextureLayer(UUID textureID, int index)
            {
                TextureID = textureID;
                TextureIndex = index;
            }
        }

        class BakeStatus : IDisposable
        {
            public readonly Dictionary<UUID, OutfitItem> OutfitItems = new Dictionary<UUID, OutfitItem>();
            public readonly Dictionary<UUID, Image> Textures = new Dictionary<UUID, Image>();
            public readonly Dictionary<UUID, Image> TexturesResized128 = new Dictionary<UUID, Image>();
            public readonly Dictionary<UUID, Image> TexturesResized512 = new Dictionary<UUID, Image>();
            public UUID Layer0TextureID = UUID.Zero;

            public BakeStatus()
            {

            }

            public bool TryGetTexture(BakeType bakeType, UUID textureID, out Image img)
            {
                int targetDimension;
                Dictionary<UUID, Image> resizeCache;
                if(bakeType == BakeType.Eyes)
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
                    if(img.Width != targetDimension || img.Height != targetDimension)
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
                foreach(Image img in Textures.Values)
                {
                    img.Dispose();
                }
                foreach (Image img in TexturesResized128.Values)
                {
                    img.Dispose();
                }
                foreach (Image img in TexturesResized512.Values)
                {
                    img.Dispose();
                }
            }
        }

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

        #region Actual Baking Code
        public void BakeAppearance(bool rebake = false)
        {
            if(m_CurrentOutfitFolder == UUID.Zero)
            {
                InventoryFolder currentOutfitFolder = InventoryService.Folder[Owner.ID, AssetType.CurrentOutfitFolder];
                m_CurrentOutfitFolder = currentOutfitFolder.ID;
            }

            InventoryFolderContent currentOutfit = InventoryService.Folder.Content[Owner.ID, m_CurrentOutfitFolder];
            if(currentOutfit.Version == Appearance.Serial || rebake)
            {
                return;
            }

            using (BakeStatus bakeStatus = new BakeStatus())
            {
                foreach (InventoryItem item in currentOutfit.Items)
                {
                    if (item.AssetType == AssetType.Link)
                    {
                        bakeStatus.OutfitItems.Add(item.AssetID, new OutfitItem(item));
                    }
                }

                List<InventoryItem> actualItems = InventoryService.Item[Owner.ID, new List<UUID>(bakeStatus.OutfitItems.Keys)];
                foreach (InventoryItem actualItem in actualItems)
                {
                    OutfitItem outfitItem;
                    AssetData outfitData;
                    if (bakeStatus.OutfitItems.TryGetValue(actualItem.ID, out outfitItem))
                    {
                        outfitItem.ActualItem = actualItem;
                        switch (actualItem.AssetType)
                        {
                            case AssetType.Bodypart:
                            case AssetType.Clothing:
                                if (AssetService.TryGetValue(actualItem.AssetID, out outfitData))
                                {
                                    try
                                    {
                                        outfitItem.WearableData = new Wearable(outfitData);
                                    }
                                    catch (Exception e)
                                    {
                                        string info = string.Format("Asset {0} for agent {1} ({2}) failed to decode as wearable", actualItem.AssetID, Owner.FullName, Owner.ID);
                                        m_BakeLog.ErrorFormat(info, e);
                                        throw new BakingErrorException(info, e);
                                    }
                                    foreach (UUID textureID in outfitItem.WearableData.Textures.Values)
                                    {
                                        if (bakeStatus.Textures.ContainsKey(textureID))
                                        {
                                            /* skip we already got that one */
                                            continue;
                                        }
                                        AssetData textureData;
                                        if (!AssetService.TryGetValue(textureID, out textureData))
                                        {
                                            string info = string.Format("Asset {0} for agent {1} ({2}) failed to be retrieved", actualItem.AssetID, Owner.FullName, Owner.ID);
                                            m_BakeLog.ErrorFormat(info);
                                            throw new BakingErrorException(info);
                                        }

                                        if (textureData.Type != AssetType.Texture)
                                        {
                                            string info = string.Format("Asset {0} for agent {1} ({2}) is not a texture (got {3})", actualItem.AssetID, Owner.FullName, Owner.ID, textureData.Type.ToString());
                                            m_BakeLog.ErrorFormat(info);
                                            throw new BakingErrorException(info);
                                        }

                                        try
                                        {
                                            bakeStatus.Textures.Add(textureData.ID, CSJ2K.J2kImage.FromStream(textureData.InputStream));
                                        }
                                        catch (Exception e)
                                        {
                                            string info = string.Format("Asset {0} for agent {1} ({2}) failed to decode as a texture", actualItem.AssetID, Owner.FullName, Owner.ID);
                                            m_BakeLog.ErrorFormat(info, e);
                                            throw new BakingErrorException(info, e);
                                        }
                                    }
                                }
                                break;

                            default:
                                break;
                        }
                    }

                    CoreBakeLogic(bakeStatus);
                }
            }
        }

        void AddAlpha(Bitmap bmp, Image inp)
        {
            Bitmap bmpin = null;
            try
            {
                if (inp.Width != bmp.Width || inp.Height != bmp.Height)
                {
                    bmpin = new Bitmap(inp, bmp.Size);
                }
                else
                {
                    bmpin = new Bitmap(inp);
                }

                int x;
                int y;

                for (y = 0; y < bmp.Height; ++y)
                {
                    for (x = 0; x < bmp.Width; ++x)
                    {
                        System.Drawing.Color dst = bmp.GetPixel(x, y);
                        System.Drawing.Color src = bmpin.GetPixel(x, y);
                        bmp.SetPixel(x, y, System.Drawing.Color.FromArgb(
                            dst.A > src.A ? src.A : dst.A,
                            dst.R,
                            dst.G,
                            dst.B));
                    }
                }
            }
            finally
            {
                if(null != bmpin)
                {
                    bmpin.Dispose();
                }
            }
        }

        void MultiplyLayerFromAlpha(Bitmap bmp, Image inp)
        {
            Bitmap bmpin = null;
            try
            {
                if (inp.Width != bmp.Width || inp.Height != bmp.Height)
                {
                    bmpin = new Bitmap(inp, bmp.Size);
                }
                else
                {
                    bmpin = new Bitmap(inp);
                }

                int x;
                int y;

                for (y = 0; y < bmp.Height; ++y)
                {
                    for (x = 0; x < bmp.Width; ++x)
                    {
                        System.Drawing.Color dst = bmp.GetPixel(x, y);
                        System.Drawing.Color src = bmpin.GetPixel(x, y);
                        bmp.SetPixel(x, y, System.Drawing.Color.FromArgb(
                            dst.A,
                            (byte)((dst.R * src.A) / 255),
                            (byte)((dst.G * src.A) / 255),
                            (byte)((dst.B * src.A) / 255)));
                    }
                }
            }
            finally
            {
                if (null != bmpin)
                {
                    bmpin.Dispose();
                }
            }
        }

        void ApplyTint(Bitmap bmp, System.Drawing.Color col)
        {
            int x;
            int y;
            for(y = 0; y < bmp.Height; ++y)
            {
                for (x = 0; x < bmp.Width; ++x)
                {
                    System.Drawing.Color inp = bmp.GetPixel(x, y);
                    bmp.SetPixel(x, y, System.Drawing.Color.FromArgb(
                        inp.A,
                        (byte)((inp.R * col.R) / 255),
                        (byte)((inp.G * col.G) / 255),
                        (byte)((inp.B * col.G) / 255)));
                }
            }
        }

        AssetData BakeTexture(BakeStatus status, BakeType bake)
        {
            int bakeDimensions = (bake == BakeType.Eyes) ? 128 : 512;
            Image srcimg;
            AssetData data = new AssetData();
            using (Bitmap bitmap = new Bitmap(bakeDimensions, bakeDimensions, PixelFormat.Format32bppArgb))
            {
                using (Graphics gfx = Graphics.FromImage(bitmap))
                {
                    if (bake == BakeType.Eyes)
                    {
                        /* eyes have white base texture */
                        using (SolidBrush brush = new SolidBrush(System.Drawing.Color.White))
                        {
                            gfx.FillRectangle(brush, new Rectangle(0, 0, 128, 128));
                        }
                    }
                    else if(status.Layer0TextureID != UUID.Zero &&
                        status.TryGetTexture(bake, status.Layer0TextureID, out srcimg))
                    {
                        /* all others are inited from layer 0 */
                        gfx.DrawImage(srcimg, 0, 0, 512, 512);
                    }
                    else
                    {
                        switch(bake)
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
                }

                foreach(OutfitItem item in status.OutfitItems.Values)
                {
                    if(null != item.WearableData)
                    {

                    }
                }
            }

            return data;
        }

        void CoreBakeLogic(BakeStatus bakeStatus)
        {
            Bitmap bakeHead = null;
            Bitmap bakeUpperBody = null;
            Bitmap bakeLowerBody = null;
            Bitmap bakeEyes = null;
            Bitmap bakeSkirt = null;
            Bitmap bakeHair = null;

            try
            {
                bakeHead = new Bitmap(512, 512, PixelFormat.Format32bppArgb);
                bakeUpperBody = new Bitmap(512, 512, PixelFormat.Format32bppArgb);
                bakeLowerBody = new Bitmap(512, 512, PixelFormat.Format32bppArgb);
                bakeEyes = new Bitmap(128, 128, PixelFormat.Format32bppArgb);
                bakeHair = new Bitmap(512, 512, PixelFormat.Format32bppArgb);


            }
            finally
            {
                if(null != bakeHead)
                {
                    bakeHead.Dispose();
                }
                if(null != bakeUpperBody)
                {
                    bakeUpperBody.Dispose();
                }
                if(null != bakeLowerBody)
                {
                    bakeLowerBody.Dispose();
                }
                if(null != bakeEyes)
                {
                    bakeEyes.Dispose();
                }
                if(null != bakeSkirt)
                {
                    bakeSkirt.Dispose();
                }
                if(null != bakeHair)
                {
                    bakeHair.Dispose();
                }
            }
        }

        #endregion

        #region Base Bake textures
        static class BaseBakes
        {
            public static readonly Image HeadAlpha;
            public static readonly Image HeadColor;
            public static readonly Image HeadHair;
            public static readonly Image HeadSkinGrain;
            public static readonly Image LowerBodyColor;
            public static readonly Image UpperBodyColor;

            static BaseBakes()
            {
                HeadAlpha = LoadResourceImage("head_alpha.tga.gz");
                HeadColor = LoadResourceImage("head_color.tga.gz");
                HeadHair = LoadResourceImage("head_hair.tga.gz");
                HeadSkinGrain = LoadResourceImage("head_skingrain.tga.gz");
                LowerBodyColor = LoadResourceImage("lowerbody_color.tga.gz");
                UpperBodyColor = LoadResourceImage("upperbody_color.tga.gz");
            }

            static Image LoadResourceImage(string name)
            {
                Assembly assembly = typeof(BaseBakes).Assembly;
                using (Stream resource = assembly.GetManifestResourceStream(assembly.GetName().Name + ".Resources." + name))
                {
                    using (GZipStream gz = new GZipStream(resource, CompressionMode.Decompress))
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            byte[] buf = new byte[10240];
                            int bytesRead;
                            for (bytesRead = gz.Read(buf, 0, buf.Length);
                                bytesRead > 0;
                                bytesRead = gz.Read(buf, 0, buf.Length))
                            {
                                ms.Write(buf, 0, bytesRead);
                            }
                            ms.Seek(0, SeekOrigin.Begin);
                            return Paloma.TargaImage.LoadTargaImage(ms);
                        }
                    }
                }
            }
        }
        #endregion
    }
}
