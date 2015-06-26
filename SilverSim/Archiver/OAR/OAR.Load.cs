﻿/*

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

using SilverSim.Archiver.Common;
using SilverSim.Archiver.Tar;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Parcel;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace SilverSim.Archiver.OAR
{
    public static partial class OAR
    {
        public class MultiRegionOARLoadingTriedOnRegion : Exception
        {
        }

        public class OARFormatException : Exception
        {
        }

        [Flags]
        public enum LoadOptions
        {
            None = 0,
            Merge = 0x000000001,
            NoAssets = 0x00000002
        }

        public static void Load(
            SceneInterface scene,
            LoadOptions options,
            Stream inputFile)
        {
            TarArchiveReader reader;
            {
                GZipStream gzipStream = new GZipStream(inputFile, CompressionMode.Decompress);
                reader = new TarArchiveReader(gzipStream);
            }

            GridVector baseLoc = scene.RegionData.Location;

            GridVector regionSize = new GridVector(256, 256);
            Dictionary<string, ArchiveXmlLoader.RegionInfo> regionMapping = new Dictionary<string, ArchiveXmlLoader.RegionInfo>();
            List<ArchiveXmlLoader.RegionInfo> regionInfos = new List<ArchiveXmlLoader.RegionInfo>();
            bool parcelsCleared = false;

            for (; ; )
            {
                TarArchiveReader.Header header;
                try
                {
                    header = reader.ReadHeader();
                }
                catch (TarArchiveReader.EndOfTarException)
                {
                    return;
                }

                if (header.FileType == TarFileType.File)
                {
                    if(header.FileName == "archive.xml")
                    {
                        ArchiveXmlLoader.RegionInfo rinfo = ArchiveXmlLoader.LoadArchiveXml(new ObjectXmlStreamFilter(reader), regionInfos);

                        regionSize = rinfo.RegionSize;
                        foreach (ArchiveXmlLoader.RegionInfo reginfo in regionInfos)
                        {
                            regionMapping.Add(reginfo.Path, reginfo);
                        }
                        if(regionInfos.Count != 0 && scene != null)
                        {
                            throw new MultiRegionOARLoadingTriedOnRegion();
                        }
                    }
                    if (header.FileName.StartsWith("assets/") && (options & LoadOptions.NoAssets) == 0)
                    {
                        /* Load asset */
                        AssetData ad = reader.LoadAsset(header, scene.Owner);
                        scene.AssetService.Store(ad);
                    }

                    if (header.FileName.StartsWith("regions/"))
                    {
                        string[] pcomps = header.FileName.Split(new char[] { '/' }, 3);
                        if (pcomps.Length < 3)
                        {
                            throw new OARFormatException();
                        }
                        string regionname = pcomps[1];
                        header.FileName = pcomps[2];
                        regionSize = regionMapping[regionname].RegionSize;
                        scene = SceneManager.Scenes[regionMapping[regionname].ID];
                        parcelsCleared = false;
                    }

                    if (header.FileName.StartsWith("objects/"))
                    {
                        /* Load objects */
                        List<ObjectGroup> sogs = ObjectXML.fromXml(reader, scene.Owner);
                        foreach (ObjectGroup sog in sogs)
                        {
                            if(sog.Owner.ID == UUID.Zero)
                            {
                                sog.Owner = scene.Owner;
                            }
                            scene.Add(sog);
                        }
                    }
                    if (header.FileName.StartsWith("terrains/") && (options & LoadOptions.Merge) == 0)
                    {
                        /* Load terrains */
                        scene.Terrain.AllPatches = TerrainLoader.LoadStream(reader, (int)regionSize.X, (int)regionSize.Y);
                    }
                    if (header.FileName.StartsWith("landdata/") && (options & LoadOptions.Merge) == 0)
                    {
                        /* Load landdata */
                        if ((options & LoadOptions.Merge) == 0 && !parcelsCleared)
                        {
                            scene.ClearParcels();
                            parcelsCleared = true;
                        }
                        ParcelInfo pinfo = ParcelLoader.LoadParcel(new ObjectXmlStreamFilter(reader), regionSize);
                        if(pinfo.Owner.ID == UUID.Zero)
                        {
                            pinfo.Owner = scene.Owner;
                        }
                        scene.AddParcel(pinfo);
                    }
                    if (header.FileName.StartsWith("settings/") && (options & LoadOptions.Merge) == 0)
                    {
                        /* Load settings */
                        RegionSettingsLoader.LoadRegionSettings(new ObjectXmlStreamFilter(reader), scene);
                    }
                }
            }
        }
    }
}
