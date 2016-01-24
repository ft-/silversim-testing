// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.Scene.Types.SceneEnvironment
{
    public partial class EnvironmentController
    {
        class WaterConfig
        {
            public bool EnableTideControl;
            public double TidalBase = 20;
            public double TidalMoonAmplitude = 0.5;
            public double TidalSunAmplitude = 0.1;

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
            TidalBaseHeight,
            TidalMoonAmplitude,
            TidalSunAmplitude,
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
                        TriggerOnEnvironmentControllerChange();
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
                    case FloatWaterParams.TidalBaseHeight:
                        lock(m_TidalLock)
                        {
                            return m_WaterConfig.TidalBase;
                        }

                    case FloatWaterParams.TidalMoonAmplitude:
                        lock (m_TidalLock)
                        {
                            return m_WaterConfig.TidalMoonAmplitude;
                        }

                    case FloatWaterParams.TidalSunAmplitude:
                        lock(m_TidalLock)
                        {
                            return m_WaterConfig.TidalSunAmplitude;
                        }

                    default:
                        return 0;
                }
            }
            set
            {
                switch (type)
                {
                    case FloatWaterParams.TidalBaseHeight:
                        lock(m_TidalLock)
                        {
                            m_WaterConfig.TidalBase = value;
                        }
                        TriggerOnEnvironmentControllerChange();
                        break;

                    case FloatWaterParams.TidalMoonAmplitude:
                        lock (m_TidalLock)
                        {
                            m_WaterConfig.TidalMoonAmplitude = value;
                        }
                        TriggerOnEnvironmentControllerChange();
                        break;

                    case FloatWaterParams.TidalSunAmplitude:
                        lock(m_TidalLock)
                        {
                            m_WaterConfig.TidalSunAmplitude = value;
                        }
                        TriggerOnEnvironmentControllerChange();
                        break;

                    default:
                        break;
                }
            }
        }

        void TidalTimer()
        {
            double waterHeight;
            double sun_phase = SunPhase;
            double moon_phase = MoonPhase;

            lock (m_TidalLock)
            {
                if(!m_WaterConfig.EnableTideControl)
                {
                    return;
                }

                waterHeight = m_WaterConfig.TidalBase;
                waterHeight += Math.Sin(moon_phase) * m_WaterConfig.TidalMoonAmplitude;
                waterHeight += Math.Sin(sun_phase) * m_WaterConfig.TidalSunAmplitude;
            }

            m_Scene.RegionSettings.WaterHeight = waterHeight;
            m_Scene.TriggerRegionSettingsChanged();
        }
    }
}
