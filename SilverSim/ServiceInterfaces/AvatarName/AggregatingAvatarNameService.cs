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

using SilverSim.Threading;
using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.AvatarName
{
    public class AggregatingAvatarNameService : AvatarNameServiceInterface
    {
        private readonly RwLockedList<AvatarNameServiceInterface> m_ServiceList;

        public AggregatingAvatarNameService(RwLockedList<AvatarNameServiceInterface> serviceList)
        {
            m_ServiceList = serviceList;
        }

        public override bool TryGetValue(string firstName, string lastName, out UGUIWithName uui)
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
            if (uui == null)
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

        public override List<UGUIWithName> Search(string[] names)
        {
            Dictionary<UUID, UGUIWithName> results = new Dictionary<UUID, UGUIWithName>();
            foreach (AvatarNameServiceInterface service in m_ServiceList)
            {
                try
                {
                    foreach (var uui in service.Search(names))
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
            return new List<UGUIWithName>(results.Values);
        }

        public override bool TryGetValue(UUID key, out UGUIWithName uui)
        {
            uui = null;
            bool notFoundFirst = false;
            foreach (var service in m_ServiceList)
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
            if (uui == null)
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

        public override void Store(UGUIWithName uui)
        {
            foreach (var service in m_ServiceList)
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
            foreach (var service in m_ServiceList)
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
