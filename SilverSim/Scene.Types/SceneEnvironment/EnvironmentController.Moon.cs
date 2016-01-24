// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
