// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using System;
using System.Reflection;
using System.Threading;

namespace SilverSim.Main
{
    public static class Application
    {
        public static ConfigurationLoader m_ConfigLoader;
        public static ManualResetEvent m_ShutdownEvent = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            Console.TreatControlCAsInput = true;

            Thread.CurrentThread.Name = "SilverSim:Main";
            try
            {
                m_ConfigLoader = new ConfigurationLoader(args, m_ShutdownEvent);
            }
            catch(ConfigurationLoader.ConfigurationError e)
            {
#if DEBUG
                System.Console.Write(e.StackTrace.ToString());
                System.Console.WriteLine();
#endif
                Environment.Exit(1);
            }
            catch(ConfigurationLoader.TestingError)
            {
                Environment.Exit(1);
            }
            catch(Exception e)
            {
#if DEBUG
                System.Console.Write(String.Format("Exception {0}: {1}", e.GetType().Name, e.Message));
                System.Console.Write(e.StackTrace.ToString());
                System.Console.WriteLine();
#endif
                Environment.Exit(1);
            }

            m_ShutdownEvent.WaitOne();

            m_ConfigLoader.Shutdown();
        }
    }
}
