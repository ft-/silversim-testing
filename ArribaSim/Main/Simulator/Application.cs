using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ArribaSim.Main.Common;

namespace ArribaSim.Main.Simulator
{
    public static class Application
    {
        private const string DEFAULT_CONFIG_FILENAME = "../data/ArribaSim.ini";

        public static ConfigurationLoader m_ConfigLoader;

        public static void Main(string[] args)
        {
            Thread.CurrentThread.Name = "ArribaSim:Main";
            try
            {
                m_ConfigLoader = new ConfigurationLoader(args, DEFAULT_CONFIG_FILENAME, "Simulator.defaults.ini");
            }
            catch(ConfigurationLoader.ConfigurationError e)
            {
#if DEBUG
                System.Console.Write(e.StackTrace.ToString());
                System.Console.WriteLine();
#endif
                return;
            }
            catch(Exception e)
            {
#if DEBUG
                System.Console.Write(String.Format("Exception {0}: {1}", e.GetType().Name, e.Message));
                System.Console.Write(e.StackTrace.ToString());
                System.Console.WriteLine();
#endif
                return;
            }
        }
    }
}
