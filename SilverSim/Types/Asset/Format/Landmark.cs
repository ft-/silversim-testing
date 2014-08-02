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
        public URI GatekeeperURI = null;
        #endregion

        #region Constructors
        public Landmark()
        {

        }

        public Landmark(AssetData asset)
        {
            string input = Encoding.UTF8.GetString(asset.Data);
            input = input.Replace('\t', ' ');
            string[] lines = input.Split('\n');
            string[] versioninfo = lines[0].Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries);
            if(versioninfo[0] != "Landmark")
            {
                throw new NotALandmarkFormat();
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
                    LocalPos = new Vector3(float.Parse(para[1]), float.Parse(para[2]), float.Parse(para[3]));
                }
                else if(para.Length == 2 && para[0] == "region_handle")
                {
                    Location = new GridVector(ulong.Parse(para[1]));
                }
                else if(para.Length == 2 && para[0] == "gatekeeper")
                {
                    GatekeeperURI = new URI(para[1]);
                }
            }
        }
        #endregion

        #region Operators
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

            asset.Data = Encoding.UTF8.GetBytes(landmarkdata);
            asset.Type = AssetType.Landmark;
            asset.Name = "Landmark";
            return asset;
        }
        #endregion
    }
}
