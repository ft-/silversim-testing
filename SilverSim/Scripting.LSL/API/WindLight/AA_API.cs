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

namespace SilverSim.Scripting.LSL.API.WindLight
{
    [ScriptApiName("WindLight_Aurora")]
    [LSLImplementation]
    public class AA_API : MarshalByRefObject, IScriptApi, IPlugin
    {
        public AA_API()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        #region Aurora naming of constants
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_OK = -1;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_ERROR = -2;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_ERROR_NO_SCENE_SET = -3;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_ERROR_SCENE_MUST_BE_STATIC = -4;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_ERROR_SCENE_MUST_NOT_BE_STATIC = -5;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_ERROR_BAD_SETTING = -6;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_ERROR_NO_PRESET_FOUND = -7;

        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_AMBIENT = 0;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_SKY_BLUE_DENSITY = 1;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_SKY_BLUR_HORIZON = 2;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_CLOUD_COLOR = 3;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_CLOUD_POS_DENSITY1 = 4;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_CLOUD_POS_DENSITY2 = 5;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_CLOUD_SCALE = 6;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_CLOUD_SCROLL_X = 7;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_CLOUD_SCROLL_Y = 8;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_CLOUD_SCROLL_X_LOCK = 9;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_CLOUD_SCROLL_Y_LOCK = 10;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_CLOUD_SHADOW = 11;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_SKY_DENSITY_MULTIPLIER = 12;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_SKY_DISTANCE_MULTIPLIER = 13;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_SKY_GAMMA = 14;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_SKY_GLOW = 15;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_SKY_HAZE_DENSITY = 16;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_SKY_HAZE_HORIZON = 17;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_SKY_LIGHT_NORMALS = 18;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_SKY_MAX_ALTITUDE = 19;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_SKY_STAR_BRIGHTNESS = 20;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_SKY_SUNLIGHT_COLOR = 21;

        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_WATER_BLUR_MULTIPLIER = 22;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_WATER_FRESNEL_OFFSET = 23;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_WATER_FRESNEL_SCALE = 24;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_WATER_NORMAL_MAP = 25;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_WATER_NORMAL_SCALE = 26;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_WATER_SCALE_ABOVE = 27;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_WATER_SCALE_BELOW = 28;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_WATER_UNDERWATER_FOG_MODIFIER = 29;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_WATER_FOG_COLOR = 30;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_WATER_FOG_DENSITY = 31;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_WATER_BIG_WAVE_DIRECTION = 32;
        [APILevel(APIFlags.WindLight_Aurora)]
        public const int WL_WATER_LITTLE_WAVE_DIRECTION = 33;
        #endregion

        #region New naming of constants
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_OK = -1;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_ERROR = -2;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_ERROR_NO_SCENE_SET = -3;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_ERROR_SCENE_MUST_BE_STATIC = -4;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_ERROR_SCENE_MUST_NOT_BE_STATIC = -5;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_ERROR_BAD_SETTING = -6;
        [APILevel(APIFlags.WindLight_New)]
        public const int REGION_WL_ERROR_NO_PRESET_FOUND = -7;

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
        #endregion

        #region Aurora Sim API
        [APILevel(APIFlags.WindLight_Aurora)]
        public AnArray aaWindlightGetScene(ScriptInstance Instance, AnArray rules)
        {
#warning Implement aaWindlightGetScene(AnArray)
            return new AnArray();
        }

        [APILevel(APIFlags.WindLight_Aurora)]
        public AnArray aaWindlightGetScene(ScriptInstance Instance, int dayCycleIndex, AnArray rules)
        {
#warning Implement aaWindlightGetScene(int, AnArray)
            return new AnArray();
        }

        [APILevel(APIFlags.WindLight_Aurora)]
        public int aaWindlightGetSceneIsStatic(ScriptInstance Instance)
        {
#warning Implement aaWindlightGetSceneIsStatic()
            return 0;
        }

        [APILevel(APIFlags.WindLight_Aurora)]
        public int aaWindlightGetSceneDayCycleKeyFrameCount(ScriptInstance Instance)
        {
#warning aaWindlightGetSceneDayCycleKeyFrameCount()
            return 0;
        }

        [APILevel(APIFlags.WindLight_Aurora)]
        public AnArray aaWindlightGetDayCycle(ScriptInstance Instance)
        {
#warning aaWindlightGetDayCycle()
            return new AnArray();
        }

        [APILevel(APIFlags.WindLight_Aurora)]
        public int aaWindlightRemoveDayCycleFrame(ScriptInstance Instance, int dayCycleFrame)
        {
#warning Implement aaWindlightRemoveDayCycleFrame(int)
            return 0;
        }

        [APILevel(APIFlags.WindLight_Aurora)]
        public int aaWindlightAddDayCycleFrame(ScriptInstance Instance, double dayCyclePosition, int dayCycleFrameToCopy)
        {
#warning aaWindlightAddDayCycleFrame
            return 0;
        }

        [APILevel(APIFlags.WindLight_Aurora)]
        public int aaWindlightSetScene(ScriptInstance Instance, AnArray rules)
        {
#warning Implement aaWindlightSetScene(AnArray)
            return 0;
        }

        [APILevel(APIFlags.WindLight_Aurora)]
        public int aaWindlightSetScene(ScriptInstance Instance, int dayCycleIndex, AnArray list)
        {
#warning Implement aaWindlightSetScene(AnArray)
            return 0;
        }
        #endregion

        #region New WindLight API
        [APILevel(APIFlags.WindLight_New)]
        public AnArray rwlWindlightGetScene(ScriptInstance Instance, AnArray rules)
        {
            return aaWindlightGetScene(Instance, rules);
        }

        [APILevel(APIFlags.WindLight_New)]
        public AnArray rwlWindlightGetScene(ScriptInstance Instance, int dayCycleIndex, AnArray rules)
        {
            return aaWindlightGetScene(Instance, dayCycleIndex, rules);
        }

        [APILevel(APIFlags.WindLight_New)]
        public int rwlWindlightGetSceneIsStatic(ScriptInstance Instance)
        {
            return aaWindlightGetSceneIsStatic(Instance);
        }

        [APILevel(APIFlags.WindLight_New)]
        public int rwlWindlightGetSceneDayCycleKeyFrameCount(ScriptInstance Instance)
        {
            return aaWindlightGetSceneDayCycleKeyFrameCount(Instance);
        }

        [APILevel(APIFlags.WindLight_New)]
        public AnArray rwlWindlightGetDayCycle(ScriptInstance Instance)
        {
            return aaWindlightGetDayCycle(Instance);
        }

        [APILevel(APIFlags.WindLight_New)]
        public int rwlWindlightRemoveDayCycleFrame(ScriptInstance Instance, int dayCycleFrame)
        {
            return aaWindlightRemoveDayCycleFrame(Instance, dayCycleFrame);
        }

        [APILevel(APIFlags.WindLight_New)]
        public int rwlWindlightAddDayCycleFrame(ScriptInstance Instance, double dayCyclePosition, int dayCycleFrameToCopy)
        {
            return aaWindlightAddDayCycleFrame(Instance, dayCyclePosition, dayCycleFrameToCopy);
        }

        [APILevel(APIFlags.WindLight_New)]
        public int rwlWindlightSetScene(ScriptInstance Instance, AnArray rules)
        {
            return aaWindlightSetScene(Instance, rules);
        }

        [APILevel(APIFlags.WindLight_New)]
        public int rwlWindlightSetScene(ScriptInstance Instance, int dayCycleIndex, AnArray list)
        {
            return aaWindlightSetScene(Instance, dayCycleIndex, list);
        }
        #endregion
    }
}
