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

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using System;
using SilverSim.Scene.Types.WindLight;

namespace SilverSim.Scripting.LSL.API.WindLight
{
    [ScriptApiName("WindLight")]
    [LSLImplementation]
    public class WindLight_API : MarshalByRefObject, IScriptApi, IPlugin
    {
        public WindLight_API()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_AMBIENT = 0;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_SKY_BLUE_DENSITY = 1;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_SKY_BLUR_HORIZON = 2;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_CLOUD_COLOR = 3;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_CLOUD_POS_DENSITY1 = 4;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_CLOUD_POS_DENSITY2 = 5;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_CLOUD_SCALE = 6;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_CLOUD_SCROLL_X = 7;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_CLOUD_SCROLL_Y = 8;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_CLOUD_SCROLL_X_LOCK = 9;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_CLOUD_SCROLL_Y_LOCK = 10;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_CLOUD_SHADOW = 11;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_SKY_DENSITY_MULTIPLIER = 12;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_SKY_DISTANCE_MULTIPLIER = 13;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_SKY_GAMMA = 14;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_SKY_GLOW = 15;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_SKY_HAZE_DENSITY = 16;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_SKY_HAZE_HORIZON = 17;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_SKY_LIGHT_NORMALS = 18;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_SKY_MAX_ALTITUDE = 19;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_SKY_STAR_BRIGHTNESS = 20;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_SKY_SUNLIGHT_COLOR = 21;

        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_WATER_BLUR_MULTIPLIER = 22;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_WATER_FRESNEL_OFFSET = 23;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_WATER_FRESNEL_SCALE = 24;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_WATER_NORMAL_MAP = 25;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_WATER_NORMAL_SCALE = 26;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_WATER_SCALE_ABOVE = 27;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_WATER_SCALE_BELOW = 28;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_WATER_UNDERWATER_FOG_MODIFIER = 29;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_WATER_FOG_COLOR = 30;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_WATER_FOG_DENSITY = 31;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_WATER_BIG_WAVE_DIRECTION = 32;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_WATER_LITTLE_WAVE_DIRECTION = 33;

        [APILevel(APIFlags.WindLight_New)]
        public AnArray rwlWindlightGetWaterSettings(ScriptInstance Instance, AnArray rules)
        {
            AnArray res = new AnArray();
            EnvironmentSettings envsettings;
            lock(Instance)
            {
                envsettings = Instance.Part.ObjectGroup.Scene.EnvironmentSettings;
            }

            foreach(IValue iv in rules)
            {
                if (!(iv is Integer))
                {
                    lock (Instance)
                    {
                        Instance.ShoutError(string.Format("Invalid parameter type {0}", iv.LSL_Type.ToString()));
                        return res;
                    }
                }

                switch(iv.AsInt)
                {
                    case REGION_WL_WATER_BLUR_MULTIPLIER:
                        res.Add(envsettings.WaterSettings.BlurMultiplier);
                        break;

                    case REGION_WL_WATER_FRESNEL_OFFSET:
                        res.Add(envsettings.WaterSettings.FresnelOffset);
                        break;

                    case REGION_WL_WATER_FRESNEL_SCALE:
                        res.Add(envsettings.WaterSettings.FresnelScale);
                        break;

                    case REGION_WL_WATER_NORMAL_MAP:
                        res.Add(envsettings.WaterSettings.NormalMap);
                        break;

                    case REGION_WL_WATER_UNDERWATER_FOG_MODIFIER:
                        res.Add(envsettings.WaterSettings.UnderwaterFogModifier);
                        break;

                    case REGION_WL_WATER_SCALE_ABOVE:
                        res.Add(envsettings.WaterSettings.ScaleAbove);
                        break;

                    case REGION_WL_WATER_SCALE_BELOW:
                        res.Add(envsettings.WaterSettings.ScaleBelow);
                        break;

                    case REGION_WL_WATER_FOG_DENSITY:
                        res.Add(envsettings.WaterSettings.WaterFogDensity);
                        break;

                    case REGION_WL_WATER_FOG_COLOR:
                        res.Add(new Quaternion(
                            envsettings.WaterSettings.WaterFogColor.X,
                            envsettings.WaterSettings.WaterFogColor.Y,
                            envsettings.WaterSettings.WaterFogColor.Z,
                            envsettings.WaterSettings.WaterFogColor.W));
                        break;

                    case REGION_WL_WATER_BIG_WAVE_DIRECTION:
                        res.Add(envsettings.WaterSettings.Wave1Direction);
                        break;

                    case REGION_WL_WATER_LITTLE_WAVE_DIRECTION:
                        res.Add(envsettings.WaterSettings.Wave2Direction);
                        break;

                    case REGION_WL_WATER_NORMAL_SCALE:
                        res.Add(envsettings.WaterSettings.NormScale);
                        break;

                    default:
                        Instance.ShoutError(string.Format("Invalid parameter type {0}", iv.LSL_Type.ToString()));
                        return res;
                }
            }
            return res;
        }
    }
}
