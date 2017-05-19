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

using SilverSim.Main.Common;
using SilverSim.Scene.ServiceInterfaces.Terrain;
using SilverSim.Viewer.Messages.LayerData;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace SilverSim.LoadStore.Terrain.Formats
{
    [TerrainStorageType]
    [Description("LLRAW Terrain Format")]
    public class LLRAW : ITerrainFileStorage, IPlugin
    {
        public struct HeightmapLookupValue : IComparable<HeightmapLookupValue>, IEquatable<HeightmapLookupValue>
        {
            public readonly ushort Index;
            public readonly float Value;

            public HeightmapLookupValue(ushort index, float value)
            {
                Index = index;
                Value = value;
            }

            public int CompareTo(HeightmapLookupValue val) => Value.CompareTo(val.Value);

            public static bool operator ==(HeightmapLookupValue a, HeightmapLookupValue b) => a.Equals(b);

            public static bool operator !=(HeightmapLookupValue a, HeightmapLookupValue b) => !a.Equals(b);

            public static bool operator >(HeightmapLookupValue a, HeightmapLookupValue b) => a.Value > b.Value;

            public static bool operator <(HeightmapLookupValue a, HeightmapLookupValue b) => a.Value < b.Value;

            [SuppressMessage("Gendarme.Rules.Correctness", "AvoidFloatingPointEqualityRule")]
            public override bool Equals(object obj)
            {
                if(obj is HeightmapLookupValue)
                {
                    return Value.Equals(((HeightmapLookupValue)obj).Value);
                }
                return false;
            }

            public override int GetHashCode() => Value.GetHashCode();

            [SuppressMessage("Gendarme.Rules.Correctness", "AvoidFloatingPointEqualityRule")]
            public bool Equals(HeightmapLookupValue v) => Value.Equals(v.Value);
        }

        /// <summary>Lookup table to speed up terrain exports</summary>
        static readonly HeightmapLookupValue[] LookupHeightTable;

        static LLRAW()
        {
            LookupHeightTable = new HeightmapLookupValue[256 * 256];

            for (int i = 0; i < 256; i++)
            {
                for (int j = 0; j < 256; j++)
                {
                    LookupHeightTable[i + (j * 256)] = new HeightmapLookupValue((ushort)(i + (j * 256)), (float)((double)i * ((double)j / 128.0d)));
                }
            }
            Array.Sort(LookupHeightTable);
        }

        public string Name => "llraw";

        public List<LayerPatch> LoadFile(string filename, int suggested_width, int suggested_height)
        {
            using (var input = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                return input.LoadLLRawStream(suggested_width, suggested_height);
            }
        }

        public List<LayerPatch> LoadStream(Stream input, int suggested_width, int suggested_height) => 
            input.LoadLLRawStream(suggested_width, suggested_height);

        public void SaveFile(string filename, List<LayerPatch> terrain)
        {
            using (var output = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                SaveStream(output, terrain);
            }
        }

        public void SaveStream(Stream output, List<LayerPatch> terrain)
        {
            byte[] outdata = terrain.ToLLRaw();

            output.Write(outdata, 0, outdata.Length);
        }

        public bool SupportsLoading => true;

        public bool SupportsSaving => true;

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }
    }
}
