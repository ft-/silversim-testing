// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
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
            string input = Encoding.ASCII.GetString(asset.Data);
            input = input.Replace('\t', ' ');
            string[] lines = input.Split('\n');
            string[] versioninfo = lines[0].Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries);
            if(versioninfo[0] != "Landmark")
            {
                throw new NotALandmarkFormatException();
            }

            for(int idx = 1; idx < lines.Length; ++idx)
            {
                string line = lines[idx];
                string[] para = line.Split(' ');
                if(para.Length == 2 && para[0] == "region_id")
                {
                    RegionID = para[1];
                }
                else if(para.Length == 4 && para[0] == "local_pos")
                {
                    double x, y, z;
                    if(!double.TryParse(para[1], out x) || double.TryParse(para[2], out y) || double.TryParse(para[3], out z))
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

        public AssetData Asset()
        {
            return (AssetData)this;
        }

        public static implicit operator AssetData(Landmark v)
        {
            AssetData asset = new AssetData();
            string landmarkdata;
            
            if(v.GatekeeperURI != null)
            {
                landmarkdata = String.Format("Landmark version 2\nregion_id {0}\nlocal_pos {1} {2} {3}\nregion_handle {4}\ngatekeeper {5}\n",
                    v.RegionID,
                    v.LocalPos.X, v.LocalPos.Y, v.LocalPos.Z,
                    v.Location.RegionHandle,
                    v.GatekeeperURI);
            }
            else
            {
                landmarkdata = String.Format("Landmark version 2\nregion_id {0}\nlocal_pos {1} {2} {3}\nregion_handle {4}\n",
                    v.RegionID,
                    v.LocalPos.X, v.LocalPos.Y, v.LocalPos.Z,
                    v.Location.RegionHandle);
            }

            asset.Data = Encoding.ASCII.GetBytes(landmarkdata);
            asset.Type = AssetType.Landmark;
            asset.Name = "Landmark";
            return asset;
        }
        #endregion
    }
}
