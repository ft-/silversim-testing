namespace SilverSim.Scene.Types.SceneEnvironment
{
    public partial class EnvironmentController
    {
        struct WeatherConfig
        {
            public bool EnableLightShareControl;
        }

        WeatherConfig m_WeatherConfig = new WeatherConfig();

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
