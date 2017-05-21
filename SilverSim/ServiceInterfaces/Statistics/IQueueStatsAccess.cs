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

using System;
using System.Collections.Generic;

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
