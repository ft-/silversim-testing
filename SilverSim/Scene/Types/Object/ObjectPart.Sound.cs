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

using log4net;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;
using System.Threading;
using ThreadedClasses;

namespace SilverSim.Scene.Types.Object
{
    public partial class ObjectPart
    {
        public class SoundParam
        {
            #region Constructor
            public SoundParam()
            {

            }
            #endregion

            #region Fields
            public UUID SoundID;
            public double Gain;
            public double Radius;
            public PrimitiveSoundFlags Flags;
            #endregion
        }
        private readonly SoundParam m_Sound = new SoundParam();

        public SoundParam Sound
        {
            get
            {
                SoundParam p = new SoundParam();
                lock(m_Sound)
                {
                    p.Flags = m_Sound.Flags;
                    p.Gain = m_Sound.Gain;
                    p.Radius = m_Sound.Radius;
                    p.SoundID = m_Sound.SoundID;
                }
                return p;
            }
            set
            {
                lock(m_Sound)
                {
                    m_Sound.SoundID = value.SoundID;
                    m_Sound.Gain = value.Gain;
                    m_Sound.Radius = value.Radius;
                    m_Sound.Flags = value.Flags;
                }
                IsChanged = true;
                TriggerOnUpdate(0);
            }
        }

        public class CollisionSoundParam
        {
            #region Constructor
            public CollisionSoundParam()
            {

            }
            #endregion

            #region Fields
            public UUID ImpactSound = UUID.Zero;
            public double ImpactVolume = 0f;
            #endregion
        }
        private readonly CollisionSoundParam m_CollisionSound = new CollisionSoundParam();

        private bool m_IsSoundQueueing = false;

        public CollisionSoundParam CollisionSound
        {
            get
            {
                CollisionSoundParam res = new CollisionSoundParam();
                lock (m_CollisionSound)
                {
                    res.ImpactSound = m_CollisionSound.ImpactSound;
                    res.ImpactVolume = m_CollisionSound.ImpactVolume;
                }
                return res;
            }
            set
            {
                lock (m_CollisionSound)
                {
                    m_CollisionSound.ImpactSound = value.ImpactSound;
                    m_CollisionSound.ImpactVolume = value.ImpactVolume;
                }
                IsChanged = true;
                TriggerOnUpdate(0);
            }
        }
    }
}
