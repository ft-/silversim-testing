// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net.Appender;
using log4net.Core;
using ThreadedClasses;

namespace SilverSim.Main.Common.Log
{
    public class LogController : AppenderSkeleton
    {
        public static RwLockedList<BlockingQueue<LoggingEvent>> Queues = new RwLockedList<BlockingQueue<LoggingEvent>>();

        protected override void Append(LoggingEvent loggingEvent)
        {
            Queues.ForEach(delegate(BlockingQueue<LoggingEvent> q)
            {
                q.Enqueue(loggingEvent);
            });
        }
    }
}
