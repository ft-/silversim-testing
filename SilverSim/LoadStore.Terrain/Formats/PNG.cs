// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.ComponentModel;
using System.Drawing.Imaging;

namespace SilverSim.LoadStore.Terrain.Formats
{
    [TerrainStorageType]
    [Description("PNG Terrain Storage Format Writer")]
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
