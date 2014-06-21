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
    }
}
