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

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Object;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
    {
        private class ObjectImageUpdateListEntry
        {
            public uint SequenceNumber;
            public long Timestamp;
        }

        private static readonly TimeProvider m_ObjectImageUpdateTimeSource = TimeProvider.StopWatch;
        private readonly Dictionary<UUID, ObjectImageUpdateListEntry> m_ObjectImageUpdates = new Dictionary<UUID, ObjectImageUpdateListEntry>();

        private void RemoveObjectImageUpdateLock(UUID id)
        {
            lock(m_ObjectImageUpdates)
            {
                m_ObjectImageUpdates.Remove(id);
            }
        }

        [PacketHandler(MessageType.ObjectImage)]
        public void HandleObjectImage(Message m)
        {
            var req = (ObjectImage)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }

            SceneInterface scene = Scene;
            ViewerAgent agent = Agent;
            if(scene == null)
            {
                return;
            }

            long tc = m_ObjectImageUpdateTimeSource.TickCount;
            long maxticks = m_ObjectImageUpdateTimeSource.SecsToTicks(60);

            foreach (ObjectImage.ObjectDataEntry data in req.ObjectData)
            {
#if DEBUG
                m_Log.DebugFormat("ObjectImage localid={0}", data.ObjectLocalID);
#endif

                ObjectPart part;
                if (!scene.Primitives.TryGetValue(data.ObjectLocalID, out part))
                {
                    continue;
                }
                if (!scene.CanEdit(agent, part.ObjectGroup, part.ObjectGroup.GlobalPosition))
                {
                    continue;
                }

                bool isValidUpdate = false;
                lock(m_ObjectImageUpdates)
                {
                    ObjectImageUpdateListEntry entry;
                    if (!m_ObjectImageUpdates.TryGetValue(part.ID, out entry))
                    {
                        isValidUpdate = true;
                        entry = new ObjectImageUpdateListEntry
                        {
                            Timestamp = tc,
                            SequenceNumber = m.CircuitSequenceNumber
                        };
                        m_ObjectImageUpdates.Add(part.ID, entry);
                    }
                    else if((int)(m.CircuitSequenceNumber - entry.SequenceNumber) > 0 ||
                        m_ObjectImageUpdateTimeSource.TicksElapsed(tc, entry.Timestamp) > maxticks)
                    {
                        isValidUpdate = true;
                        entry.Timestamp = tc;
                        entry.SequenceNumber = m.CircuitSequenceNumber;
                    }
                }

                if (isValidUpdate)
                {
                    part.TextureEntryBytes = data.TextureEntry;
                    part.MediaURL = data.MediaURL;
                }
            }
        }
    }
}
