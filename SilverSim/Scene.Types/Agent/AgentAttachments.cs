// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
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

        public AgentAttachments()
        {

        }

        public ObjectGroup this[UUID id]
        {
            get
            {
                m_AttachmentsRwLock.AcquireReaderLock(-1);
                try
                {
                    return m_AllAttachments[id];
                }
                finally
                {
                    m_AttachmentsRwLock.ReleaseReaderLock();
                }
            }
        }

        public List<ObjectGroup> this[AttachmentPoint ap]
        {
            get
            {
                m_AttachmentsRwLock.AcquireReaderLock(-1);
                try
                {
                    Dictionary<UUID, ObjectGroup> aplist;
                    if(m_AttachmentsPerPoint.TryGetValue(ap, out aplist))
                    {
                        return new List<ObjectGroup>(aplist.Values);
                    }
                    return new List<ObjectGroup>();
                }
                finally
                {
                    m_AttachmentsRwLock.ReleaseReaderLock();
                }
            }
        }

        public void Add(AttachmentPoint ap, ObjectGroup sog)
        {
            m_AttachmentsRwLock.AcquireWriterLock(-1);
            try
            {
                m_AllAttachments.Add(sog.ID, sog);
                if(!m_AttachmentsPerPoint.ContainsKey(ap))
                {
                    m_AttachmentsPerPoint.Add(ap, new Dictionary<UUID,ObjectGroup>());
                }
                m_AttachmentsPerPoint[ap].Add(sog.ID, sog);
                m_AttachedTo[sog.ID] = ap;
            }
            finally
            {
                m_AttachmentsRwLock.ReleaseWriterLock();
            }
        }

        public bool Remove(UUID sogid)
        {
            m_AttachmentsRwLock.AcquireWriterLock(-1);
            try
            {
                AttachmentPoint ap  = m_AttachedTo[sogid];
                m_AttachmentsPerPoint[ap].Remove(sogid);
                bool ret = m_AllAttachments.Remove(sogid);
                m_AttachedTo.Remove(sogid);
                return ret;
            }
            catch(KeyNotFoundException)
            {
                return false;
            }
            finally
            {
                m_AttachmentsRwLock.ReleaseWriterLock();
            }
        }

        public List<ObjectGroup> RemoveAll()
        {
            m_AttachmentsRwLock.AcquireWriterLock(-1);
            try
            {
                List<ObjectGroup> attachments = new List<ObjectGroup>(m_AllAttachments.Values);
                foreach(Dictionary<UUID, ObjectGroup> dict in m_AttachmentsPerPoint.Values)
                {
                    dict.Clear();
                }
                attachments.Clear();
                m_AllAttachments.Clear();
                return attachments;
            }
            finally
            {
                m_AttachmentsRwLock.ReleaseWriterLock();
            }
        }
    }
}
