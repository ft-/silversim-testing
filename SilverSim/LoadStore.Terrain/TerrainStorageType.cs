// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.LoadStore.Terrain
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    sealed class TerrainStorageType : Attribute
    {
        public TerrainStorageType()
        {

        }
    }
}
