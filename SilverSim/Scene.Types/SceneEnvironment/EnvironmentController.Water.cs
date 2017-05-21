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

using System;

namespace SilverSim.Scene.Types.SceneEnvironment
{
    public partial class EnvironmentController
    {
        private class WaterConfig
        {
            public bool EnableTideControl;
            public double TidalBaseHeight = 20;
            public double TidalMoonAmplitude = 0.5;
            public double TidalSunAmplitude = 0.1;
        }

        private readonly WaterConfig m_WaterConfig = new WaterConfig();

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

        private readonly object m_TidalLock = new object();

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
                            return m_WaterConfig.TidalBaseHeight;
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
                            m_WaterConfig.TidalBaseHeight = value;
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

        private void TidalTimer()
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

                waterHeight = m_WaterConfig.TidalBaseHeight;
                waterHeight += Math.Sin(moon_phase) * m_WaterConfig.TidalMoonAmplitude;
                waterHeight += Math.Sin(sun_phase) * m_WaterConfig.TidalSunAmplitude;
            }

            m_Scene.RegionSettings.WaterHeight = waterHeight;
            m_Scene.TriggerRegionSettingsChanged();
        }
    }
}
