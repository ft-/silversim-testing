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
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using ThreadedClasses;
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
    }
}
