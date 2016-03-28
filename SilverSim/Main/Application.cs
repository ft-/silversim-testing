// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace SilverSim.Main
{
    static class Application
    {
        static ConfigurationLoader m_ConfigLoader;
        static ManualResetEvent m_ShutdownEvent = new ManualResetEvent(false);

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        static void Main(string[] args)
        {
            Console.TreatControlCAsInput = true;

            Thread.CurrentThread.Name = "SilverSim:Main";
            try
            {
                m_ConfigLoader = new ConfigurationLoader(args, m_ShutdownEvent);
            }
            catch(ConfigurationLoader.ConfigurationErrorException e)
            {
                Console.WriteLine(e.Message);
#if DEBUG
                Console.WriteLine(e.StackTrace);
#endif
                Environment.Exit(1);
            }
            catch(ConfigurationLoader.TestingErrorException)
            {
                Environment.Exit(1);
            }
            catch(Exception e)
            {
                Console.WriteLine(String.Format("Exception {0}: {1}", e.GetType().Name, e.Message));
#if DEBUG
                Console.WriteLine(e.StackTrace);
#endif
                Environment.Exit(1);
            }

            m_ShutdownEvent.WaitOne();

            m_ConfigLoader.Shutdown();
        }
    }
}
