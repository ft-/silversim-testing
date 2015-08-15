// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ThreadedClasses;

namespace SilverSim.Scene.Physics.Bullet.Implementation
{
    public partial class BulletScene
    {
        public C5.TreeDictionary<UUID, int> Primitives = new C5.TreeDictionary<UUID, int>();


        readonly BlockingQueue<ObjectUpdateInfo> m_BulletUpdateQueue = new BlockingQueue<ObjectUpdateInfo>();
        readonly BlockingQueue<ObjectUpdateInfo> m_BulletAddQueue = new BlockingQueue<ObjectUpdateInfo>();
        bool m_StopBulletThreads = false;

        void BulletUpdateThread()
        {
            Thread.CurrentThread.Name = "Bullet:Object:Update (" + m_Scene.RegionData.ID + ") Thread";
            while (!m_StopBulletThreads)
            {
                ObjectUpdateInfo info;
                try
                {
                    info = m_BulletUpdateQueue.Dequeue(1000);
                }
                catch
                {
                    continue;
                }

                if(info.IsKilled)
                {
                    Remove(info.Part);
                }
                else
                {
                    UpdateObject(info.Part);
                }
            }
        }

        void BulletAddThread()
        {
            Thread.CurrentThread.Name = "Bullet:Object:Add (" + m_Scene.RegionData.ID + ") Thread";
            while (!m_StopBulletThreads)
            {
                ObjectUpdateInfo info;
                try
                {
                    info = m_BulletAddQueue.Dequeue(1000);
                }
                catch
                {
                    continue;
                }

                /* most mesh calculations are done during addition, so we do this in separate threading */
                if (!info.IsKilled)
                {
                    Add(info.Part);
                }
            }
        }

        public void ScheduleUpdate(ObjectUpdateInfo info, UUID fromSceneID)
        {
            bool inScene;
            int serial = 0;
            lock(Primitives)
            {
                inScene = Primitives.Contains(info.Part.ID);
                if(inScene)
                {
                    serial = Primitives[info.Part.ID];
                    if(serial == info.SerialNumber)
                    {
                        return;
                    }
                }
            }
            if(inScene)
            {
                m_BulletUpdateQueue.Enqueue(info);
            }
            else
            {
                m_BulletAddQueue.Enqueue(info);
            }
        }
    }
}
