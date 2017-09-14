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

#pragma warning disable IDE0018
#pragma warning disable RCS1029

using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Grid;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        private static readonly RwLockedDictionary<ulong, KeyValuePair<ulong, RegionInfo>> m_InterGridDestinations = new RwLockedDictionary<ulong, KeyValuePair<ulong, RegionInfo>>();
        private static readonly Random m_RandomNumber = new Random();
        private static readonly object m_RandomNumberLock = new object();

        private ushort NewInterGridRegionLocY
        {
            get
            {
                int rand;
                lock (m_RandomNumberLock)
                {
                    rand = m_RandomNumber.Next(1, 65535);
                }
                return (ushort)rand;
            }
        }

        private void CleanDestinationCache()
        {
            var regionHandles = new List<ulong>();
            foreach (var kvp in m_InterGridDestinations)
            {
                if (Date.GetUnixTime() - kvp.Value.Key > 240)
                {
                    regionHandles.Add(kvp.Key);
                }
            }
            foreach (ulong r in regionHandles)
            {
                m_InterGridDestinations.Remove(r);
            }
        }

        public GridVector CacheInterGridDestination(RegionInfo di)
        {
            CleanDestinationCache();
            var hgRegionHandle = new GridVector()
            {
                GridX = 0,
                GridY = NewInterGridRegionLocY
            };
            m_InterGridDestinations.Add(di.Location.RegionHandle, new KeyValuePair<ulong, RegionInfo>(Date.GetUnixTime(), di));
            return hgRegionHandle;
        }

        public bool TryGetDestination(GridVector gv, out RegionInfo di)
        {
            KeyValuePair<ulong, RegionInfo> dest;
            di = default(RegionInfo);
            if (m_InterGridDestinations.TryGetValue(gv.RegionHandle, out dest) &&
                Date.GetUnixTime() - dest.Key <= 240)
            {
                di = dest.Value;
                return true;
            }

            CleanDestinationCache();

            return false;
        }
    }
}
