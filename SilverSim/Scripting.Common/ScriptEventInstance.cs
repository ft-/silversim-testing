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
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Script;
using System;
using System.Globalization;

namespace SilverSim.Scripting.Common
{
    public abstract class ScriptEventInstance : ScriptInstance
    {
        readonly object m_Lock = new object();
        readonly NonblockingQueue<IScriptEvent> m_Events = new NonblockingQueue<IScriptEvent>();
        double m_ExecutionTime;

        protected ScriptEventInstance(ObjectPart part, ObjectPartInventoryItem item, byte[] state)
        {
            Part = part;
            Item = item;
        }

        public override double ExecutionTime
        {
            get
            {
                lock (m_Lock)
                {
                    return m_ExecutionTime;
                }
            }
            set
            {
                lock (m_Lock)
                {
                    m_ExecutionTime = value;
                }
            }
        }

        public override bool HasEventsPending => m_Events.Count != 0;

        public override bool IsLinkMessageReceiver => false;

        public override bool IsRunning { get; set; }

        public override ObjectPartInventoryItem Item { get; }

        public override ObjectPart Part { get; }

        public override IScriptState ScriptState => null;

        public override void PostEvent(IScriptEvent e)
        {
            if (IsRunning && !IsAborting)
            {
                m_Events.Enqueue(e);
                Part.ObjectGroup.Scene.ScriptThreadPool.PostScript(this);
            }
        }

        public override void ProcessEvent()
        {
            IScriptEvent evgot;
            try
            {
                evgot = m_Events.Dequeue();
            }
            catch
            {
                return;
            }
            int exectime;
            float execfloat;
            int startticks = Environment.TickCount;

            try
            {
                ProcessEvent(evgot);
            }
            finally
            {
                exectime = Environment.TickCount - startticks;
                execfloat = exectime / 1000f;
                lock (m_Lock)
                {
                    m_ExecutionTime += execfloat;
                }
            }
        }

        public abstract void ProcessEvent(IScriptEvent ev);

        public override void Remove()
        {
            /* nothing to do */
        }

        public override void Reset()
        {
            /* nothing to do */
        }

        public override void Start(int startparam = 0)
        {
            /* nothing to do */
        }

        public override void RevokePermissions(UUID permissionsKey, ScriptPermissions permissions)
        {
            /* nothing to do */
        }

        public override void ShoutError(IListenEventLocalization localizedMessage)
        {
        }

        public override void ShoutError(string msg)
        {
        }
    }
}
