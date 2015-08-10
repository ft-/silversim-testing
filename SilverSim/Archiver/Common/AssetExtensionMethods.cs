// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Archiver.Tar;
using SilverSim.Types;
using SilverSim.Types.Asset;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SilverSim.Archiver.Common
{
    public static class AssetExtensionMethods
    {
        public static AssetData LoadAsset(
            this TarArchiveReader reader,
            TarArchiveReader.Header hdr,
            UUI creator)
        {
            AssetData asset = new AssetData();
            asset.FileName = hdr.FileName;
            byte[] assetData = new byte[hdr.Length];
            if(hdr.Length != reader.Read(assetData, 0, hdr.Length))
            {
                throw new IOException();
            }
            asset.Name = "From Archive";
            asset.Data = assetData;
            asset.Creator = creator;

            return asset;
        }
    }
}
