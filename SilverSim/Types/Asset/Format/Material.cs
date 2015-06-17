/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.StructuredData.LLSD;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace SilverSim.Types.Asset.Format
{
    public class Material : IReferencesAccessor
    {
        #region Fields
        public UUID MaterialID;
        public int AlphaMaskCutoff = 1;
        public int DiffuseAlphaMode = 0;
        public int EnvIntensity = 0;
        public UUID NormMap = UUID.Zero;
        public int NormOffsetX = 0;
        public int NormOffsetY = 0;
        public int NormRepeatX = 0;
        public int NormRepeatY = 0;
        public int NormRotation = 0;
        public ColorAlpha SpecColor = new ColorAlpha();
        public int SpecExp = 0;
        public UUID SpecMap = UUID.Zero;
        public int SpecOffsetX = 0;
        public int SpecOffsetY = 0;
        public int SpecRepeatX = 0;
        public int SpecRepeatY = 0;
        public int SpecRotation = 0;
        #endregion

        #region Constructors
        public Material()
        {
            MaterialID = UUID.Random;
        }

        public Material(AssetData asset)
        {
            IValue v = LLSD_XML.Deserialize(asset.InputStream);
            if(!(v is Map))
            {
                throw new NotAMaterialFormat();
            }
            Map m = (Map)v;

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
            AnArray a = (AnArray)m["SpecColor"];
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
                List<UUID> reflist = new List<UUID>();
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

        public AssetData Asset()
        {
            return (AssetData)this;
        }

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

        public static implicit operator AssetData(Material v)
        {
            AssetData asset = new AssetData();
            asset.ID = v.MaterialID;
            MemoryStream ms = new MemoryStream();

            using(XmlTextWriter w = new XmlTextWriter(ms, UTF8NoBOM))
            {
                w.WriteStartElement("llsd");
                v.WriteMap(w);
                w.WriteEndElement();
                w.Flush();
            }

            asset.Data = ms.ToArray();
            asset.Type = AssetType.Material;
            asset.Name = "Material";
            return asset;
        }
        #endregion

        private static Encoding UTF8NoBOM = new System.Text.UTF8Encoding(false);
    }
}
