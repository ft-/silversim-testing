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

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Scene.Management.IM;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.Threading;
using SilverSim.Types.IM;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace SilverSim.Main.IM
{
    #region Service Implementation
    [SuppressMessage("Gendarme.Rules.Concurrency", "DoNotLockOnThisOrTypesRule")]
    [Description("IM Service")]
    public class IMServiceHandler : IMServiceInterface, IPlugin
    {
        protected internal BlockingQueue<GridInstantMessage> m_Queue = new BlockingQueue<GridInstantMessage>();
        protected internal RwLockedList<Thread> m_Threads = new RwLockedList<Thread>();
        readonly uint m_MaxThreads;
        readonly object m_Lock = new object();
        IMRouter m_IMRouter;

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
                        m_IMRouter.SendWithResultDelegate(im);
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
            m_IMRouter = loader.IMRouter;
        }
        #endregion

        #region IM Service
        public override void Send(GridInstantMessage im)
        {
            m_Queue.Enqueue(im);
            lock(m_Lock)
            {
                if(m_Queue.Count > m_Threads.Count && m_Threads.Count < m_MaxThreads)
                {
                    ThreadManager.CreateThread(IMSendThread).Start(this);
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
