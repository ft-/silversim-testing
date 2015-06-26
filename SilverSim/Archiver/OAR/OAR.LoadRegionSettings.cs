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

using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using System.IO;
using System.Xml;

namespace SilverSim.Archiver.OAR
{
    public static partial class OAR
    {
        static class RegionSettingsLoader
        {
            static void LoadRegionSettingsGeneral(XmlTextReader reader, SceneInterface scene)
            {
                if(reader.IsEmptyElement)
                {
                    return;
                }

                for(;;)
                {
                    if(!reader.Read())
                    {
                        throw new OARFormatException();
                    }

                    switch(reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            switch(reader.Name)
                            {
                                case "AllowDamage":
                                    scene.RegionSettings.AllowDamage = reader.ReadElementValueAsBoolean();
                                    break;

                                case "AllowLandResell":
                                    scene.RegionSettings.AllowLandResell = reader.ReadElementValueAsBoolean();
                                    break;

                                case "AllowLandJoinDivide":
                                    scene.RegionSettings.AllowLandJoinDivide = reader.ReadElementValueAsBoolean();
                                    break;

                                case "BlockFly":
                                    scene.RegionSettings.BlockFly = reader.ReadElementValueAsBoolean();
                                    break;

                                case "BlockLandShowInSearch":
                                    scene.RegionSettings.BlockShowInSearch = reader.ReadElementValueAsBoolean();
                                    break;

                                case "BlockTerraform":
                                    scene.RegionSettings.BlockTerraform = reader.ReadElementValueAsBoolean();
                                    break;

                                case "DisableCollisions":
                                    scene.RegionSettings.DisableCollisions = reader.ReadElementValueAsBoolean();
                                    break;

                                case "DisablePhysics":
                                    scene.RegionSettings.DisablePhysics = reader.ReadElementValueAsBoolean();
                                    break;

                                case "DisableScripts":
                                    scene.RegionSettings.DisableScripts = reader.ReadElementValueAsBoolean();
                                    break;

                                case "MaturityRating":
                                    scene.RegionSettings.Maturity = reader.ReadElementValueAsInt();
                                    break;

                                case "RestrictPushing":
                                    scene.RegionSettings.RestrictPushing = reader.ReadElementValueAsBoolean();
                                    break;

                                case "AgentLimit":
                                    scene.RegionSettings.AgentLimit = reader.ReadElementValueAsInt();
                                    break;

                                case "ObjectBonus":
                                    scene.RegionSettings.ObjectBonus = reader.ReadElementValueAsDouble();
                                    break;

                                default:
                                    if(!reader.IsEmptyElement)
                                    {
                                        reader.Skip();
                                    }
                                    break;
                            }
                            break;

                        case XmlNodeType.EndElement:
                            if(reader.Name != "General")
                            {
                                throw new OARFormatException();
                            }
                            return;
                    }
                }
            }

            static void LoadRegionSettingsGroundTextures(XmlTextReader reader, SceneInterface scene)
            {
                if (reader.IsEmptyElement)
                {
                    return;
                }

                for (; ; )
                {
                    if (!reader.Read())
                    {
                        throw new OARFormatException();
                    }

                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            switch (reader.Name)
                            {
                                case "Texture1":
                                    scene.RegionSettings.TerrainTexture1 = reader.ReadElementValueAsString();
                                    break;

                                case "Texture2":
                                    scene.RegionSettings.TerrainTexture2 = reader.ReadElementValueAsString();
                                    break;

                                case "Texture3":
                                    scene.RegionSettings.TerrainTexture3 = reader.ReadElementValueAsString();
                                    break;

                                case "Texture4":
                                    scene.RegionSettings.TerrainTexture4 = reader.ReadElementValueAsString();
                                    break;

                                case "ElevationLowSW":
                                    scene.RegionSettings.Elevation1SW = reader.ReadElementValueAsDouble();
                                    break;

                                case "ElevationLowNW":
                                    scene.RegionSettings.Elevation1NW = reader.ReadElementValueAsDouble();
                                    break;

                                case "ElevationLowSE":
                                    scene.RegionSettings.Elevation1SE = reader.ReadElementValueAsDouble();
                                    break;

                                case "ElevationLowNE":
                                    scene.RegionSettings.Elevation1NE = reader.ReadElementValueAsDouble();
                                    break;

                                case "ElevationHighSW":
                                    scene.RegionSettings.Elevation2SW = reader.ReadElementValueAsDouble();
                                    break;

                                case "ElevationHighNW":
                                    scene.RegionSettings.Elevation2NW = reader.ReadElementValueAsDouble();
                                    break;

                                case "ElevationHighSE":
                                    scene.RegionSettings.Elevation2SE = reader.ReadElementValueAsDouble();
                                    break;

                                case "ElevationHighNE":
                                    scene.RegionSettings.Elevation2NE = reader.ReadElementValueAsDouble();
                                    break;

                                default:
                                    if (!reader.IsEmptyElement)
                                    {
                                        reader.Skip();
                                    }
                                    break;
                            }
                            break;

                        case XmlNodeType.EndElement:
                            if (reader.Name != "GroundTextures")
                            {
                                throw new OARFormatException();
                            }
                            return;
                    }
                }
            }

            static void LoadRegionSettingsTerrain(XmlTextReader reader, SceneInterface scene)
            {
                if (reader.IsEmptyElement)
                {
                    return;
                }


                for (; ; )
                {
                    if (!reader.Read())
                    {
                        throw new OARFormatException();
                    }

                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            switch (reader.Name)
                            {
                                case "WaterHeight":
                                    scene.RegionSettings.WaterHeight = reader.ReadElementValueAsDouble();
                                    break;

                                case "TerrainRaiseLimit":
                                    scene.RegionSettings.TerrainRaiseLimit = reader.ReadElementValueAsDouble();
                                    break;

                                case "TerrainLowerLimit":
                                    scene.RegionSettings.TerrainLowerLimit = reader.ReadElementValueAsDouble();
                                    break;

                                case "UseEstateSun":
                                    reader.ReadElementValueAsBoolean();
                                    break;

                                case "FixedSun":
                                    reader.ReadElementValueAsBoolean();
                                    break;

                                case "SunPosition":
                                    reader.ReadElementValueAsDouble();
                                    break;

                                default:
                                    if (!reader.IsEmptyElement)
                                    {
                                        reader.Skip();
                                    }
                                    break;
                            }
                            break;

                        case XmlNodeType.EndElement:
                            if (reader.Name != "Terrain")
                            {
                                throw new OARFormatException();
                            }
                            return;
                    }
                }
            }

            static void LoadRegionSettingsTelehub(XmlTextReader reader, SceneInterface scene)
            {
                if (reader.IsEmptyElement)
                {
                    return;
                }
                reader.Skip();
            }

            static void LoadRegionSettingsInner(XmlTextReader reader, SceneInterface scene)
            {
                for(;;)
                {
                    if(!reader.Read())
                    {
                        throw new OARFormatException();
                    }

                    switch(reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            switch(reader.Name)
                            {
                                case "General":
                                    LoadRegionSettingsGeneral(reader, scene);
                                    break;

                                case "GroundTextures":
                                    LoadRegionSettingsGroundTextures(reader, scene);
                                    break;

                                case "Terrain":
                                    LoadRegionSettingsTerrain(reader, scene);
                                    break;

                                case "Telehub":
                                    LoadRegionSettingsTelehub(reader, scene);
                                    break;
                            }
                            break;

                        case XmlNodeType.EndElement:
                            if (reader.Name != "RegionSettings")
                            {
                                throw new OARFormatException();
                            }
                            return;
                    }
                }
            }

            static void LoadRegionSettings(XmlTextReader reader, SceneInterface scene)
            {
                for(;;)
                {
                    if(!reader.Read())
                    {
                        throw new OARFormatException();
                    }

                    switch(reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if(reader.Name == "RegionSettings")
                            {
                                if(reader.IsEmptyElement)
                                {
                                    throw new OARFormatException();
                                }
                                LoadRegionSettingsInner(reader, scene);
                                return;
                            }
                            else
                            {
                                throw new OARFormatException();
                            }
                    }
                }
            }

            public static void LoadRegionSettings(Stream s, SceneInterface scene)
            {
                using(XmlTextReader reader = new XmlTextReader(s))
                {
                    LoadRegionSettings(s, scene);
                }
            }
        }
    }
}
