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

using OpenJp2.Net;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types.Agent;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using UUID = SilverSim.Types.UUID;

namespace SilverSim.Scene.Agent.Bakery
{
    public class BakeProcessor : IBakeTextureInputCache, IDisposable
    {
        private readonly List<UUID> m_ProcessedAssetIDs = new List<UUID>();
        private readonly Dictionary<UUID, Image> m_Textures = new Dictionary<UUID, Image>();
        private readonly Dictionary<UUID, Image> m_TexturesResized128 = new Dictionary<UUID, Image>();
        private readonly Dictionary<UUID, Image> m_TexturesResized512 = new Dictionary<UUID, Image>();
        private readonly Dictionary<UUID, byte[]> m_Bumps = new Dictionary<UUID, byte[]>();
        private readonly Dictionary<UUID, byte[]> m_BumpsResized128 = new Dictionary<UUID, byte[]>();
        private readonly Dictionary<UUID, byte[]> m_BumpsResized512 = new Dictionary<UUID, byte[]>();
        private AssetServiceInterface m_AssetService;

        private class Targets
        {
            public Dictionary<BakeTarget, Graphics> Graphics = new Dictionary<BakeTarget, Graphics>();
            public Dictionary<BakeTarget, Bitmap> Images = new Dictionary<BakeTarget, Bitmap>();
            public Dictionary<BakeTarget, Rectangle> Rectangles = new Dictionary<BakeTarget, Rectangle>();
            public Dictionary<BakeTarget, byte[]> Bumps = new Dictionary<BakeTarget, byte[]>();

            public Targets()
            {
                Bumps.Add(BakeTarget.Head, new byte[512 * 512]);
                Bumps.Add(BakeTarget.UpperBody, new byte[512 * 512]);
                Bumps.Add(BakeTarget.LowerBody, new byte[512 * 512]);
                Rectangles.Add(BakeTarget.Eyes, new Rectangle(0, 0, 128, 128));
                Rectangles.Add(BakeTarget.Hair, new Rectangle(0, 0, 512, 512));
                Rectangles.Add(BakeTarget.Head, new Rectangle(0, 0, 512, 512));
                Rectangles.Add(BakeTarget.LowerBody, new Rectangle(0, 0, 512, 512));
                Rectangles.Add(BakeTarget.Skirt, new Rectangle(0, 0, 512, 512));
                Rectangles.Add(BakeTarget.UpperBody, new Rectangle(0, 0, 512, 512));
            }
        }

        private void TryLoadTexture(UUID textureID)
        {
            AssetData data;
            if(m_ProcessedAssetIDs.Contains(textureID))
            {
                return;
            }
            m_ProcessedAssetIDs.Add(textureID);
            if (m_AssetService.TryGetValue(textureID, out data))
            {
                byte[] bump = null;
                Image img;
                try
                {
                    img = J2cDecoder.DecodeWithDump(data.Data, out bump);
                }
                catch
                {
                    img = new Bitmap(BaseBakes.UndefinedTexture);
                }
                m_Textures.Add(textureID, img);
                if (bump != null)
                {
                    m_Bumps.Add(textureID, bump);
                }
            }
        }

        public bool TryGetTexture(UUID textureID, BakeTarget bakeType, out Image img)
        {
            if(textureID == UUID.Zero)
            {
                img = null;
                return false;
            }

            int targetDimension;
            Dictionary<UUID, Image> resizeCache;
            if (bakeType == BakeTarget.Eyes)
            {
                resizeCache = m_TexturesResized128;
                targetDimension = 128;
            }
            else
            {
                resizeCache = m_TexturesResized512;
                targetDimension = 512;
            }

            /* do not redo the hard work of rescaling images unnecessarily */
            if (resizeCache.TryGetValue(textureID, out img))
            {
                return true;
            }

            TryLoadTexture(textureID);

            if (m_Textures.TryGetValue(textureID, out img))
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

        public bool TryGetBump(UUID textureID, BakeTarget bakeType, out byte[] bump)
        {
            if (textureID == UUID.Zero)
            {
                bump = null;
                return false;
            }

            int targetDimension;
            Dictionary<UUID, byte[]> resizeCache;
            if (bakeType == BakeTarget.Eyes)
            {
                resizeCache = m_BumpsResized128;
                targetDimension = 128;
            }
            else
            {
                resizeCache = m_BumpsResized512;
                targetDimension = 512;
            }

            /* do not redo the hard work of rescaling images unnecessarily */
            if (resizeCache.TryGetValue(textureID, out bump))
            {
                return true;
            }

            TryLoadTexture(textureID);

            if (m_Bumps.TryGetValue(textureID, out bump))
            {
                if (bump.Length != targetDimension * targetDimension)
                {
                    bump = ResizeBump(bump, targetDimension);
                    resizeCache.Add(textureID, bump);
                }
                return true;
            }

            bump = null;
            return false;
        }

        private byte[] ResizeBump(byte[] srcbump, int targetDimension)
        {
            int n = targetDimension * targetDimension;
            int di = 0, si;

            var dstbump = new byte[n];
            int srcDimension = 128;
            if(srcbump.Length == 512 * 512)
            {
                srcDimension = 512;
            }

            for (int y = 0; y < targetDimension; y++)
            {
                for (int x = 0; x < targetDimension; x++)
                {
                    si = (y * srcDimension / targetDimension) * srcDimension + (x * targetDimension / srcDimension);
                    dstbump[di] = srcbump[si];
                    di++;
                }
            }

            return dstbump;
        }

        public void Dispose()
        {
            foreach (var img in m_Textures.Values)
            {
                img.Dispose();
            }
            m_Textures.Clear();
            foreach (var img in m_TexturesResized128.Values)
            {
                img.Dispose();
            }
            m_TexturesResized128.Clear();
            foreach (var img in m_TexturesResized512.Values)
            {
                img.Dispose();
            }
            m_TexturesResized512.Clear();
            m_Bumps.Clear();
            m_BumpsResized128.Clear();
            m_BumpsResized512.Clear();
        }

        public BakeOutput Process(BakeCache cache, AssetServiceInterface assetSource, Action<string> logOutput = null)
        {
            var output = new BakeOutput();
            if(cache.IsBaked)
            {
                throw new AlreadyBakedException();
            }

            m_AssetService = assetSource;

            output.VisualParams = VisualParamsMapper.CreateVisualParams(cache.Wearables);

            var Tgt = new Targets();
            var SourceBakers = new Dictionary<WearableType, List<AbstractSubBaker>>();
            foreach(WearableType t in Enum.GetValues(typeof(WearableType)))
            {
                SourceBakers.Add(t, new List<AbstractSubBaker>());
            }

            foreach(AbstractSubBaker subbaker in cache.SubBakers)
            {
                SourceBakers[subbaker.Type].Add(subbaker);
            }

            try
            {
                foreach(BakeTarget idx in BakeIndices)
                {
                    int dimensions = idx == BakeTarget.Eyes ? 128 : 512;
                    Bitmap bmp;
                    if(idx == BakeTarget.Skirt && SourceBakers[WearableType.Skirt].Count == 0)
                    {
                        continue;
                    }
                    Tgt.Images.Add(idx, bmp = new Bitmap(dimensions, dimensions, PixelFormat.Format32bppArgb));
                    Graphics gfx = Graphics.FromImage(bmp);
                    if(idx == BakeTarget.Hair)
                    {
                        gfx.CompositingMode = CompositingMode.SourceCopy;
                        using (var b = new SolidBrush(Color.FromArgb(0, Color.White)))
                        {
                            gfx.FillRectangle(b, new Rectangle(0, 0, dimensions, dimensions));
                        }
                    }
                    gfx.CompositingMode = CompositingMode.SourceOver;
                    Tgt.Graphics.Add(idx, gfx);
                }

                logOutput?.Invoke("Processing R,G,B and bump parts");
                DrawSubBakers(Tgt, SourceBakers[WearableType.Skin], SkinIndices);
                DrawSubBakers(Tgt, SourceBakers[WearableType.Tattoo], SkinIndices);
                DrawSubBakers(Tgt, SourceBakers[WearableType.Hair], new BakeTarget[] { BakeTarget.Hair });
                DrawSubBakers(Tgt, SourceBakers[WearableType.Eyes].OrderBy(item => item.Ordinal), new BakeTarget[] { BakeTarget.Eyes });
                DrawSubBakers(Tgt, SourceBakers[WearableType.Underpants].OrderBy(item => item.Ordinal), new BakeTarget[] { BakeTarget.LowerBody });
                DrawSubBakers(Tgt, SourceBakers[WearableType.Undershirt].OrderBy(item => item.Ordinal), new BakeTarget[] { BakeTarget.UpperBody });
                DrawSubBakers(Tgt, SourceBakers[WearableType.Socks].OrderBy(item => item.Ordinal), new BakeTarget[] { BakeTarget.LowerBody });
                DrawSubBakers(Tgt, SourceBakers[WearableType.Shoes].OrderBy(item => item.Ordinal), new BakeTarget[] { BakeTarget.LowerBody });
                DrawBumpMaps(Tgt, SourceBakers[WearableType.Shoes], new BakeTarget[] { BakeTarget.LowerBody });
                DrawSubBakers(Tgt, SourceBakers[WearableType.Pants].OrderBy(item => item.Ordinal), new BakeTarget[] { BakeTarget.LowerBody });
                DrawBumpMaps(Tgt, SourceBakers[WearableType.Pants], new BakeTarget[] { BakeTarget.LowerBody });
                DrawSubBakers(Tgt, SourceBakers[WearableType.Shirt].OrderBy(item => item.Ordinal), new BakeTarget[] { BakeTarget.UpperBody });
                DrawBumpMaps(Tgt, SourceBakers[WearableType.Shirt], new BakeTarget[] { BakeTarget.UpperBody });
                DrawSubBakers(Tgt, SourceBakers[WearableType.Jacket].OrderBy(item => item.Ordinal), ClothingIndices);
                DrawBumpMaps(Tgt, SourceBakers[WearableType.Jacket], ClothingIndices);
                DrawSubBakers(Tgt, SourceBakers[WearableType.Gloves].OrderBy(item => item.Ordinal), new BakeTarget[] { BakeTarget.UpperBody });
                DrawBumpMaps(Tgt, SourceBakers[WearableType.Gloves], new BakeTarget[] { BakeTarget.UpperBody });
                DrawSubBakers(Tgt, SourceBakers[WearableType.Skirt].OrderBy(item => item.Ordinal), new BakeTarget[] { BakeTarget.Skirt });
                DrawBumpMaps(Tgt, SourceBakers[WearableType.Skirt], new BakeTarget[] { BakeTarget.Skirt });

                /* for alpha masks we have to get rid of the Graphics */
                foreach (Graphics gfx in Tgt.Graphics.Values)
                {
                    gfx.Dispose();
                }
                Tgt.Graphics.Clear();

                logOutput?.Invoke("Processing alpha mask");
                /* clean out alpha channel. the ones we used before are not necessary anymore */
                foreach (KeyValuePair<BakeTarget, Bitmap> kvp in Tgt.Images)
                {
                    int byteSize = kvp.Value.Width * kvp.Value.Height * 4;
                    BitmapData lockBits = kvp.Value.LockBits(Tgt.Rectangles[kvp.Key], ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                    var rawdata = new byte[byteSize];
                    Marshal.Copy(lockBits.Scan0, rawdata, 0, byteSize);
                    for(int bytePos = byteSize; bytePos-- != 0; bytePos -= 3)
                    {
                        rawdata[bytePos] = 255;
                    }
                    Marshal.Copy(rawdata, 0, lockBits.Scan0, byteSize);
                    kvp.Value.UnlockBits(lockBits);
                }

                if (SourceBakers[WearableType.Alpha].Count != 0)
                {
                    var AlphaMaskBakes = new Dictionary<BakeTarget, byte[]>();

                    foreach(AbstractSubBaker baker in SourceBakers[WearableType.Alpha])
                    {
                        foreach (BakeTarget tgt in BakeIndices)
                        {
                            Image srcimg;
                            Bitmap tgtimg;
                            byte[] dstAlphaMask;
                            byte[] srcAlphaMask;
                            int dimensions = tgt == BakeTarget.Eyes ? 128 : 512;
                            int imagebytes = dimensions * dimensions * 4;

                            srcimg = baker.BakeAlphaMaskOutput(this, tgt);
                            if(srcimg == null)
                            {
                                continue;
                            }

                            if(!AlphaMaskBakes.TryGetValue(tgt, out dstAlphaMask))
                            {
                                if(!Tgt.Images.TryGetValue(tgt, out tgtimg))
                                {
                                    continue;
                                }
                                dstAlphaMask = GetRawData(tgtimg);
                            }

                            using (Bitmap srcbmp = new Bitmap(srcimg))
                            {
                                srcAlphaMask = GetRawData(srcbmp);
                            }

                            for(int idx = imagebytes; idx-- != 0; idx -= 3)
                            {
                                dstAlphaMask[idx] = Math.Min(dstAlphaMask[idx], srcAlphaMask[idx]);
                            }
                        }
                    }

                    foreach(KeyValuePair<BakeTarget, byte[]> kvp in AlphaMaskBakes)
                    {
                        UpdateRawData(Tgt.Images[kvp.Key], kvp.Value);
                    }
                }

                logOutput?.Invoke("Compressing bakes");
                byte[] finalbump;
                output.HairBake = new AssetData
                {
                    ID = UUID.RandomFixedFirst(0xffffffff),
                    Type = AssetType.Texture,
                    Temporary = true,
                    Data = Tgt.Bumps.TryGetValue(BakeTarget.Hair, out finalbump) ? 
                        J2cEncoder.EncodeWithBump(Tgt.Images[BakeTarget.Hair], true, finalbump) :
                        J2cEncoder.Encode(Tgt.Images[BakeTarget.Hair], true),
                    Name = "Bake Texture Hair"
                };
                output.HeadBake = new AssetData
                {
                    ID = UUID.RandomFixedFirst(0xffffffff),
                    Type = AssetType.Texture,
                    Temporary = true,
                    Data = J2cEncoder.EncodeWithBump(Tgt.Images[BakeTarget.Head], true, Tgt.Bumps[BakeTarget.Head]),
                    Name = "Bake Texture Head"
                };

                output.UpperBake = new AssetData
                {
                    ID = UUID.RandomFixedFirst(0xffffffff),
                    Type = AssetType.Texture,
                    Temporary = true,
                    Data = J2cEncoder.EncodeWithBump(Tgt.Images[BakeTarget.UpperBody], true, Tgt.Bumps[BakeTarget.UpperBody]),
                    Name = "Bake Texture Upperbody"
                };

                output.LowerBake = new AssetData
                {
                    ID = UUID.RandomFixedFirst(0xffffffff),
                    Type = AssetType.Texture,
                    Temporary = true,
                    Data = J2cEncoder.EncodeWithBump(Tgt.Images[BakeTarget.LowerBody], true, Tgt.Bumps[BakeTarget.LowerBody]),
                    Name = "Bake Texture Lowerbody"
                };

                output.EyeBake = new AssetData
                {
                    ID = UUID.RandomFixedFirst(0xffffffff),
                    Type = AssetType.Texture,
                    Temporary = true,
                    Data = J2cEncoder.Encode(Tgt.Images[BakeTarget.Eyes], true),
                    Name = "Bake Texture Eyes"
                };

                Bitmap finalSkirt;
                if(Tgt.Images.TryGetValue(BakeTarget.Skirt, out finalSkirt))
                {
                    output.SkirtBake = new AssetData
                    {
                        ID = UUID.RandomFixedFirst(0xffffffff),
                        Type = AssetType.Texture,
                        Temporary = true,
                        Data = Tgt.Bumps.TryGetValue(BakeTarget.Skirt, out finalbump) ?
                            J2cEncoder.EncodeWithBump(finalSkirt, true, finalbump) :
                            J2cEncoder.Encode(finalSkirt, true),
                        Name = "Bake Texture Skirt"
                    };
                }
            }
            finally
            {
                foreach(Graphics gfx in Tgt.Graphics.Values)
                {
                    gfx.Dispose();
                }
                foreach(Bitmap bmp in Tgt.Images.Values)
                {
                    bmp.Dispose();
                }
            }

            return output;
        }

        private void DrawBumpMaps(Targets Tgt, IEnumerable<AbstractSubBaker> bakers, BakeTarget[] targets)
        {
            foreach(BakeTarget target in targets)
            {
                foreach(AbstractSubBaker baker in bakers)
                {
                    byte[] tgtbump;
                    if(!Tgt.Bumps.TryGetValue(target, out tgtbump))
                    {
                        tgtbump = target == BakeTarget.Eyes ? new byte[128 * 128] : new byte[512 * 512];
                        Tgt.Bumps.Add(target, tgtbump);
                    }

                    byte[] bump = baker.BakeBumpOutput(this, target);
                    if(bump != null)
                    {
                        ApplyBump(tgtbump, bump);
                    }
                }
            }
        }

        private void ApplyBump(byte[] tgt, byte[] src)
        {
            if (tgt.Length != src.Length)
            {
                throw new ArgumentException(nameof(src));
            }

            for (int i = 0; i < tgt.Length; ++i)
            {
                tgt[i] = Math.Max(tgt[i], src[i]);
            }
        }

        private byte[] GetRawData(Bitmap bmp)
        {
            int byteCount = bmp.Width * bmp.Height * 4;
            var rawdata = new byte[byteCount];
            BitmapData bmpLock = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(bmpLock.Scan0, rawdata, 0, byteCount);
            bmp.UnlockBits(bmpLock);
            return rawdata;
        }

        private void UpdateRawData(Bitmap bmp, byte[] rawdata)
        {
            int byteCount = bmp.Width * bmp.Height * 4;
            if(byteCount != rawdata.Length)
            {
                throw new ArgumentException(nameof(rawdata));
            }
            BitmapData bmpLock = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(rawdata, 0, bmpLock.Scan0, byteCount);
            bmp.UnlockBits(bmpLock);
        }

        private void DrawSubBakers(Targets Tgt, IEnumerable<AbstractSubBaker> bakers, BakeTarget[] targets)
        {
            foreach (AbstractSubBaker baker in bakers)
            {
                foreach (BakeTarget tgt in targets)
                {
                    Tgt.Graphics[tgt].DrawTinted(
                        Tgt.Rectangles[tgt],
                        baker.BakeImageOutput(this, tgt),
                        baker.BakeImageColor(tgt));
                }
            }
        }

        private static readonly BakeTarget[] ClothingIndices =
        {
            BakeTarget.LowerBody,
            BakeTarget.UpperBody
        };

        private static readonly BakeTarget[] SkinIndices =
        {
            BakeTarget.Head,
            BakeTarget.LowerBody,
            BakeTarget.UpperBody
        };

        private static readonly BakeTarget[] BakeIndices =
        {
            BakeTarget.Hair,
            BakeTarget.Eyes,
            BakeTarget.Head,
            BakeTarget.LowerBody,
            BakeTarget.UpperBody,
            BakeTarget.Skirt
        };
    }
}
