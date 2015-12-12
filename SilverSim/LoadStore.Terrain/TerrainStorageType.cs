// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.LoadStore.Terrain
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    sealed class TerrainStorageTypeAttribute : Attribute
    {
        public TerrainStorageTypeAttribute()
        {

        }
    }
}
