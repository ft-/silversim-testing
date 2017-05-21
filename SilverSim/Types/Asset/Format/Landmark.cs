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

using System;
using System.Globalization;
using System.Text;

namespace SilverSim.Types.Asset.Format
{
    public class Landmark
    {
        #region Fields
        public UUID RegionID = UUID.Zero;
        public Vector3 LocalPos = Vector3.Zero;
        public GridVector Location = GridVector.Zero;
        public URI GatekeeperURI;
        #endregion

        #region Constructors
        public Landmark()
        {
        }

        public Landmark(AssetData asset)
        {
            var input = Encoding.ASCII.GetString(asset.Data);
            input = input.Replace('\t', ' ');
            var lines = input.Split('\n');
            var versioninfo = lines[0].Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries);
            if(versioninfo[0] != "Landmark")
            {
                throw new NotALandmarkFormatException();
            }

            for(int idx = 1; idx < lines.Length; ++idx)
            {
                var line = lines[idx];
                var para = line.Split(' ');
                if(para.Length == 2 && para[0] == "region_id")
                {
                    RegionID = para[1];
                }
                else if(para.Length == 4 && para[0] == "local_pos")
                {
                    double x;
                    double y;
                    double z;

                    if(!double.TryParse(para[1], NumberStyles.Float, CultureInfo.InvariantCulture, out x) ||
                        !double.TryParse(para[2], NumberStyles.Float, CultureInfo.InvariantCulture, out y) ||
                        !double.TryParse(para[3], NumberStyles.Float, CultureInfo.InvariantCulture, out z))
                    {
                        throw new NotALandmarkFormatException();
                    }
                    LocalPos = new Vector3(x, y, z);
                }
                else if(para.Length == 2 && para[0] == "region_handle")
                {
                    ulong u;
                    if(!ulong.TryParse(para[1], out u))
                    {
                        throw new NotALandmarkFormatException();
                    }
                    Location = new GridVector(u);
                }
                else if(para.Length == 2 && para[0] == "gatekeeper")
                {
                    GatekeeperURI = new URI(para[1]);
                }
            }
        }
        #endregion

        #region Operators

        public AssetData Asset() => this;

        public static implicit operator AssetData(Landmark v)
        {
            var asset = new AssetData();
            string landmarkdata = (v.GatekeeperURI != null) ?
                string.Format("Landmark version 2\nregion_id {0}\nlocal_pos {1} {2} {3}\nregion_handle {4}\ngatekeeper {5}\n",
                    v.RegionID,
                    v.LocalPos.X, v.LocalPos.Y, v.LocalPos.Z,
                    v.Location.RegionHandle,
                    v.GatekeeperURI) :
                string.Format("Landmark version 2\nregion_id {0}\nlocal_pos {1} {2} {3}\nregion_handle {4}\n",
                    v.RegionID,
                    v.LocalPos.X, v.LocalPos.Y, v.LocalPos.Z,
                    v.Location.RegionHandle);

            asset.Data = Encoding.ASCII.GetBytes(landmarkdata);
            asset.Type = AssetType.Landmark;
            asset.Name = "Landmark";
            return asset;
        }
        #endregion
    }
}
