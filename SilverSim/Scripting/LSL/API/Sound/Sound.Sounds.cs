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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Asset;

namespace SilverSim.Scripting.LSL.API.Sound
{
    public partial class Sound_API
    {
        [APILevel(APIFlags.LSL)]
        public static void llCollisionSound(ScriptInstance Instance, string impact_sound, double impact_volume)
        {
            ObjectPart.CollisionSoundParam para = new ObjectPart.CollisionSoundParam();

            lock (Instance)
            {
                if (impact_volume < 0f) impact_volume = 0f;
                if (impact_volume > 1f) impact_volume = 1f;

                para.ImpactVolume = impact_volume;
                try
                {
                    para.ImpactSound = getSoundAssetID(Instance, impact_sound);
                    Instance.Part.CollisionSound = para;
                }
                catch
                {
                    Instance.ShoutError(string.Format("Inventory item {0} does not reference a sound", impact_sound));
                }
            }
        }

        [APILevel(APIFlags.LSL)]
        public static void llLoopSound(ScriptInstance Instance, string sound, double volume)
        {
#warning Implement llLoopSound(string, double)
        }

        [APILevel(APIFlags.LSL)]
        public static void llLoopSoundMaster(ScriptInstance Instance, string sound, double volume)
        {
#warning Implement llLoopSoundMaster(string, double)
        }

        [APILevel(APIFlags.LSL)]
        public static void llLoopSoundSlave(ScriptInstance Instance, string sound, double volume)
        {
#warning Implement llLoopSoundSlave(string, double)
        }

        [APILevel(APIFlags.LSL)]
        public static void llPreloadSound(ScriptInstance Instance, string sound)
        {
#warning Implement llPreloadSound(string)
        }

        [APILevel(APIFlags.LSL)]
        public static void llStopSound(ScriptInstance Instance)
        {
#warning Implement llStopSound()
        }

        [APILevel(APIFlags.LSL)]
        public static void llPlaySound(ScriptInstance Instance, string sound, double volume)
        {
#warning Implement llPlaySound(string, double)
        }

        [APILevel(APIFlags.LSL)]
        public static void llPlaySoundSlave(ScriptInstance Instance, string sound, double volume)
        {
#warning Implement llPlaySoundSlave(string, double)
        }

        [APILevel(APIFlags.LSL)]
        public static void llTriggerSound(ScriptInstance Instance, string sound, double volume)
        {
#warning Implement llTriggerSound(string, double)
        }

        [APILevel(APIFlags.LSL)]
        public static void llTriggerSoundLimited(ScriptInstance Instance, string sound, double volume, Vector3 top_north_east, Vector3 bottom_south_west)
        {
#warning Implement llTriggerSoundLimited(string, double, Vector3, Vector3)
        }

        [APILevel(APIFlags.LSL)]
        public static void llAdjustSoundVolume(ScriptInstance Instance, double volume)
        {
#warning Implement llAdjustSoundVolume(double)
        }

        [APILevel(APIFlags.LSL)]
        public static void llSetSoundQueueing(ScriptInstance Instance, int queue)
        {
            lock (Instance) Instance.Part.IsSoundQueueing = queue != 0;
        }

        [APILevel(APIFlags.LSL)]
        public static void llSetSoundRadius(ScriptInstance Instance, double radius)
        {
#warning Implement llSetSoundRadius(double)
        }
    }
}
