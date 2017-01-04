// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Threading;
using SilverSim.Types;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.ServiceInterfaces.AvatarName
{
    public class AggregatingAvatarNameService : AvatarNameServiceInterface
    {
        readonly RwLockedList<AvatarNameServiceInterface> m_ServiceList;

        public AggregatingAvatarNameService(RwLockedList<AvatarNameServiceInterface> serviceList)
        {
            m_ServiceList = serviceList;
        }

        public override bool TryGetValue(string firstName, string lastName, out UUI uui)
        {
            uui = null;
            bool notFoundFirst = false;
            foreach (AvatarNameServiceInterface service in m_ServiceList)
            {
                try
                {
                    if (service.TryGetValue(firstName, lastName, out uui))
                    {
                        if (!uui.IsAuthoritative)
                        {
                            notFoundFirst = true;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        notFoundFirst = true;
                    }
                }
                catch
                {
                    notFoundFirst = true;
                }
            }
            if (null == uui)
            {
                return false;
            }
            if (notFoundFirst && uui.IsAuthoritative)
            {
                foreach (AvatarNameServiceInterface service in m_ServiceList)
                {
                    try
                    {
                        service.Store(uui);
                    }
                    catch
                    {
                        /* ignore errors here */
                    }
                }
            }
            return true;
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public override UUI this[string firstName, string lastName]
        {
            get
            {
                UUI uui;
                if (!TryGetValue(firstName, lastName, out uui))
                {
                    throw new KeyNotFoundException();
                }
                return uui;
            }
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public override List<UUI> Search(string[] names)
        {
            Dictionary<UUID, UUI> results = new Dictionary<UUID, UUI>();
            foreach (AvatarNameServiceInterface service in m_ServiceList)
            {
                try
                {
                    foreach (UUI uui in service.Search(names))
                    {
                        if (!results.ContainsKey(uui.ID))
                        {
                            results.Add(uui.ID, uui);
                        }
                    }
                }
                catch
                {
                    /* no action required */
                }
            }
            return new List<UUI>(results.Values);
        }

        public override bool TryGetValue(UUID key, out UUI uui)
        {
            uui = null;
            bool notFoundFirst = false;
            foreach (AvatarNameServiceInterface service in m_ServiceList)
            {
                try
                {
                    if (service.TryGetValue(key, out uui))
                    {
                        uui = service[key];
                        if (!uui.IsAuthoritative)
                        {
                            notFoundFirst = true;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        notFoundFirst = true;
                    }
                }
                catch
                {
                    notFoundFirst = true;
                }
            }
            if (null == uui)
            {
                return false;
            }
            if (notFoundFirst && uui.IsAuthoritative)
            {
                foreach (AvatarNameServiceInterface service in m_ServiceList)
                {
                    try
                    {
                        service.Store(uui);
                    }
                    catch
                    {
                        /* ignore errors here */
                    }
                }
            }
            return true;
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public override UUI this[UUID key]
        {
            get
            {
                UUI uui = null;
                if (!TryGetValue(key, out uui))
                {
                    throw new KeyNotFoundException();
                }
                return uui;
            }
        }

        public override void Store(UUI uui)
        {
            foreach (AvatarNameServiceInterface service in m_ServiceList)
            {
                try
                {
                    service.Store(uui);
                }
                catch
                {
                    /* ignore errors here */
                }
            }
        }

        public override bool Remove(UUID key)
        {
            bool isRemoved = false;
            foreach (AvatarNameServiceInterface service in m_ServiceList)
            {
                try
                {
                    isRemoved = isRemoved || service.Remove(key);
                }
                catch
                {
                    /* ignore errors here */
                }
            }

            return isRemoved;
        }
    }
}
