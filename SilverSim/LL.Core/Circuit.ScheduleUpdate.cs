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
using SilverSim.Scene.Types.Agent;

namespace SilverSim.LL.Core
{
    public partial class Circuit
    {
        private ThreadedClasses.NonblockingQueue<ObjectUpdateInfo> m_PhysicalOutQueue = new NonblockingQueue<ObjectUpdateInfo>();
        private ThreadedClasses.NonblockingQueue<ObjectUpdateInfo> m_NonPhysicalOutQueue = new NonblockingQueue<ObjectUpdateInfo>();
        private AutoResetEvent m_ObjectUpdateSignal = new AutoResetEvent(false);
        private bool m_TriggerFirstUpdate = false;

        public void ScheduleUpdate(ObjectUpdateInfo info)
        {
            AddScheduleUpdate(info);
            m_ObjectUpdateSignal.Set();
        }

        public void AddScheduleUpdate(ObjectUpdateInfo info)
        {
            if (info.Part.ObjectGroup.IsAttachedToPrivate && info.Part.ObjectGroup.Owner != Agent.Owner)
            {
                /* do not signal private attachments to anyone else than the owner */
            }
            else if (info.IsPhysics && !info.IsKilled && !info.Part.ObjectGroup.IsAttached)
            {
                m_PhysicalOutQueue.Enqueue(info);
            }
            else
            {
                m_NonPhysicalOutQueue.Enqueue(info);
            }
        }

        public void ScheduleFirstUpdate()
        {
            m_TriggerFirstUpdate = true;
            m_ObjectUpdateSignal.Set();
        }

        private void HandleObjectUpdates()
        {
            Dictionary<UInt32, int> LastObjSerialNo = new Dictionary<uint, int>();
            NonblockingQueue<ObjectUpdateInfo>[] queues = new NonblockingQueue<ObjectUpdateInfo>[2];
            queues[0] = m_PhysicalOutQueue;
            queues[1] = m_NonPhysicalOutQueue;

            while (m_ObjectUpdateThreadRunning)
            {
                if (m_PhysicalOutQueue.Count != 0 || m_NonPhysicalOutQueue.Count != 0)
                {
                }
                else if(!m_ObjectUpdateSignal.WaitOne(1000))
                {
                    continue;
                }

                if(m_TriggerFirstUpdate)
                {
                    foreach (IAgent agent in Scene.RootAgents)
                    {
                        if (agent != this)
                        {
                            Scene.SendAgentObjectToAgent(agent, Agent);
                        }
                    }
                    foreach (ObjectUpdateInfo ui in Scene.UpdateInfos)
                    {
                        AddScheduleUpdate(ui);
                    }
                    m_TriggerFirstUpdate = false;
                }

                Messages.Object.KillObject ko = null;
                Messages.Object.ObjectUpdate full_updatemsg = null;
                Messages.Object.ImprovedTerseObjectUpdate terse_updatemsg = null;
                Messages.Object.ObjectProperties prop_updatemsg = null;

                while (m_PhysicalOutQueue.Count != 0 || m_NonPhysicalOutQueue.Count != 0)
                {
                    foreach (NonblockingQueue<ObjectUpdateInfo> q in queues)
                    {
                        ObjectUpdateInfo ui;
                        if(q.Count == 0)
                        {
                            continue;
                        }
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
                                    if(ui.Part.Owner.ID == AgentID)
                                    {
                                        od.UpdateFlags |= Types.Primitive.PrimitiveFlags.ObjectYouOwner;
                                    }
                                    if (full_updatemsg == null)
                                    {
                                        full_updatemsg = new Messages.Object.ObjectUpdate();
                                        if(Scene == null)
                                        {
                                            /* kill the schedule thread when Scene has been cleared */
                                            return;
                                        }
                                        full_updatemsg.GridPosition = Scene.RegionData.Location;
                                        full_updatemsg.TimeDilation = 65535;
                                    }

                                    full_updatemsg.ObjectData.Add(od);
                                    if (full_updatemsg.ObjectData.Count == 3)
                                    {
                                        SendMessage(full_updatemsg);
                                        full_updatemsg = null;
                                    }
                                }

#if ONLY_SELECTED
                                Messages.Object.ObjectProperties.ObjData objprop = ui.SerializeObjProperties();
                                if (objprop != null)
                                {
                                    if (prop_updatemsg == null)
                                    {
                                        prop_updatemsg = new Messages.Object.ObjectProperties();
                                    }

                                    prop_updatemsg.ObjectData.Add(objprop);
                                    if (prop_updatemsg.ObjectData.Count == 3)
                                    {
                                        /* ObjectUpdate must be first */
                                        if (full_updatemsg != null)
                                        {
                                            SendMessage(full_updatemsg);
                                            full_updatemsg = null;
                                        }

                                        SendMessage(prop_updatemsg);
                                        prop_updatemsg = null;
                                    }
                                }
#endif
                            }
                            else
                            {
                                Messages.Object.ImprovedTerseObjectUpdate.ObjData od = ui.SerializeTerse();
                                if(od != null)
                                {
                                    Messages.Object.ImprovedTerseObjectUpdate m = new Messages.Object.ImprovedTerseObjectUpdate();
                                    m.ObjectData.Add(od);
                                    if (Scene == null)
                                    {
                                        /* kill the schedule thread when Scene has been cleared */
                                        return;
                                    }
                                    m.GridPosition = Scene.RegionData.Location;
                                    m.TimeDilation = 65535;
                                    SendMessage(m);
                                }
                            }
                        }
                    }
                }

                if (prop_updatemsg != null)
                {
                    SendMessage(prop_updatemsg);
                    prop_updatemsg = null;
                }
                if(full_updatemsg != null)
                {
                    SendMessage(full_updatemsg);
                    full_updatemsg = null;
                }
                if(terse_updatemsg != null)
                {
                    SendMessage(terse_updatemsg);
                    terse_updatemsg = null;
                }
                if(ko != null)
                {
                    SendMessage(ko);
                    ko = null;
                }
            }
        }
    }
}
