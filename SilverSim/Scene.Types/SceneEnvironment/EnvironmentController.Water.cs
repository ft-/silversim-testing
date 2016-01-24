// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Scene.Types.SceneEnvironment
{
    public partial class EnvironmentController
    {
        class WaterConfig
        {
            public bool EnableTideControl;
            public double TidalBase = 20;
            public double TidalAmplitude = 0.5;
            public double TidalPhaseOffset;
            public double TidalPeriodLengthInSecs = 2.1 * 3600; /* alittle difference for having it move through time */

            public WaterConfig()
            {

            }
        }

        readonly WaterConfig m_WaterConfig = new WaterConfig();

        public enum BooleanWaterParams
        {
            EnableTideControl,
        }

        public enum FloatWaterParams
        {
            TidalBase,
            TidalAmplitude,
            TidalPhaseOffset,
            TidalPeriodLengthInSecs
        }

        readonly object m_TidalLock = new object();

        public bool this[BooleanWaterParams type]
        {
            get
            {
                switch (type)
                {
                    case BooleanWaterParams.EnableTideControl:
                        return m_WaterConfig.EnableTideControl;

                    default:
                        return false;
                }
            }
            set
            {
                switch (type)
                {
                    case BooleanWaterParams.EnableTideControl:
                        lock(m_TidalLock)
                        {
                            m_WaterConfig.EnableTideControl = value;
                        }
                        break;

                    default:
                        break;
                }
            }
        }

        public double this[FloatWaterParams type]
        {
            get
            {
                switch (type)
                {
                    case FloatWaterParams.TidalBase:
                        lock(m_TidalLock)
                        {
                            return m_WaterConfig.TidalBase;
                        }

                    case FloatWaterParams.TidalAmplitude:
                        lock (m_TidalLock)
                        {
                            return m_WaterConfig.TidalAmplitude;
                        }

                    case FloatWaterParams.TidalPhaseOffset:
                        lock (m_TidalLock)
                        {
                            return m_WaterConfig.TidalPhaseOffset;
                        }

                    case FloatWaterParams.TidalPeriodLengthInSecs:
                        lock(m_TidalLock)
                        {
                            return m_WaterConfig.TidalPeriodLengthInSecs;
                        }

                    default:
                        return 0;
                }
            }
            set
            {
                switch (type)
                {
                    case FloatWaterParams.TidalBase:
                        lock(m_TidalLock)
                        {
                            m_WaterConfig.TidalBase = value;
                        }
                        break;

                    case FloatWaterParams.TidalAmplitude:
                        lock (m_TidalLock)
                        {
                            m_WaterConfig.TidalAmplitude = value;
                        }
                        break;

                    case FloatWaterParams.TidalPhaseOffset:
                        lock (m_TidalLock)
                        {
                            m_WaterConfig.TidalPhaseOffset = value % (2 * Math.PI);
                        }
                        break;

                    case FloatWaterParams.TidalPeriodLengthInSecs:
                        if(value < 1)
                        {
                            value = 1;
                        }
                        lock(m_TidalLock)
                        {
                            m_WaterConfig.TidalPeriodLengthInSecs = value;
                        }
                        break;

                    default:
                        break;
                }
            }
        }

        void TidalTimer()
        {
            double waterHeight;
            lock(m_TidalLock)
            {
                if(!m_WaterConfig.EnableTideControl)
                {
                    return;
                }
                ulong utctime = Date.GetUnixTime();
                double DailyOmega = 2f / m_WaterConfig.TidalPeriodLengthInSecs;
                double daily_phase = DailyOmega * utctime + m_WaterConfig.TidalPhaseOffset;
                double moon_phase = daily_phase % (2 * Math.PI);
                waterHeight = Math.Sin(moon_phase) * m_WaterConfig.TidalAmplitude + m_WaterConfig.TidalBase;
            }

            m_Scene.RegionSettings.WaterHeight = waterHeight;
            m_Scene.TriggerRegionSettingsChanged();
        }
    }
}
