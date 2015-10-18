// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;

namespace SilverSim.Main.Service
{
    sealed class MainService : ServiceBase
    {
        public const string SERVICE_NAME = "SilverSim";

        public MainService()
        {
            ServiceName = SERVICE_NAME;
            EventLog eventLog = EventLog;
            eventLog.Source = SERVICE_NAME;
            eventLog.Log = "Application";

            CanHandlePowerEvent = false;
            CanHandleSessionChangeEvent = false;
            CanPauseAndContinue = false;
            CanShutdown = true;
            CanStop = true;

            if (!EventLog.SourceExists(eventLog.Source))
            {
                EventLog.CreateEventSource(eventLog.Source, eventLog.Log);
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

        ConfigurationLoader m_ConfigLoader;
        ManualResetEvent m_ShutdownEvent = new ManualResetEvent(false);
        ManualResetEvent m_ShutdownCompleteEvent = new ManualResetEvent(false);

        void ServiceMain(object obj)
        {
            string[] args = (string[])obj;
            EventLog eventLog = EventLog;

            m_ShutdownCompleteEvent.Reset();

            Thread.CurrentThread.Name = "SilverSim:Main";
            try
            {
                m_ConfigLoader = new ConfigurationLoader(args, m_ShutdownEvent, ConfigurationLoader.LocalConsole.Disallowed);
            }
            catch (ConfigurationLoader.ConfigurationErrorException e)
            {
                eventLog.WriteEntry(String.Format("Exception {0}: {1}", e.GetType().Name, e.Message) + e.StackTrace);
            }
            catch (Exception e)
            {
                eventLog.WriteEntry(String.Format("Exception {0}: {1}", e.GetType().Name, e.Message) + e.StackTrace);
            }

            m_ShutdownEvent.WaitOne();

            m_ConfigLoader.Shutdown();
            m_ShutdownCompleteEvent.Set();
        }

    }
}
