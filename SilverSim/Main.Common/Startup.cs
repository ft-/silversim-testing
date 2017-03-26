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
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;

namespace SilverSim.Main.Common
{
    public class Startup
    {
        ConfigurationLoader m_ConfigLoader;
        readonly ManualResetEvent m_ShutdownEvent = new ManualResetEvent(false);

        public bool IsRunningAsService { get; set; }

        public bool Run(string[] args, Action<string> writeLine)
        {
            try
            {
                m_ConfigLoader = new ConfigurationLoader(args, m_ShutdownEvent, IsRunningAsService ? ConfigurationLoader.LocalConsole.Disallowed : ConfigurationLoader.LocalConsole.Allowed, IsRunningAsService);
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
