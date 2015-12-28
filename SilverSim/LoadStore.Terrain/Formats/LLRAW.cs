// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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

            public int CompareTo(HeightmapLookupValue val)
            {
                return Value.CompareTo(val.Value);
            }

            public static bool operator ==(HeightmapLookupValue a, HeightmapLookupValue b)
            {
                return a.Equals(b);
            }

            public static bool operator !=(HeightmapLookupValue a, HeightmapLookupValue b)
            {
                return !a.Equals(b);
            }

            public static bool operator >(HeightmapLookupValue a, HeightmapLookupValue b)
            {
                return a.Value > b.Value;
            }

            public static bool operator <(HeightmapLookupValue a, HeightmapLookupValue b)
            {
                return a.Value < b.Value;
            }

            [SuppressMessage("Gendarme.Rules.Correctness", "AvoidFloatingPointEqualityRule")]
            public override bool Equals(object obj)
            {
                if(obj is HeightmapLookupValue)
                {
                    return Value.Equals(((HeightmapLookupValue)obj).Value);
                }
                return false;
            }

            public override int GetHashCode()
            {
                return Value.GetHashCode();
            }

            [SuppressMessage("Gendarme.Rules.Correctness", "AvoidFloatingPointEqualityRule")]
            public bool Equals(HeightmapLookupValue v)
            {
                return Value.Equals(v.Value);
            }
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
            Array.Sort<HeightmapLookupValue>(LookupHeightTable);
        }

        public LLRAW()
        {

        }

        public string Name
        {
            get
            {
                return "llraw";
            }
        }

        public List<LayerPatch> LoadFile(string filename, int suggested_width, int suggested_height)
        {
            using (Stream input = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                return input.LoadLLRawStream(suggested_width, suggested_height);
            }
        }

        public List<LayerPatch> LoadStream(Stream input, int suggested_width, int suggested_height)
        {
            return input.LoadLLRawStream(suggested_width, suggested_height);
        }

        public void SaveFile(string filename, List<LayerPatch> terrain)
        {
            using (Stream output = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                SaveStream(output, terrain);
            }
        }

        public void SaveStream(Stream output, List<LayerPatch> terrain)
        {
            byte[] outdata = terrain.ToLLRaw();

            output.Write(outdata, 0, outdata.Length);
        }

        public bool SupportsLoading
        {
            get
            {
                return true;
            }
        }

        public bool SupportsSaving
        {
            get 
            {
                return true;
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
        }
    }
}
