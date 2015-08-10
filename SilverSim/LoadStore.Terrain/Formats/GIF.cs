﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Drawing.Imaging;

namespace SilverSim.LoadStore.Terrain.Formats
{
    [TerrainStorageType]
    public class GIF : DrawingSaveCommon
    {
        public override string Name
        {
            get
            {
                return "gif";
            }
        }

        protected override ImageFormat TargetImageFormat
        {
            get
            {
                return ImageFormat.Gif;
            }
        }
    }
}
