// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Net.Sockets;
using System.Threading;

namespace SilverSim.Main.Common
{
    public class Startup
    {
        ConfigurationLoader m_ConfigLoader;
        ManualResetEvent m_ShutdownEvent = new ManualResetEvent(false);

        public bool Run(string[] args, Action<string> writeLine)
        {
            try
            {
                m_ConfigLoader = new ConfigurationLoader(args, m_ShutdownEvent);
            }
            catch (ConfigurationLoader.ConfigurationErrorException e)
            {
                writeLine(e.Message);
#if DEBUG
                writeLine(e.StackTrace);
#endif
                return false;
            }
            catch (ConfigurationLoader.TestingErrorException)
            {
                return false;
            }
            catch(SocketException e)
            {
                writeLine(string.Format("Startup Exception {0}: {1}\n{2}", e.GetType().Name, e.Message, e.StackTrace));
                return false;
            }
            catch (Exception e)
            {
#if DEBUG
                writeLine(string.Format("Startup Exception {0}: {1}\n{2}", e.GetType().Name, e.Message, e.StackTrace));
#else
                writeLine(string.Format("Startup Exception {0}: {1}", e.GetType().Name, e.Message));
#endif
                return false;
            }

            m_ShutdownEvent.WaitOne();

            m_ConfigLoader.Shutdown();
            return true;
        }

        public void Shutdown()
        {
            m_ConfigLoader.TriggerShutdown();
        }

        ~Startup()
        {
            m_ShutdownEvent.Dispose();
        }
    }
}
