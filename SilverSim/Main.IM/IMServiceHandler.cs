// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Scene.Management.IM;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.Types.IM;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using ThreadedClasses;

namespace SilverSim.Main.IM
{
    #region Service Implementation
    [SuppressMessage("Gendarme.Rules.Concurrency", "DoNotLockOnThisOrTypesRule")]
    public class IMServiceHandler : IMServiceInterface, IPlugin
    {
        protected internal BlockingQueue<GridInstantMessage> m_Queue = new BlockingQueue<GridInstantMessage>();
        protected internal RwLockedList<Thread> m_Threads = new RwLockedList<Thread>();
        private uint m_MaxThreads;

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        void IMSendThread(object s)
        {
            IMServiceHandler service = (IMServiceHandler)s;
            Thread thread = Thread.CurrentThread;
            thread.Name = "IM:Send Thread";
            service.m_Threads.Add(thread);
            try
            {
                while(true)
                {
                    GridInstantMessage im = m_Queue.Dequeue(1000);
                    try
                    {
                        IMRouter.SendWithResultDelegate(im);
                    }
                    catch
                    {
                        im.OnResult(im, false);
                    }
                }
            }
            catch
            {
                service.m_Threads.Remove(thread);
            }
        }
        
        #region Constructor
        public IMServiceHandler(uint maxThreads)
        {
            m_MaxThreads = maxThreads;
        }

        public void Startup(ConfigurationLoader loader)
        {
        }
        #endregion

        #region IM Service
        public override void Send(GridInstantMessage im)
        {
            m_Queue.Enqueue(im);
            lock(this)
            {
                if(m_Queue.Count > m_Threads.Count && m_Threads.Count < m_MaxThreads)
                {
                    new Thread(IMSendThread).Start(this);
                }
            }
        }
        #endregion
    }
    #endregion

    #region Factory
    [PluginName("IMService")]
    public class IMServiceHandlerFactory : IPluginFactory
    {
        public IMServiceHandlerFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new IMServiceHandler((uint)ownSection.GetInt("MaxThreads"));
        }
    }
    #endregion
}
