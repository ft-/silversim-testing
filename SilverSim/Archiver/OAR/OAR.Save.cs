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

using SilverSim.Archiver.Tar;
using SilverSim.LL.Messages.LayerData;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Parcel;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace SilverSim.Archiver.OAR
{
    public static partial class OAR
    {
        [Flags]
        public enum SaveOptions
        {
            NoAssets = 0x00000001,
            Publish = 0x00000002,
        }

        public static void Save(
            SceneInterface scene,
            SaveOptions options,
            string fileName)
        {
            TarArchiveWriter writer = new TarArchiveWriter(new GZipStream(new FileStream(fileName, FileMode.Create), CompressionMode.Compress));

            bool saveAssets = (options & SaveOptions.NoAssets) == 0;
            XmlSerializationOptions xmloptions = XmlSerializationOptions.None;
            if((options & SaveOptions.Publish) == 0)
            {
                xmloptions |= XmlSerializationOptions.WriteOwnerInfo;
            }

            writer.WriteFile("archive.xml", WriteArchiveXml08(scene, saveAssets));

            Dictionary<string, AssetData> objectAssets = new Dictionary<string,AssetData>();

            foreach (ObjectGroup sog in scene.Objects)
            {
                if(sog.IsTemporary)
                {
                    /* skip temporary */
                    continue;
                }
                using(MemoryStream ms = new MemoryStream())
                {
                    using(XmlTextWriter objectwriter = new XmlTextWriter(ms, UTF8NoBOM))
                    {
                        sog.ToXml(objectwriter, xmloptions);
                        AssetData data = new AssetData();
                        data.Data = ms.GetBuffer();
                        data.Type = AssetType.Object;
                        objectAssets.Add(sog.Name + "_" + sog.GlobalPosition.X_String + "-" + sog.GlobalPosition.Y_String + "-" + sog.GlobalPosition.Z_String + "__" + sog.ID + ".xml", data);
                    }
                }
            }

            if (saveAssets)
            {
                /* we only parse sim details when saving assets */
                List<UUID> assetIDs = new List<UUID>();
                AssetData data;

                foreach (AssetData objdata in objectAssets.Values)
                {
                    foreach (UUID id in objdata.References)
                    {
                        if (!assetIDs.Contains(id))
                        {
                            assetIDs.Add(id);
                        }
                    }
                }

                foreach (ParcelInfo pinfo in scene.Parcels)
                {
                    assetIDs.Add(pinfo.MediaID);
                    assetIDs.Add(pinfo.SnapshotID);
                }

                int assetidx = 0;
                while(assetidx < assetIDs.Count)
                {
                    UUID assetID = assetIDs[assetidx++];
                    try
                    {
                        data = scene.AssetService[assetID];
                    }
                    catch
                    {
                        continue;
                    }
                    writer.WriteAsset(data);
                    foreach(UUID refid in data.References)
                    {
                        if(!assetIDs.Contains(refid))
                        {
                            assetIDs.Add(refid);
                        }
                    }
                }
            }

            foreach(ParcelInfo pinfo in scene.Parcels)
            {
                using(MemoryStream ms = new MemoryStream())
                {
                    using(XmlTextWriter xmlwriter = new XmlTextWriter(ms, UTF8NoBOM))
                    {
                        xmlwriter.WriteStartElement("LandData");
                        {
                            xmlwriter.WriteNamedValue("Area", pinfo.Area);
                            xmlwriter.WriteNamedValue("AuctionID", pinfo.AuctionID);
                            xmlwriter.WriteNamedValue("AuthBuyerID", pinfo.AuthBuyer.ID);
                            xmlwriter.WriteNamedValue("Category", (byte)pinfo.Category);
                            xmlwriter.WriteNamedValue("ClaimDate", pinfo.ClaimDate.DateTimeToUnixTime());
                            xmlwriter.WriteNamedValue("ClaimPrice", pinfo.ClaimPrice);
                            xmlwriter.WriteNamedValue("GlobalID", pinfo.ID);
                            xmlwriter.WriteNamedValue("GroupID", pinfo.Group.ID);
                            xmlwriter.WriteNamedValue("IsGroupOwned", pinfo.GroupOwned);
                            xmlwriter.WriteNamedValue("Bitmap", Convert.ToBase64String(pinfo.LandBitmap.Data));
                            xmlwriter.WriteNamedValue("Description", pinfo.Description);
                            xmlwriter.WriteNamedValue("Flags", (uint)pinfo.Flags);
                            xmlwriter.WriteNamedValue("LandingType", (uint)pinfo.LandingType);
                            xmlwriter.WriteNamedValue("Name", pinfo.Name);
                            xmlwriter.WriteNamedValue("Status", (uint)pinfo.Status);
                            xmlwriter.WriteNamedValue("LocalID", pinfo.LocalID);
                            xmlwriter.WriteNamedValue("MediaAutoScale", pinfo.MediaAutoScale);
                            xmlwriter.WriteNamedValue("MediaID", pinfo.MediaID);
                            xmlwriter.WriteNamedValue("MediaURL", pinfo.MediaURI.ToString());
                            xmlwriter.WriteNamedValue("MusicURL", pinfo.MusicURI.ToString());
                            xmlwriter.WriteNamedValue("OwnerID", pinfo.Owner.ID);
                            xmlwriter.WriteStartElement("ParcelAccessList");
                            {
#warning Implement saving Parcel Access List
                            }
                            xmlwriter.WriteEndElement();
                            xmlwriter.WriteNamedValue("PassHours", pinfo.PassHours);
                            xmlwriter.WriteNamedValue("PassPrice", pinfo.PassPrice);
                            xmlwriter.WriteNamedValue("SalePrice", pinfo.SalePrice);
                            xmlwriter.WriteNamedValue("SnapshotID", pinfo.SnapshotID);
                            xmlwriter.WriteNamedValue("UserLocation", pinfo.LandingPosition.ToString());
                            xmlwriter.WriteNamedValue("UserLookAt", pinfo.LandingLookAt.ToString());
                            xmlwriter.WriteNamedValue("Dwell", pinfo.Dwell);
                            xmlwriter.WriteNamedValue("OtherCleanTime", pinfo.OtherCleanTime);
                        }
                        xmlwriter.WriteEndElement();

                        writer.WriteFile("landdata/" + pinfo.ID + ".xml", ms.GetBuffer());
                    }
                }
            }

            foreach(KeyValuePair<string, AssetData> kvp in objectAssets)
            {
                writer.WriteFile("objects/" + kvp.Key, kvp.Value.Data);
            }

            writer.WriteFile("terrains/" + scene.RegionData.Name + ".r32", GenTerrainFile(scene.Terrain.AllPatches));
        }

        static byte[] WriteArchiveXml08(SceneInterface scene, bool assetsIncluded)
        {
            using(MemoryStream ms = new MemoryStream())
            {
                using(XmlTextWriter writer = new XmlTextWriter(ms, UTF8NoBOM))
                {
                    writer.WriteStartElement("archive");
                    writer.WriteAttributeString("major_version", "0");
                    writer.WriteAttributeString("major_version", "8");
                    {
                        writer.WriteStartElement("creation_info");
                        {
                            writer.WriteNamedValue("datetime", Date.GetUnixTime());
                            writer.WriteNamedValue("id", scene.ID);
                        }
                        writer.WriteEndElement();

                        writer.WriteNamedValue("assets_includes", assetsIncluded);

                        writer.WriteStartElement("region_info");
                        {
                            writer.WriteNamedValue("is_megaregion", false);
                            writer.WriteNamedValue("size_in_meters", scene.RegionData.Size.ToString());
                        }
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();

                    return ms.GetBuffer();
                }
            }
        }

        static byte[] GenTerrainFile(List<LayerPatch> terrain)
        {
            using (MemoryStream output = new MemoryStream())
            {
                float[] outdata = new float[terrain.Count * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES];
                uint minX = terrain[0].X;
                uint minY = terrain[0].Y;
                uint maxX = terrain[0].X;
                uint maxY = terrain[0].Y;

                /* determine line width */
                foreach (LayerPatch p in terrain)
                {
                    minX = Math.Min(minX, p.X);
                    minY = Math.Min(minY, p.Y);
                    maxX = Math.Max(maxX, p.X);
                    maxY = Math.Max(maxY, p.Y);
                }

                uint linewidth = maxX - minX + 1;

                /* build output data */
                foreach (LayerPatch p in terrain)
                {
                    for (uint y = 0; y < LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++y)
                    {
                        for (uint x = 0; x < LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++x)
                        {
                            outdata[LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES * y + x + p.XYToYNormal(linewidth, minY)] = p[x, y];
                        }
                    }
                }

                using (BinaryWriter bs = new BinaryWriter(output))
                {
                    foreach (float f in outdata)
                    {
                        bs.Write(f);
                    }
                    return output.GetBuffer();
                }
            }
        }

        public static uint XYToYNormal(this LayerPatch p, uint lineWidth, uint minY)
        {
            return (p.Y - minY) * lineWidth + p.X;
        }

        static UTF8Encoding UTF8NoBOM = new UTF8Encoding(false);
    }
}
