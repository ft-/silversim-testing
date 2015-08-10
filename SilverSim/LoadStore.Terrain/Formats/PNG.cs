// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Drawing.Imaging;

namespace SilverSim.LoadStore.Terrain.Formats
{
    [TerrainStorageType]
    public class PNG : DrawingSaveCommon
    {
        public override string Name
        {
            get
            {
                return "png";
            }
        }

        protected override ImageFormat TargetImageFormat
        {
            get 
            {
                return ImageFormat.Png;
            }
        }
    }
}
