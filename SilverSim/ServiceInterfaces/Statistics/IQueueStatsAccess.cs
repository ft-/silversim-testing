using System;
using System.Collections.Generic;
// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.ServiceInterfaces.Statistics
{
    public struct QueueStat
    {
        public readonly string Status;
        public readonly int Count;
        public readonly uint Processed;

        public QueueStat(string status, int count, uint processed)
        {
            Status = status;
            Count = count;
            Processed = processed;
        }
    }
    public sealed class QueueStatAccessor
    {
        public readonly string Name;
        public readonly Func<QueueStat> GetData;

        public QueueStatAccessor(string name, Func<QueueStat> getCount)
        {
            Name = name;
            GetData = getCount;
        }
    }

    public interface IQueueStatsAccess
    {
        IList<QueueStatAccessor> QueueStats { get; }
    }
}
