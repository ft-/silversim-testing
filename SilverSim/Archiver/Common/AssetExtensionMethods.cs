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
