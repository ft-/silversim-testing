// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;

namespace SilverSim.Main.Service
{
    sealed class MainService : ServiceBase
    {
        public const string SERVICE_NAME = "SilverSim";
        Action m_ShutdownDelegate;

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

        [SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule")]
        static void Main()
        {
            Run(new MainService());
        }

        protected override void OnStart(string[] args)
        {
            new Thread(ServiceMain).Start(args);
            base.OnStart(args);
        }

        protected override void OnStop()
        {
            Action shutdownDelegate = m_ShutdownDelegate;
            if(shutdownDelegate != null)
            {
                shutdownDelegate();
            }
            while(!m_ShutdownCompleteEvent.WaitOne(1000))
            {
                RequestAdditionalTime(1000);
            }
            base.OnStop();
        }

        protected override void OnShutdown()
        {
            Action shutdownDelegate = m_ShutdownDelegate;
            if (shutdownDelegate != null)
            {
                shutdownDelegate();
            }
            while (!m_ShutdownCompleteEvent.WaitOne(1000))
            {
                RequestAdditionalTime(1000);
            }
            base.OnShutdown();
        }

        readonly ManualResetEvent m_ShutdownCompleteEvent = new ManualResetEvent(false);

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        void ServiceMain(object obj)
        {
            string[] args = (string[])obj;
            EventLog eventLog = EventLog;

            m_ShutdownCompleteEvent.Reset();

            Thread.CurrentThread.Name = "SilverSim:Main";

            /* by not hard referencing the assembly we can actually implement an updater concept here */
            Assembly assembly = Assembly.Load("SilverSim.Main.Common");
            Type t = assembly.GetType("SilverSim.Main.Common.Startup");
            object startup = Activator.CreateInstance(t);
            MethodInfo mi = t.GetMethod("Run");
            PropertyInfo pi = t.GetProperty("IsRunningAsService");
            pi.SetMethod.Invoke(startup, new object[] { true });
            m_ShutdownDelegate = (Action)Delegate.CreateDelegate(typeof(Action), startup, t.GetMethod("Shutdown"));
            Action<string> del = eventLog.WriteEntry;
            if (!(bool)mi.Invoke(startup, new object[] { args, del }))
            {
                Stop();
            }

            m_ShutdownCompleteEvent.Set();
        }

        ~MainService()
        {
            m_ShutdownCompleteEvent.Dispose();
        }
    }
}
