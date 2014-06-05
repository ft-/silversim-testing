using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArribaSim.Main.Common;

namespace ArribaSim.Main.Simulator
{
    public static class Application
    {
        private const string DEFAULT_CONFIG_FILENAME = "../data/ArribaSim.ini";

        public static ConfigurationLoader m_ConfigLoader;

        public static void Main(string[] args)
        {
            try
            {
                m_ConfigLoader = new ConfigurationLoader(args, DEFAULT_CONFIG_FILENAME, "Simulator.defaults.ini");
            }
            catch(ConfigurationLoader.ConfigurationError)
            {
                return;
            }
            catch(Exception e)
            {
                System.Console.Write(String.Format("Exception {0}: {1}", e.GetType().Name, e.Message));
                return;
            }
        }
    }
}
