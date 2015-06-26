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
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using SilverSim.Archiver.Common;
using SilverSim.Types.Asset;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Management.Scene;
using System.Xml;
using SilverSim.Types.Asset.Format;

namespace SilverSim.Archiver.OAR
{
    public static partial class OAR
    {
        public class OARFormatException : Exception
        {
            public OARFormatException()
            {

            }
        }

        [Flags]
        public enum LoadOptions
        {
            Merge = 0x000000001,
            NoAssets = 0x00000002
        }

        public static void Load(
            SceneInterface scene,
            LoadOptions options,
            string fileName)
        {
            TarArchiveReader reader;
            {
                FileStream inputFile = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                GZipStream gzipStream = new GZipStream(inputFile, CompressionMode.Decompress);
                reader = new TarArchiveReader(gzipStream);
            }

            GridVector baseLoc = scene.RegionData.Location;

            GridVector regionSize = new GridVector(256, 256);
            Dictionary<string, ArchiveXmlLoader.RegionInfo> regionMapping = new Dictionary<string, ArchiveXmlLoader.RegionInfo>();
            List<ArchiveXmlLoader.RegionInfo> regionInfos = new List<ArchiveXmlLoader.RegionInfo>();

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
                        ArchiveXmlLoader.RegionInfo rinfo = ArchiveXmlLoader.LoadArchiveXml(reader, regionInfos);

                        regionSize = rinfo.RegionSize;
                        foreach (ArchiveXmlLoader.RegionInfo reginfo in regionInfos)
                        {
                            regionMapping.Add(reginfo.Path, reginfo);
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
                    }

                    if (header.FileName.StartsWith("objects/"))
                    {
                        /* Load objects */
                        List<ObjectGroup> sogs = ObjectXML.fromXml(reader, scene.Owner);
                        foreach (ObjectGroup sog in sogs)
                        {
                            scene.Add(sog);
                        }
                    }
                    if (header.FileName.StartsWith("terrains/"))
                    {
                        /* Load terrains */
                    }
                    if (header.FileName.StartsWith("landdata/"))
                    {
                        /* Load landdata */
                    }
                    if (header.FileName.StartsWith("settings/"))
                    {
                        /* Load settings */
                        RegionSettingsLoader.LoadRegionSettings(reader, scene);
                    }
                }
            }

        }
    }
}
