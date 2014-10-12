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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Scene.Types.Object;
using ThreadedClasses;
using System.Threading;

namespace SilverSim.LL.Core
{
    public partial class Circuit
    {
        private ThreadedClasses.NonblockingQueue<ObjectUpdateInfo> m_PhysicalOutQueue = new NonblockingQueue<ObjectUpdateInfo>();
        private ThreadedClasses.NonblockingQueue<ObjectUpdateInfo> m_NonPhysicalOutQueue = new NonblockingQueue<ObjectUpdateInfo>();
        private object m_ObjectUpdateSignal = new object();

        public void ScheduleUpdate(ObjectUpdateInfo info)
        {
            if(info.IsPhysics)
            {
                if(!info.IsKilled)
                {
                    m_PhysicalOutQueue.Enqueue(info);
                }
            }
            else
            {
                m_NonPhysicalOutQueue.Enqueue(info);
            }
        }

        private void HandleObjectUpdates(object para)
        {
            ThreadedClasses.BlockingQueue<ObjectUpdateInfo> queue = (ThreadedClasses.BlockingQueue<ObjectUpdateInfo>)para;
            Dictionary<UInt32, int> LastObjSerialNo = new Dictionary<uint, int>();
            NonblockingQueue<ObjectUpdateInfo>[] queues = new NonblockingQueue<ObjectUpdateInfo>[2];
            queues[0] = m_PhysicalOutQueue;
            queues[1] = m_NonPhysicalOutQueue;

            for(;;)
            {
                
                if(!Monitor.Wait(m_ObjectUpdateSignal, 5000))
                {
                    continue;
                }

                Messages.Object.KillObject ko = null;

                while (m_PhysicalOutQueue.Count != 0 && m_NonPhysicalOutQueue.Count != 0)
                {
                    foreach (NonblockingQueue<ObjectUpdateInfo> q in queues)
                    {
                        ObjectUpdateInfo ui;
                        try
                        {
                            ui = q.Dequeue();
                        }
                        catch
                        {
                            continue;
                        }

                        if(ui.IsKilled)
                        {
                            if(ko == null)
                            {
                                ko = new Messages.Object.KillObject();
                            }

                            ko.LocalIDs.Add(ui.LocalID);
                            if(ko.LocalIDs.Count > 250)
                            {
                                SendMessage(ko);
                                ko = null;
                            }
                        }
                        else
                        {
                            bool dofull = false;
                            try
                            {
                                int serialno = LastObjSerialNo[ui.LocalID];
                                dofull = serialno != ui.SerialNumber;
                            }
                            catch
                            {
                                dofull = true;
                            }

                            if(dofull)
                            {
                                Messages.Object.ObjectUpdate.ObjData od = ui.SerializeFull();
                                if(od != null)
                                {
                                    Messages.Object.ObjectUpdate m = new Messages.Object.ObjectUpdate();
                                    m.ObjectData.Add(od);
                                    m.TimeDilation = 65535;
                                    SendMessage(m);
                                }
                            }
                            else
                            {
                                Messages.Object.ImprovedTerseObjectUpdate.ObjData od = ui.SerializeTerse();
                                if(od != null)
                                {
                                    Messages.Object.ImprovedTerseObjectUpdate m = new Messages.Object.ImprovedTerseObjectUpdate();
                                    m.ObjectData.Add(od);
                                    m.TimeDilation = 65535;
                                    SendMessage(m);
                                }
                            }
                        }
                    }
                }

                if(ko != null)
                {
                    SendMessage(ko);
                }
            }
        }
    }
}
