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

using log4net.Appender;
using log4net.Core;
using SilverSim.Threading;
using System;

namespace SilverSim.Main.Common.Log
{
    public class LogController : AppenderSkeleton
    {
        public readonly static RwLockedList<BlockingQueue<LoggingEvent>> Queues = new RwLockedList<BlockingQueue<LoggingEvent>>();
        public static event Action<DateTime, string, string, string> LogCallbacks;

        protected override void Append(LoggingEvent loggingEvent)
        {
            Queues.ForEach((BlockingQueue<LoggingEvent> q) => q.Enqueue(loggingEvent));
            foreach(Action<DateTime, string, string, string> d in LogCallbacks?.GetInvocationList() ?? new Delegate[0])
            {
                try
                {
                    d(loggingEvent.TimeStamp, loggingEvent.Level.Name, loggingEvent.LoggerName, loggingEvent.RenderedMessage);
                }
                catch
                {
                    /* ignore exceptions here. If not, we would end up in endless loops */
                }
            }
        }

        public static void AddLogAction(Action<DateTime, string, string, string> d)
        {
            LogCallbacks += d;
        }

        public static void RemoveLogAction(Action<DateTime, string, string, string> d)
        {
            LogCallbacks -= d;
        }
    }
}
