// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Viewer.Messages.LayerData;
using System.Collections.Generic;
using System.IO;

namespace SilverSim.Scene.ServiceInterfaces.Terrain
{
    public interface ITerrainFileStorage
    {
        string Name { get; }
        /* if a loader cannot guess the size unambiguously, it can use the suggested parameters */
        List<LayerPatch> LoadFile(string filename, int suggested_width, int suggested_height);
        List<LayerPatch> LoadStream(Stream input, int suggested_width, int suggested_height);
        void SaveFile(string filename, List<LayerPatch> terrain);
        void SaveStream(Stream output, List<LayerPatch> terrain);
        bool SupportsLoading { get; }
        bool SupportsSaving { get; }
    }
}
