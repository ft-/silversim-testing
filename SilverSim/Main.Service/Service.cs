/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.Main.Common;
using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;

namespace SilverSim.Main.Service
{
    class MainService : ServiceBase
    {
        public const string SERVICE_NAME = "SilverSim";

        public MainService()
        {
            ServiceName = SERVICE_NAME;
            EventLog.Source = SERVICE_NAME;
            EventLog.Log = "Application";

            CanHandlePowerEvent = false;
            CanHandleSessionChangeEvent = false;
            CanPauseAndContinue = false;
            CanShutdown = true;
            CanStop = true;

            if (!EventLog.SourceExists(EventLog.Source))
            {
                EventLog.CreateEventSource(EventLog.Source, EventLog.Log);
            }
        }

        static void Main()
        {
            ServiceBase.Run(new MainService());
        }

        protected override void OnStart(string[] args)
        {
            new System.Threading.Thread(ServiceMain).Start(args);
            base.OnStart(args);
        }

        protected override void OnStop()
        {
            m_ShutdownEvent.Set();
            while(!m_ShutdownCompleteEvent.WaitOne(1000))
            {
                RequestAdditionalTime(1000);
            }
            base.OnStop();
        }

        protected override void OnShutdown()
        {
            m_ShutdownEvent.Set();
            while (!m_ShutdownCompleteEvent.WaitOne(1000))
            {
                RequestAdditionalTime(1000);
            }
            base.OnShutdown();
        }

        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            return base.OnPowerEvent(powerStatus);
        }

        public ConfigurationLoader m_ConfigLoader;
        public ManualResetEvent m_ShutdownEvent = new ManualResetEvent(false);
        public ManualResetEvent m_ShutdownCompleteEvent = new ManualResetEvent(false);

        void ServiceMain(object obj)
        {
            string[] args = (string[])obj;

            m_ShutdownCompleteEvent.Reset();

            Thread.CurrentThread.Name = "SilverSim:Main";
            try
            {
                m_ConfigLoader = new ConfigurationLoader(args, m_ShutdownEvent, ConfigurationLoader.LocalConsole.Disallowed);
            }
            catch (ConfigurationLoader.ConfigurationError e)
            {
                EventLog.WriteEntry(String.Format("Exception {0}: {1}", e.GetType().Name, e.Message) + e.StackTrace.ToString());
            }
            catch (Exception e)
            {
                EventLog.WriteEntry(String.Format("Exception {0}: {1}", e.GetType().Name, e.Message) + e.StackTrace.ToString());
            }

            m_ShutdownEvent.WaitOne();

            m_ConfigLoader.Shutdown();
            m_ShutdownCompleteEvent.Set();
        }

    }
}
