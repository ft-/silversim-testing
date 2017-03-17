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
using System;

namespace SilverSim.Scene.Types.SceneEnvironment
{
    public partial class EnvironmentController
    {
        double m_MoonPhase;
        double m_MoonPhaseOffset;
        double m_MoonPeriodLengthInSecs = 2.1 * 3600; /* a little difference for having it move through day time */

        public double MoonPhase
        {
            get
            {
                lock(m_EnvironmentLock)
                {
                    return m_MoonPhase;
                }
            }
        }

        public double MoonPhaseOffset
        {
            get
            {
                lock(m_EnvironmentLock)
                {
                    return m_MoonPhaseOffset;
                }
            }
            set
            {
                lock(m_EnvironmentLock)
                {
                    m_MoonPhaseOffset = value % (2 * Math.PI);
                }
                TriggerOnEnvironmentControllerChange();
            }
        }

        public double MoonPeriodLengthInSecs
        {
            get
            {
                lock(m_EnvironmentLock)
                {
                    return m_MoonPeriodLengthInSecs;
                }
            }
            set
            {
                lock(m_EnvironmentLock)
                {
                    if(value < 1)
                    {
                        value = 1;
                    }
                    m_MoonPeriodLengthInSecs = value;
                }
                TriggerOnEnvironmentControllerChange();
            }
        }
        public void UpdateMoonPhase()
        {
            lock(m_EnvironmentLock)
            {
                ulong utctime = Date.GetUnixTime();
                double DailyOmega = 2f / m_MoonPeriodLengthInSecs;
                m_MoonPhase = DailyOmega * utctime + m_MoonPhaseOffset;
            }
        }
    }
}
