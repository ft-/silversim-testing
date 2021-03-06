﻿// SilverSim is distributed under the terms of the
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

using SilverSim.Scene.Types.Object;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Agent;
using System.Collections.Generic;
using System.Threading;

namespace SilverSim.Scene.Types.Agent
{
    public class AgentAttachments
    {
        private readonly ReaderWriterLock m_AttachmentsRwLock = new ReaderWriterLock();
        private readonly Dictionary<UUID, ObjectGroup> m_AllAttachments = new Dictionary<UUID,ObjectGroup>();
        private readonly Dictionary<UUID, AttachmentPoint> m_AttachedTo = new Dictionary<UUID,AttachmentPoint>();
        private readonly Dictionary<AttachmentPoint, Dictionary<UUID, ObjectGroup>> m_AttachmentsPerPoint = new Dictionary<AttachmentPoint,Dictionary<UUID,ObjectGroup>>();

        public ObjectGroup this[UUID id] => m_AttachmentsRwLock.AcquireReaderLock(() => m_AllAttachments[id]);

        public List<ObjectGroup> this[AttachmentPoint ap] => m_AttachmentsRwLock.AcquireReaderLock(() =>
        {
            Dictionary<UUID, ObjectGroup> aplist;
            if (m_AttachmentsPerPoint.TryGetValue(ap, out aplist))
            {
                return new List<ObjectGroup>(aplist.Values);
            }
            return new List<ObjectGroup>();
        });

        public bool TryGetValueByInventoryID(UUID itemID, out ObjectGroup grp)
        {
            grp = m_AttachmentsRwLock.AcquireReaderLock(() =>
            {
                foreach(ObjectGroup igrp in m_AllAttachments.Values)
                {
                    if(igrp.FromItemID == itemID)
                    {
                        return igrp;
                    }
                }
                return null;
            });
            return grp != null;
        }

        public bool TryGetValueByLocalID(UUID sceneID, uint localid, out ObjectGroup grp)
        {
            grp = m_AttachmentsRwLock.AcquireReaderLock(() =>
            {
                foreach (ObjectGroup igrp in m_AllAttachments.Values)
                {
                    if (igrp.LocalID[sceneID] == localid)
                    {
                        return igrp;
                    }
                }
                return null;
            });
            return grp != null;
        }

        public bool TryGetValue(UUID sogid, out ObjectGroup grp)
        {
            grp = m_AttachmentsRwLock.AcquireReaderLock(() =>
            {
                ObjectGroup igrp;
                bool res = m_AllAttachments.TryGetValue(sogid, out igrp);
                return res ? igrp : null;
            });
            return grp != null;
        }

        public void Add(AttachmentPoint ap, ObjectGroup sog) => m_AttachmentsRwLock.AcquireWriterLock(() =>
        {
            m_AllAttachments.Add(sog.ID, sog);
            if (!m_AttachmentsPerPoint.ContainsKey(ap))
            {
                m_AttachmentsPerPoint.Add(ap, new Dictionary<UUID, ObjectGroup>());
            }
            m_AttachmentsPerPoint[ap].Add(sog.ID, sog);
            m_AttachedTo[sog.ID] = ap;
        });

        public bool Remove(UUID sogid) => m_AttachmentsRwLock.AcquireWriterLock(() =>
        {
            try
            {
                AttachmentPoint ap = m_AttachedTo[sogid];
                m_AttachmentsPerPoint[ap].Remove(sogid);
                bool ret = m_AllAttachments.Remove(sogid);
                m_AttachedTo.Remove(sogid);
                return ret;
            }
            catch (KeyNotFoundException)
            {
                return false;
            }
        });

        public List<ObjectGroup> All => m_AttachmentsRwLock.AcquireReaderLock(() => new List<ObjectGroup>(m_AllAttachments.Values));

        public int Count => m_AttachmentsRwLock.AcquireReaderLock(() => m_AllAttachments.Count);

        public int AvailableSlots
        {
            get
            {
                int avail = 55 - Count;
                if(avail < 0)
                {
                    avail = 0;
                }
                return avail;
            }
        }

        public List<ObjectGroup> RemoveAll() => m_AttachmentsRwLock.AcquireWriterLock(() =>
        {
            var attachments = new List<ObjectGroup>(m_AllAttachments.Values);
            foreach (var dict in m_AttachmentsPerPoint.Values)
            {
                dict.Clear();
            }
            attachments.Clear();
            m_AllAttachments.Clear();
            return attachments;
        });
    }
}
