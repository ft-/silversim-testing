// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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

        public UUID getTextureAssetID(ScriptInstance Instance, string item)
        {
            UUID assetID;
            if (!UUID.TryParse(item, out assetID))
            {
                /* must be an inventory item */
                lock (Instance)
                {
                    ObjectPartInventoryItem i = Instance.Part.Inventory[item];
                    if (i.InventoryType != Types.Inventory.InventoryType.Texture)
                    {
                        throw new InvalidOperationException(string.Format("Inventory item {0} is not a texture", item));
                    }
                    assetID = i.AssetID;
                }
            }
            return assetID;
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

            if(envsettings == null)
            {
                return res;
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
                        Instance.ShoutError(string.Format("Invalid parameter type {0}", iv.AsInt));
                        return res;
                }
            }
            return res;
        }

        [APILevel(APIFlags.WindLight_New)]
        public int rwlWindlightSetWaterSettings(ScriptInstance Instance, AnArray rules)
        {
            AnArray res = new AnArray();
            EnvironmentSettings envsettings;
            lock (Instance)
            {
                envsettings = Instance.Part.ObjectGroup.Scene.EnvironmentSettings;
                if(null == envsettings)
                {
                    envsettings = new EnvironmentSettings();
                }
            }

            if(rules.Count % 2 != 0)
            {
                return 0;
            }

            WaterEntry waterSettings = envsettings.WaterSettings;

            for (int paraidx = 0; paraidx < rules.Count; paraidx += 2 )
            {
                IValue ivtype = rules[paraidx];
                IValue ivvalue = rules[paraidx + 1];
                if (!(ivtype is Integer))
                {
                    lock (Instance)
                    {
                        Instance.ShoutError(string.Format("Invalid parameter type {0}", ivtype.LSL_Type.ToString()));
                        return 0;
                    }
                }

                switch (ivtype.AsInt)
                {
                    case REGION_WL_WATER_BLUR_MULTIPLIER:
                        if(!(ivvalue is Real))
                        {
                            Instance.ShoutError(string.Format("Invalid parameter type {0} for REGION_WL_WATER_BLUR_MODIFIER", ivvalue.LSL_Type.ToString()));
                            return 0;
                        }
                        waterSettings.BlurMultiplier = ivvalue.AsReal;
                        break;

                    case REGION_WL_WATER_FRESNEL_OFFSET:
                        if(!(ivvalue is Real))
                        {
                            Instance.ShoutError(string.Format("Invalid parameter type {0} for REGION_WL_WATER_FRESNEL_OFFSET", ivvalue.LSL_Type.ToString()));
                            return 0;
                        }
                        waterSettings.FresnelOffset = ivvalue.AsReal;
                        break;

                    case REGION_WL_WATER_FRESNEL_SCALE:
                        if(!(ivvalue is Real))
                        {
                            Instance.ShoutError(string.Format("Invalid parameter type {0} for REGION_WL_WATER_FRESNEL_SCALE", ivvalue.LSL_Type.ToString()));
                            return 0;
                        }
                        waterSettings.FresnelScale = ivvalue.AsReal;
                        break;

                    case REGION_WL_WATER_NORMAL_MAP:
                        lock (Instance)
                        {
                            try
                            {
                                waterSettings.NormalMap = getTextureAssetID(Instance, ivvalue.ToString());
                            }
                            catch(Exception e)
                            {
                                Instance.ShoutError(e.Message);
                                return 0;
                            }
                        }
                        break;

                    case REGION_WL_WATER_UNDERWATER_FOG_MODIFIER:
                        if(!(ivvalue is Real))
                        {
                            Instance.ShoutError(string.Format("Invalid parameter type {0} for REGION_WL_WATER_UNDERWATER_FOG_MODIFIER", ivvalue.LSL_Type.ToString()));
                            return 0;
                        }
                        waterSettings.UnderwaterFogModifier = ivvalue.AsReal;
                        break;

                    case REGION_WL_WATER_SCALE_ABOVE:
                        if(!(ivvalue is Real))
                        {
                            Instance.ShoutError(string.Format("Invalid parameter type {0} for REGION_WL_WATER_SCALE_ABOVE", ivvalue.LSL_Type.ToString()));
                            return 0;
                        }
                        waterSettings.ScaleAbove = ivvalue.AsReal;
                        break;

                    case REGION_WL_WATER_SCALE_BELOW:
                        if(!(ivvalue is Real))
                        {
                            Instance.ShoutError(string.Format("Invalid parameter type {0} for REGION_WL_WATER_SCALE_BELOW", ivvalue.LSL_Type.ToString()));
                            return 0;
                        }
                        waterSettings.ScaleBelow = ivvalue.AsReal;
                        break;

                    case REGION_WL_WATER_FOG_DENSITY:
                        if(!(ivvalue is Real))
                        {
                            Instance.ShoutError(string.Format("Invalid parameter type {0} for REGION_WL_WATER_FOG_DENSITY", ivvalue.LSL_Type.ToString()));
                            return 0;
                        }
                        waterSettings.WaterFogDensity = ivvalue.AsReal;
                        break;

                    case REGION_WL_WATER_FOG_COLOR:
                        if(!(ivvalue is Quaternion))
                        {
                            Instance.ShoutError(string.Format("Invalid parameter type {0} for REGION_WL_WATER_FOG_COLOR", ivvalue.LSL_Type.ToString()));
                            return 0;
                        }

                        {
                            Quaternion q = ivvalue.AsQuaternion;
                            waterSettings.WaterFogColor = new Vector4(q.X.Clamp(0, 1), q.Y.Clamp(0, 1), q.Z.Clamp(0,1), q.W.Clamp(0, 1));
                        }
                        break;

                    case REGION_WL_WATER_BIG_WAVE_DIRECTION:
                        if(!(ivvalue is Vector3))
                        {
                            Instance.ShoutError(string.Format("Invalid parameter type {0} for REGION_WL_WATER_BIG_WAVE_DIRECTION", ivvalue.LSL_Type.ToString()));
                            return 0;
                        }
                        waterSettings.Wave1Direction = ivvalue.AsVector3;
                        break;

                    case REGION_WL_WATER_LITTLE_WAVE_DIRECTION:
                        if(!(ivvalue is Vector3))
                        {
                            Instance.ShoutError(string.Format("Invalid parameter type {0} for REGION_WL_WATER_LITTLE_WAVE_DIRECTION", ivvalue.LSL_Type.ToString()));
                            return 0;
                        }
                        waterSettings.Wave2Direction = ivvalue.AsVector3;
                        break;

                    case REGION_WL_WATER_NORMAL_SCALE:
                        if(!(ivvalue is Vector3))
                        {
                            Instance.ShoutError(string.Format("Invalid parameter type {0} for REGION_WL_WATER_NORMAL_SCALE", ivvalue.LSL_Type.ToString()));
                            return 0;
                        }
                        waterSettings.NormScale = ivvalue.AsVector3;
                        break;

                    default:
                        Instance.ShoutError(string.Format("Invalid parameter type {0}", ivtype.AsInt));
                        return 0;
                }
            }

            envsettings.WaterSettings = waterSettings;
            lock(Instance)
            {
                Instance.Part.ObjectGroup.Scene.EnvironmentSettings = envsettings;
            }
            return 1;
        }
    }
}
