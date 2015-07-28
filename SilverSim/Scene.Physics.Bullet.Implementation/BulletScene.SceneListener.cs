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

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
