// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Viewer.Messages.LayerData;
using System.Collections.Generic;

namespace SilverSim.Scene.Types.Scene
{
    public interface ITerrainListener
    {
        void TerrainUpdate(LayerPatch layerpath);
    }
}
