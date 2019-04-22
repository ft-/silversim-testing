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
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Groups;
using System;
using System.ComponentModel;
using System.Threading;

namespace SilverSim.Main.IM
{
    [Description("IM Router")]
    [PluginName("GroupIMRouter")]
    public sealed class GroupIMRouter : IGroupChatInterface
    {
        private BlockingQueue<GroupInstantMessage> m_Queue = new BlockingQueue<GroupInstantMessage>();
        private RwLockedList<Thread> m_Threads = new RwLockedList<Thread>();
        private readonly uint m_MaxThreads;
        private readonly object m_Lock = new object();
        private AvatarNameServiceInterface m_AvatarNameService;
        private GroupsServiceInterface m_GroupsService;
        private readonly string[] m_AvatarNameServiceNames;
        private readonly string m_GroupsServiceName;
        private IMRouter m_IMRouter;

        private sealed class GroupSession
        {
            public readonly UGI Group;
            public readonly UUID SessionID = UUID.Random;
            public readonly RwLockedList<UGUI> Participants = new RwLockedList<UGUI>();

            public GroupSession(UGI group)
            {
                Group = group;
            }
        }

        private readonly RwLockedDictionary<UUID, GroupSession> m_ActiveChats = new RwLockedDictionary<UUID, GroupSession>();
        private readonly RwLockedDictionary<UUID, GroupSession> m_ActiveSessions = new RwLockedDictionary<UUID, GroupSession>();

        #region Constructor
        public GroupIMRouter(IConfig ownSection)
        {
            m_MaxThreads = (uint)ownSection.GetInt("MaxThreads");
            m_GroupsServiceName = ownSection.GetString("GroupsService");
            m_AvatarNameServiceNames = ownSection.GetString("AvatarNameServices").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_IMRouter = loader.IMRouter;
            RwLockedList<AvatarNameServiceInterface> services = new RwLockedList<AvatarNameServiceInterface>();
            foreach(string name in m_AvatarNameServiceNames)
            {
                AvatarNameServiceInterface service;
                loader.GetService(name, out service);
                services.Add(service);
            }
            m_AvatarNameService = new AggregatingAvatarNameService(services);

            loader.GetService(m_GroupsServiceName, out m_GroupsService);
        }
        #endregion

        public void Leave(UUID sessionid, UGUI agent)
        {
            GroupSession session;
            if(m_ActiveSessions.TryGetValue(sessionid, out session))
            {
                session.Participants.Remove(agent);
                m_ActiveChats.RemoveIf(session.Group.ID, (r) => r.Participants.Count == 0);
            }
        }

        public void Send(GroupInstantMessage im)
        {
            m_Queue.Enqueue(im);
            lock (m_Lock)
            {
                if (m_Queue.Count > m_Threads.Count && m_Threads.Count < m_MaxThreads)
                {
                    ThreadManager.CreateThread(IMSendThread).Start(this);
                }
            }
        }

        private void IMSendThread(object s)
        {
            var service = (IMServiceHandler)s;
            Thread thread = Thread.CurrentThread;
            thread.Name = "GroupIM:Send Thread";
            service.m_Threads.Add(thread);
            try
            {
                while (true)
                {
                    GroupInstantMessage im = m_Queue.Dequeue(1000);
                    try
                    {
                        if (im.IMSessionID == UUID.Zero)
                        {
                            IMSessionStarted(im);
                        }
                        else
                        {
                            IMSessionContinued(im);
                        }
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

        private void IMSessionStarted(GroupInstantMessage im)
        {
            if (!m_GroupsService.Members.ContainsKey(im.FromAgent, im.ToGroup, im.FromAgent))
            {
                throw new Exception("Not a member");
            }
            GroupSession session = m_ActiveChats.GetOrAddIfNotExists(im.ToGroup.ID, () => new GroupSession(im.ToGroup));
            im.IMSessionID = session.SessionID;
            im.FromGroup = im.ToGroup;
            try
            {
                foreach (GroupMember gm in m_GroupsService.Members[im.FromAgent, im.ToGroup])
                {
                    session.Participants.AddIfNotExists(gm.Principal);
                }
            }
            finally
            {
                m_ActiveSessions.RemoveIf(im.IMSessionID, (entry) => entry.Participants.Count == 0);
                m_ActiveChats.RemoveIf(im.ToGroup.ID, (entry) => entry.Participants.Count == 0);
            }

            DistributeMessage(session, im);
        }

        private void IMSessionContinued(GroupInstantMessage im)
        {
            GroupSession session;
            if(!m_ActiveSessions.TryGetValue(im.IMSessionID, out session) || session.Group.EqualsGrid(im.ToGroup))
            {
                IMSessionStarted(im);
            }
            else if(!m_GroupsService.Members.ContainsKey(im.FromAgent, im.ToGroup, im.FromAgent))
            {
                throw new Exception("Not a member");
            }
            else
            {
                DistributeMessage(session, im);
            }
        }

        private void DistributeMessage(GroupSession session, GroupInstantMessage im)
        {
            try
            {
                foreach(UGUI target in session.Participants)
                {
                    m_IMRouter.SendWithResultDelegate(im.GetAgentIM(target));
                }
            }
            catch
            {
                /* do not pass any from here since we cannot reliably say so which failed */
            }
        }
    }
}
