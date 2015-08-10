﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Archiver.Common;
using SilverSim.Archiver.Tar;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Asset;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace SilverSim.Archiver.Assets
{
    public static class AssetsLoad
    {
        public static void Load(
            AssetServiceInterface assetService,
            UUI owner,
            Stream inputFile)
        {
            TarArchiveReader reader;
            {
                GZipStream gzipStream = new GZipStream(inputFile, CompressionMode.Decompress);
                reader = new TarArchiveReader(gzipStream);
            }

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
                    if (header.FileName.StartsWith("assets/"))
                    {
                        /* Load asset */
                        AssetData ad = reader.LoadAsset(header, owner);
                        try
                        {
                            assetService.exists(ad.ID);
                        }
                        catch
                        {
                            assetService.Store(ad);
                        }
                    }
                }
            }
        }
    }
}
