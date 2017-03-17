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
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace SilverSim.Scene.Types.WindLight
{
    [SuppressMessage("Gendarme.Rules.Performance", "AvoidLargeStructureRule")]
    public struct SkyEntry
    {
        public Vector4 Ambient;
        public Vector4 BlueDensity;
        public Vector4 BlueHorizon;
        public Vector4 CloudColor;
        public Vector4 CloudPosDensity1;
        public Vector4 CloudPosDensity2;
        public Vector4 CloudScale;
        public double CloudScrollRateX;
        public double CloudScrollRateY;
        public Vector4 CloudShadow;
        public Vector4 DensityMultiplier;
        public Vector4 DistanceMultiplier;
        public double EastAngle;
        public bool EnableCloudScrollX;
        public bool EnableCloudScrollY;
        public Vector4 Gamma;
        public Vector4 SunGlow;
        public Vector4 HazeDensity;
        public Vector4 HazeHorizon;
        public Vector4 LightNorm;
        public Vector4 MaxY;
        public int PresetNum;
        public double StarBrightness;
        public double SunAngle;
        public Vector4 SunlightColor;

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        public SkyEntry(Map m)
        {
            AnArray a;

            a = (AnArray)m["ambient"];
            Ambient = a.ElementsToVector4;
            a = (AnArray)m["blue_density"];
            BlueDensity = a.ElementsToVector4;
            a = (AnArray)m["blue_horizon"];
            BlueHorizon = a.ElementsToVector4;
            a = (AnArray)m["cloud_color"];
            CloudColor = a.ElementsToVector4;
            a = (AnArray)m["cloud_pos_density1"];
            CloudPosDensity1 = a.ElementsToVector4;
            a = (AnArray)m["cloud_pos_density2"];
            CloudPosDensity2 = a.ElementsToVector4;
            a = (AnArray)m["cloud_scale"];
            CloudScale = a.ElementsToVector4;
            a = (AnArray)m["cloud_scroll_rate"];
            CloudScrollRateX = a[0].AsReal;
            CloudScrollRateY = a[1].AsReal;
            a = (AnArray)m["cloud_shadow"];
            CloudShadow = a.ElementsToVector4;
            a = (AnArray)m["density_multiplier"];
            DensityMultiplier = a.ElementsToVector4;
            a = (AnArray)m["distance_multiplier"];
            DistanceMultiplier = a.ElementsToVector4;
            EastAngle = m["east_angle"].AsReal;
            a = (AnArray)m["enable_cloud_scroll"];
            EnableCloudScrollX = a[0].AsBoolean;
            EnableCloudScrollY = a[1].AsBoolean;
            a = (AnArray)m["gamma"];
            Gamma = a.ElementsToVector4;
            a = (AnArray)m["glow"];
            SunGlow = a.ElementsToVector4;
            a = (AnArray)m["haze_density"];
            HazeDensity = a.ElementsToVector4;
            a = (AnArray)m["haze_horizon"];
            HazeHorizon = a.ElementsToVector4;
            a = (AnArray)m["lightnorm"];
            LightNorm = a.ElementsToVector4;
            a = (AnArray)m["max_y"];
            MaxY = a.ElementsToVector4;
            PresetNum = m["preset_num"].AsInt;
            StarBrightness = m["star_brightness"].AsReal;
            SunAngle = m["sun_angle"].AsReal;
            a = (AnArray)m["sunlight_color"];
            SunlightColor = a.ElementsToVector4;
        }

        public void Serialize(XmlTextWriter writer)
        {
            writer.WriteStartElement("map");
            {
                writer.WriteNamedValue("key", "ambient");
                writer.WriteStartElement("array");
                {
                    writer.WriteNamedValue("real", Ambient.X);
                    writer.WriteNamedValue("real", Ambient.Y);
                    writer.WriteNamedValue("real", Ambient.Z);
                    writer.WriteNamedValue("real", Ambient.W);
                }
                writer.WriteEndElement();
                
                writer.WriteNamedValue("key", "blue_density");
                writer.WriteStartElement("array");
                {
                    writer.WriteNamedValue("real", BlueDensity.X);
                    writer.WriteNamedValue("real", BlueDensity.Y);
                    writer.WriteNamedValue("real", BlueDensity.Z);
                    writer.WriteNamedValue("real", BlueDensity.W);
                }
                writer.WriteEndElement();

                writer.WriteNamedValue("key", "blue_horizon");
                writer.WriteStartElement("array");
                {
                    writer.WriteNamedValue("real", BlueHorizon.X);
                    writer.WriteNamedValue("real", BlueHorizon.Y);
                    writer.WriteNamedValue("real", BlueHorizon.Z);
                    writer.WriteNamedValue("real", BlueHorizon.W);
                }
                writer.WriteEndElement();

                writer.WriteNamedValue("key", "cloud_color");
                writer.WriteStartElement("array");
                {
                    writer.WriteNamedValue("real", CloudColor.X);
                    writer.WriteNamedValue("real", CloudColor.Y);
                    writer.WriteNamedValue("real", CloudColor.Z);
                    writer.WriteNamedValue("real", CloudColor.W);
                }
                writer.WriteEndElement();

                writer.WriteNamedValue("key", "cloud_pos_density1");
                writer.WriteStartElement("array");
                {
                    writer.WriteNamedValue("real", CloudPosDensity1.X);
                    writer.WriteNamedValue("real", CloudPosDensity1.Y);
                    writer.WriteNamedValue("real", CloudPosDensity1.Z);
                    writer.WriteNamedValue("real", CloudPosDensity1.W);
                }
                writer.WriteEndElement();

                writer.WriteNamedValue("key", "cloud_pos_density2");
                writer.WriteStartElement("array");
                {
                    writer.WriteNamedValue("real", CloudPosDensity2.X);
                    writer.WriteNamedValue("real", CloudPosDensity2.Y);
                    writer.WriteNamedValue("real", CloudPosDensity2.Z);
                    writer.WriteNamedValue("real", CloudPosDensity2.W);
                }
                writer.WriteEndElement();

                writer.WriteNamedValue("key", "cloud_scale");
                writer.WriteStartElement("array");
                {
                    writer.WriteNamedValue("real", CloudScale.X);
                    writer.WriteNamedValue("real", CloudScale.Y);
                    writer.WriteNamedValue("real", CloudScale.Z);
                    writer.WriteNamedValue("real", CloudScale.W);
                }
                writer.WriteEndElement();

                writer.WriteNamedValue("key", "cloud_scroll_rate");
                writer.WriteStartElement("array");
                {
                    writer.WriteNamedValue("real", CloudScrollRateX);
                    writer.WriteNamedValue("real", CloudScrollRateY);
                }
                writer.WriteEndElement();

                writer.WriteNamedValue("key", "cloud_shadow");
                writer.WriteStartElement("array");
                {
                    writer.WriteNamedValue("real", CloudShadow.X);
                    writer.WriteNamedValue("real", CloudShadow.Y);
                    writer.WriteNamedValue("real", CloudShadow.Z);
                    writer.WriteNamedValue("real", CloudShadow.W);
                }
                writer.WriteEndElement();

                writer.WriteNamedValue("key", "density_multiplier");
                writer.WriteStartElement("array");
                {
                    writer.WriteNamedValue("real", DensityMultiplier.X);
                    writer.WriteNamedValue("real", DensityMultiplier.Y);
                    writer.WriteNamedValue("real", DensityMultiplier.Z);
                    writer.WriteNamedValue("real", DensityMultiplier.W);
                }
                writer.WriteEndElement();

                writer.WriteNamedValue("key", "distance_multiplier");
                writer.WriteStartElement("array");
                {
                    writer.WriteNamedValue("real", DistanceMultiplier.X);
                    writer.WriteNamedValue("real", DistanceMultiplier.Y);
                    writer.WriteNamedValue("real", DistanceMultiplier.Z);
                    writer.WriteNamedValue("real", DistanceMultiplier.W);
                }
                writer.WriteEndElement();

                writer.WriteNamedValue("key", "east_angle");
                writer.WriteNamedValue("real", EastAngle);

                writer.WriteNamedValue("key", "enable_cloud_scroll");
                writer.WriteStartElement("array");
                {
                    writer.WriteNamedValue("boolean", EnableCloudScrollX ? "1" : "0");
                    writer.WriteNamedValue("boolean", EnableCloudScrollY ? "1" : "0");
                }
                writer.WriteEndElement();

                writer.WriteNamedValue("key", "gamma");
                writer.WriteStartElement("array");
                {
                    writer.WriteNamedValue("real", Gamma.X);
                    writer.WriteNamedValue("real", Gamma.Y);
                    writer.WriteNamedValue("real", Gamma.Z);
                    writer.WriteNamedValue("real", Gamma.W);
                }
                writer.WriteEndElement();

                writer.WriteNamedValue("key", "glow");
                writer.WriteStartElement("array");
                {
                    writer.WriteNamedValue("real", SunGlow.X);
                    writer.WriteNamedValue("real", SunGlow.Y);
                    writer.WriteNamedValue("real", SunGlow.Z);
                    writer.WriteNamedValue("real", SunGlow.W);
                }
                writer.WriteEndElement();

                writer.WriteNamedValue("key", "haze_density");
                writer.WriteStartElement("array");
                {
                    writer.WriteNamedValue("real", HazeDensity.X);
                    writer.WriteNamedValue("real", HazeDensity.Y);
                    writer.WriteNamedValue("real", HazeDensity.Z);
                    writer.WriteNamedValue("real", HazeDensity.W);
                }
                writer.WriteEndElement();

                writer.WriteNamedValue("key", "haze_horizon");
                writer.WriteStartElement("array");
                {
                    writer.WriteNamedValue("real", HazeHorizon.X);
                    writer.WriteNamedValue("real", HazeHorizon.Y);
                    writer.WriteNamedValue("real", HazeHorizon.Z);
                    writer.WriteNamedValue("real", HazeHorizon.W);
                }
                writer.WriteEndElement();

                writer.WriteNamedValue("key", "lightnorm");
                writer.WriteStartElement("array");
                {
                    writer.WriteNamedValue("real", LightNorm.X);
                    writer.WriteNamedValue("real", LightNorm.Y);
                    writer.WriteNamedValue("real", LightNorm.Z);
                    writer.WriteNamedValue("real", LightNorm.W);
                }
                writer.WriteEndElement();

                writer.WriteNamedValue("key", "max_y");
                writer.WriteStartElement("array");
                {
                    writer.WriteNamedValue("real", MaxY.X);
                    writer.WriteNamedValue("real", MaxY.Y);
                    writer.WriteNamedValue("real", MaxY.Z);
                    writer.WriteNamedValue("real", MaxY.W);
                }
                writer.WriteEndElement();

                writer.WriteNamedValue("key", "preset_num");
                writer.WriteNamedValue("integer", PresetNum);

                writer.WriteNamedValue("key", "star_brightness");
                writer.WriteNamedValue("real", StarBrightness);

                writer.WriteNamedValue("key", "sun_angle");
                writer.WriteNamedValue("real", SunAngle);

                writer.WriteNamedValue("key", "sunlight_color");
                writer.WriteStartElement("array");
                {
                    writer.WriteNamedValue("real", SunlightColor.X);
                    writer.WriteNamedValue("real", SunlightColor.Y);
                    writer.WriteNamedValue("real", SunlightColor.Z);
                    writer.WriteNamedValue("real", SunlightColor.W);
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
    }
}
