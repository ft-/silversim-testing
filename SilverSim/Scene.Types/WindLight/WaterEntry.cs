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

using SilverSim.Types;
using System.Xml;

namespace SilverSim.Scene.Types.WindLight
{
    public struct WaterEntry
    {
        public double BlurMultiplier;
        public double FresnelOffset;
        public double FresnelScale;
        public Vector3 NormScale;
        public UUID NormalMap;
        public double ScaleAbove;
        public double ScaleBelow;
        public double UnderwaterFogModifier;
        public Vector4 WaterFogColor;
        public double WaterFogDensity;
        public Vector3 Wave1Direction;
        public Vector3 Wave2Direction;

        public WaterEntry(Map m)
        {
            BlurMultiplier = m["blurMultiplier"].AsReal;
            FresnelOffset = m["fresnelOffset"].AsReal;
            FresnelScale = m["fresnelScale"].AsReal;
            var a = (AnArray)m["normScale"];
            NormScale = a.ElementsToVector3;
            NormalMap = m["normalMap"].AsUUID;
            ScaleAbove = m["scaleAbove"].AsReal;
            ScaleBelow = m["scaleBelow"].AsReal;
            UnderwaterFogModifier = m["underWaterFogMod"].AsReal;
            a = (AnArray)m["waterFogColor"];
            WaterFogColor = a.ElementsToVector4;
            WaterFogDensity = m["waterFogDensity"].AsReal;
            a = (AnArray)m["wave1Dir"];
            Wave1Direction = new Vector3(a[0].AsReal, a[1].AsReal, 0f);
            a = (AnArray)m["wave2Dir"];
            Wave2Direction = new Vector3(a[0].AsReal, a[1].AsReal, 0f);
        }

        public void Serialize(XmlTextWriter writer)
        {
            writer.WriteStartElement("map");
            {
                writer.WriteNamedValue("key", "blurMultiplier");
                writer.WriteNamedValue("real", BlurMultiplier);

                writer.WriteNamedValue("key", "fresnelOffset");
                writer.WriteNamedValue("real", FresnelOffset);

                writer.WriteNamedValue("key", "fresnelScale");
                writer.WriteNamedValue("real", FresnelScale);

                writer.WriteNamedValue("key", "normScale");
                writer.WriteStartElement("array");
                {
                    writer.WriteNamedValue("real", NormScale.X);
                    writer.WriteNamedValue("real", NormScale.Y);
                    writer.WriteNamedValue("real", NormScale.Z);
                }
                writer.WriteEndElement();

                writer.WriteNamedValue("key", "normalMap");
                writer.WriteNamedValue("uuid", NormalMap);

                writer.WriteNamedValue("key", "scaleAbove");
                writer.WriteNamedValue("real", ScaleAbove);

                writer.WriteNamedValue("key", "scaleBelow");
                writer.WriteNamedValue("real", ScaleBelow);

                writer.WriteNamedValue("key", "underWaterFogMod");
                writer.WriteNamedValue("real", UnderwaterFogModifier);

                writer.WriteNamedValue("key", "waterFogColor");
                writer.WriteStartElement("array");
                {
                    writer.WriteNamedValue("real", WaterFogColor.X);
                    writer.WriteNamedValue("real", WaterFogColor.Y);
                    writer.WriteNamedValue("real", WaterFogColor.Z);
                    writer.WriteNamedValue("real", WaterFogColor.W);
                }
                writer.WriteEndElement();

                writer.WriteNamedValue("key", "waterFogDensity");
                writer.WriteNamedValue("real", WaterFogDensity);

                writer.WriteNamedValue("key", "wave1Dir");
                writer.WriteStartElement("array");
                {
                    writer.WriteNamedValue("real", Wave1Direction.X);
                    writer.WriteNamedValue("real", Wave1Direction.Y);
                }
                writer.WriteEndElement();

                writer.WriteNamedValue("key", "wave2Dir");
                writer.WriteStartElement("array");
                {
                    writer.WriteNamedValue("real", Wave2Direction.X);
                    writer.WriteNamedValue("real", Wave2Direction.Y);
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
    }
}
