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

using SilverSim.Types.StructuredData.Llsd;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml;

namespace SilverSim.Types.Asset.Format
{
    public class Material : IReferencesAccessor
    {
        #region Fields
        public UUID MaterialID;
        public int AlphaMaskCutoff = 1;
        public int DiffuseAlphaMode;
        public int EnvIntensity;
        public UUID NormMap = UUID.Zero;
        public int NormOffsetX;
        public int NormOffsetY;
        public int NormRepeatX = 10000;
        public int NormRepeatY = 10000;
        public int NormRotation;
        public ColorAlpha SpecColor = ColorAlpha.White;
        public int SpecExp = DEFAULT_SPECULAR_LIGHT_EXPONENT;
        public UUID SpecMap = UUID.Zero;
        public int SpecOffsetX;
        public int SpecOffsetY;
        public int SpecRepeatX = 10000;
        public int SpecRepeatY = 10000;
        public int SpecRotation;
        #endregion

        [SuppressMessage("Gendarme.Rules.BadPractice", "AvoidVisibleConstantFieldRule")]
        public const double MATERIALS_MULTIPLIER = 10000f;
        [SuppressMessage("Gendarme.Rules.BadPractice", "AvoidVisibleConstantFieldRule")]
        public const byte DEFAULT_SPECULAR_LIGHT_EXPONENT = (byte)(0.2f * 255);

        #region Constructors
        public Material()
        {
            MaterialID = UUID.Random;
        }

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        public Material(UUID id, Map m)
        {
            MaterialID = id;

            AlphaMaskCutoff = m["AlphaMaskCutoff"].AsInt;
            DiffuseAlphaMode = m["DiffuseAlphaMode"].AsInt;
            EnvIntensity = m["EnvIntensity"].AsInt;
            NormMap = m["NormMap"].AsUUID;
            NormOffsetX = m["NormOffsetX"].AsInt;
            NormOffsetY = m["NormOffsetY"].AsInt;
            NormRepeatX = m["NormRepeatX"].AsInt;
            NormRepeatY = m["NormRepeatY"].AsInt;
            NormRotation = m["NormRotation"].AsInt;
            var a = (AnArray)m["SpecColor"];
            SpecColor.R_AsByte = (byte)a[0].AsUInt;
            SpecColor.G_AsByte = (byte)a[1].AsUInt;
            SpecColor.B_AsByte = (byte)a[2].AsUInt;
            SpecColor.A_AsByte = (byte)a[3].AsUInt;
            SpecExp = m["SpecExp"].AsInt;
            SpecMap = m["SpecMap"].AsUUID;
            SpecOffsetX = m["SpecOffsetX"].AsInt;
            SpecOffsetY = m["SpecOffsetY"].AsInt;
            SpecRepeatX = m["SpecRepeatX"].AsInt;
            SpecRepeatY = m["SpecRepeatY"].AsInt;
            SpecRotation = m["SpecRotation"].AsInt;
        }

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        public Material(AssetData asset)
        {
            var m = LlsdXml.Deserialize(asset.InputStream) as Map;
            if(m == null)
            {
                throw new NotAMaterialFormatException();
            }

            MaterialID = asset.ID;

            AlphaMaskCutoff = m["AlphaMaskCutoff"].AsInt;
            DiffuseAlphaMode = m["DiffuseAlphaMode"].AsInt;
            EnvIntensity = m["EnvIntensity"].AsInt;
            NormMap = m["NormMap"].AsUUID;
            NormOffsetX = m["NormOffsetX"].AsInt;
            NormOffsetY = m["NormOffsetY"].AsInt;
            NormRepeatX = m["NormRepeatX"].AsInt;
            NormRepeatY = m["NormRepeatY"].AsInt;
            NormRotation = m["NormRotation"].AsInt;
            var a = (AnArray)m["SpecColor"];
            SpecColor.R_AsByte = (byte)a[0].AsUInt;
            SpecColor.G_AsByte = (byte)a[1].AsUInt;
            SpecColor.B_AsByte = (byte)a[2].AsUInt;
            SpecColor.A_AsByte = (byte)a[3].AsUInt;
            SpecExp = m["SpecExp"].AsInt;
            SpecMap = m["SpecMap"].AsUUID;
            SpecOffsetX = m["SpecOffsetX"].AsInt;
            SpecOffsetY = m["SpecOffsetY"].AsInt;
            SpecRepeatX = m["SpecRepeatX"].AsInt;
            SpecRepeatY = m["SpecRepeatY"].AsInt;
            SpecRotation = m["SpecRotation"].AsInt;
        }

        #endregion

        #region References interface
        public List<UUID> References
        {
            get
            {
                var reflist = new List<UUID>();
                if(NormMap != UUID.Zero)
                {
                    reflist.Add(NormMap);
                }
                if (SpecMap != UUID.Zero)
                {
                    reflist.Add(SpecMap);
                }
                return reflist;
            }
        }
        #endregion

        #region Operators
        private static void WriteValue(XmlTextWriter w, byte val)
        {
            w.WriteStartElement("integer");
            w.WriteValue(val);
            w.WriteEndElement();
        }

        private static void WriteKey(XmlTextWriter w, string name, int val)
        {
            w.WriteStartElement("key");
            w.WriteValue(name);
            w.WriteEndElement();
            w.WriteStartElement("integer");
            w.WriteValue(val);
            w.WriteEndElement();
        }

        private static void WriteKey(XmlTextWriter w, string name, UUID val)
        {
            w.WriteStartElement("key");
            w.WriteValue(name);
            w.WriteEndElement();
            w.WriteStartElement("uuid");
            w.WriteValue(val.ToString());
            w.WriteEndElement();
        }

        public AssetData Asset() => this;

        public void WriteMap(XmlTextWriter w)
        {
            w.WriteStartElement("map");
            WriteKey(w, "AlphaMaskCutoff", AlphaMaskCutoff);
            WriteKey(w, "DiffuseAlphaMode", DiffuseAlphaMode);
            WriteKey(w, "EnvIntensity", EnvIntensity);
            WriteKey(w, "NormMap", NormMap);
            WriteKey(w, "NormOffsetX", NormOffsetX);
            WriteKey(w, "NormOffsetY", NormOffsetY);
            WriteKey(w, "NormRepeatX", NormRepeatX);
            WriteKey(w, "NormRepeatY", NormRepeatY);
            WriteKey(w, "NormRotation", NormRotation);
            w.WriteStartElement("key");
            w.WriteValue("SpecColor");
            w.WriteEndElement();
            w.WriteStartElement("array");
            WriteValue(w, SpecColor.R_AsByte);
            WriteValue(w, SpecColor.G_AsByte);
            WriteValue(w, SpecColor.B_AsByte);
            WriteValue(w, SpecColor.A_AsByte);
            w.WriteEndElement();
            WriteKey(w, "SpecExp", SpecExp);
            WriteKey(w, "SpecMap", SpecMap);
            WriteKey(w, "SpecOffsetX", SpecOffsetX);
            WriteKey(w, "SpecOffsetY", SpecOffsetY);
            WriteKey(w, "SpecRepeatX", SpecRepeatX);
            WriteKey(w, "SpecRepeatY", SpecRepeatY);
            WriteKey(w, "SpecRotation", SpecRotation);
            w.WriteEndElement();
        }

        public Map WriteMap()
        {
            var w = new Map
            {
                { "AlphaMaskCutoff", AlphaMaskCutoff },
                { "DiffuseAlphaMode", DiffuseAlphaMode },
                { "EnvIntensity", EnvIntensity },
                { "NormMap", NormMap },
                { "NormOffsetX", NormOffsetX },
                { "NormOffsetY", NormOffsetY },
                { "NormRepeatX", NormRepeatX },
                { "NormRepeatY", NormRepeatY },
                { "NormRotation", NormRotation }
            };
            var spec = new AnArray
            {
                SpecColor.R_AsByte,
                SpecColor.G_AsByte,
                SpecColor.B_AsByte,
                SpecColor.A_AsByte
            };
            w.Add("SpecColor", spec);
            w.Add("SpecExp", SpecExp);
            w.Add("SpecMap", SpecMap);
            w.Add("SpecOffsetX", SpecOffsetX);
            w.Add("SpecOffsetY", SpecOffsetY);
            w.Add("SpecRepeatX", SpecRepeatX);
            w.Add("SpecRepeatY", SpecRepeatY);
            w.Add("SpecRotation", SpecRotation);
            return w;
        }

        public static implicit operator AssetData(Material v)
        {
            var asset = new AssetData()
            {
                ID = v.MaterialID
            };
            using (var ms = new MemoryStream())
            {
                using (var w = ms.UTF8XmlTextWriter())
                {
                    w.WriteStartElement("llsd");
                    v.WriteMap(w);
                    w.WriteEndElement();
                    w.Flush();
                }

                asset.Data = ms.ToArray();
            }
            asset.Type = AssetType.Material;
            asset.Name = "Material";
            return asset;
        }
        #endregion
    }
}
