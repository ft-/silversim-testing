// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.Scene.Types.SceneEnvironment
{
    public partial class EnvironmentController
    {
        class WeatherConfig
        {
            public bool EnableLightShareControl;

            public WeatherConfig()
            {

            }
        }

        readonly WeatherConfig m_WeatherConfig = new WeatherConfig();

        public enum BooleanWeatherParams
        {
            EnableLightShare,
        }

        public bool this[BooleanWeatherParams type]
        {
            get
            {
                switch (type)
                {
                    case BooleanWeatherParams.EnableLightShare:
                        return m_WeatherConfig.EnableLightShareControl;

                    default:
                        return false;
                }
            }
            set
            {
                switch (type)
                {
                    case BooleanWeatherParams.EnableLightShare:
                        m_LightShareLock.AcquireWriterLock(-1);
                        try
                        {
                            m_WeatherConfig.EnableLightShareControl = value;
                        }
                        finally
                        {
                            m_LightShareLock.ReleaseWriterLock();
                        }
                        break;

                    default:
                        break;
                }
            }
        }
    }
}
