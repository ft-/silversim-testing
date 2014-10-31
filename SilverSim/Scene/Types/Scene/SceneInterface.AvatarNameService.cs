/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

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
        private class DefaultAvatarNameService : AvatarNameServiceInterface
        {
            RwLockedList<AvatarNameServiceInterface> m_ServiceList;

            public DefaultAvatarNameService(RwLockedList<AvatarNameServiceInterface> serviceList)
            {
                m_ServiceList = serviceList;
            }

            public override NameData this[UUID key]
            {
                get
                {
                    NameData nd = null;
                    bool notFoundFirst = false;
                    foreach(AvatarNameServiceInterface service in m_ServiceList)
                    {
                        try
                        {
                            nd = service[key];
                            if(!nd.Authoritative)
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
                    if(notFoundFirst && nd.Authoritative)
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
