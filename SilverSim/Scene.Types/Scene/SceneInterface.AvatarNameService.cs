// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.Types;
using ThreadedClasses;

namespace SilverSim.Scene.Types.Scene
{
    public abstract partial class SceneInterface
    {
        public class DefaultAvatarNameService : AvatarNameServiceInterface
        {
            RwLockedList<AvatarNameServiceInterface> m_ServiceList;

            internal DefaultAvatarNameService(RwLockedList<AvatarNameServiceInterface> serviceList)
            {
                m_ServiceList = serviceList;
            }

            public override UUI this[string firstName, string lastName]
            {
                get 
                {
                    UUI nd = null;
                    bool notFoundFirst = false;
                    foreach (AvatarNameServiceInterface service in m_ServiceList)
                    {
                        try
                        {
                            nd = service[firstName, lastName];
                            if (!nd.IsAuthoritative)
                            {
                                notFoundFirst = true;
                            }
                            else
                            {
                                break;
                            }
                        }
                        catch
                        {
                            notFoundFirst = true;
                        }
                    }
                    if (null == nd)
                    {
                        throw new KeyNotFoundException();
                    }
                    if (notFoundFirst && nd.IsAuthoritative)
                    {
                        foreach (AvatarNameServiceInterface service in m_ServiceList)
                        {
                            try
                            {
                                service[nd.ID] = nd;
                            }
                            catch
                            {
                                /* ignore errors here */
                            }
                        }
                    }
                    return nd;
                }
            }

            public override List<UUI> Search(string[] names)
            {
                Dictionary<UUID, UUI> results = new Dictionary<UUID, UUI>();
                foreach (AvatarNameServiceInterface service in m_ServiceList)
                {
                    try
                    {
                        foreach(UUI uui in service.Search(names))
                        {
                            if(!results.ContainsKey(uui.ID))
                            {
                                results.Add(uui.ID, uui);
                            }
                        }
                    }
                    catch
                    {

                    }
                }
                return new List<UUI>(results.Values);
            }

            public override UUI this[UUID key]
            {
                get
                {
                    UUI nd = null;
                    bool notFoundFirst = false;
                    foreach(AvatarNameServiceInterface service in m_ServiceList)
                    {
                        try
                        {
                            nd = service[key];
                            if(!nd.IsAuthoritative)
                            {
                                notFoundFirst = true;
                            }
                            else
                            {
                                break;
                            }
                        }
                        catch
                        {
                            notFoundFirst = true;
                        }
                    }
                    if(null == nd)
                    {
                        throw new KeyNotFoundException();
                    }
                    if(notFoundFirst && nd.IsAuthoritative)
                    {
                        foreach(AvatarNameServiceInterface service in m_ServiceList)
                        {
                            try
                            {
                                service[key] = nd;
                            }
                            catch
                            {
                                /* ignore errors here */
                            }
                        }
                    }
                    return nd;
                }
                set
                {
                    foreach (AvatarNameServiceInterface service in m_ServiceList)
                    {
                        try
                        {
                            service[key] = value;
                        }
                        catch
                        {
                            /* ignore errors here */
                        }
                    }
                }
            }
        }
    }
}
