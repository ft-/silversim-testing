/*

ArribaSim is distributed under the terms of the
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
using ArribaSim.Scene.Types.Object;
using ArribaSim.Types;
using ArribaSim.Types.Asset;

namespace ArribaSim.Scripting.LSL.Variants.LSL
{
    public partial class LSLScript
    {
        public void llCollisionSound(AString impact_sound, Real impact_volume)
        {
            ObjectPartInventoryItem item;
            ObjectPart.CollisionSoundParam para = new ObjectPart.CollisionSoundParam();
            
            if (impact_volume < 0) impact_volume = new Real(0);
            if (impact_volume > 1) impact_volume = new Real(1);

            para.ImpactVolume = impact_volume;

            if (Part.Inventory.TryGetValue(impact_sound.ToString(), out item))
            {
                if (item.AssetType == AssetType.Sound || item.AssetType == AssetType.SoundWAV)
                {
                    para.ImpactSound = item.AssetID;
                    Part.CollisionSound = para;
                }
                else
                {
                    llShout(DEBUG_CHANNEL, AString.Format("Inventory item {0} does not reference a sound", impact_sound));
                }
            }
            else
            {
                UUID id;
                if (UUID.TryParse(impact_sound.ToString(), out id))
                {
                    para.ImpactSound = id;
                    Part.CollisionSound = para;
                }
                else
                {
                    llShout(DEBUG_CHANNEL, AString.Format("'{0}' does not reference an inventory item nor a key", impact_sound));
                }
            }
        }

        public void llLoopSound(AString sound, Real volume)
        {

        }

        public void llLoopSoundMaster(AString sound, Real volume)
        {

        }

        public void llLoopSoundSlave(AString sound, Real volume)
        {

        }

        public void llPreloadSound(AString sound)
        {

        }

        public void llStopSound()
        {

        }

        public void llPlaySound(AString sound, Real volume)
        {

        }

        public void llPlaySoundSlave(AString sound, Real volume)
        {

        }

        public void llTriggerSound(AString sound, Real volume)
        {

        }

        public void llTriggerSoundLimited(AString sound, Real volume, Vector3 top_north_east, Vector3 bottom_south_west)
        {

        }
    }
}
