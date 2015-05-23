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

using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.GridUser;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Tests.Extensions;
using SilverSim.Types;
using SilverSim.Types.GridUser;
using System;
using System.Reflection;

namespace SilverSim.Tests.ServerParams
{
    public class Tests : ITest
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        ServerParamServiceInterface m_Service;

        public void Startup(ConfigurationLoader loader)
        {
            IConfig config = loader.Config.Configs[GetType().FullName];
            m_Service = loader.GetService<ServerParamServiceInterface>(config.GetString("ServerParamService"));
        }

        public bool Run()
        {
            m_Log.Info("Testing default value facilities");
            UUID regionID = new UUID("11223344-1122-1122-1122-112233445566");
            string test1 = m_Service[regionID, "Param1", "test"];
            string test2 = m_Service[regionID, "Param1", "test"];
            if(test1 != "test")
            {
                m_Log.Warn("Result 1 of default value is not \"test\"");
                return false;
            }
            if(test2 != "test")
            {
                m_Log.Warn("Result 2 of default value is not \"test\"");
                return false;
            }

            m_Log.Info("Testing storing of UUID.Zero bound parameter");
            m_Service[UUID.Zero, "Param1"] = "param1";
            if(m_Service[UUID.Zero, "Param1"] != "param1")
            {
                m_Log.Warn("Value not stored");
                return false;
            }

            m_Log.Info("Testing value passing from UUID.Zero to region");
            if (m_Service[regionID, "Param1", "test"] != "param1")
            {
                m_Log.Warn("Value not passed to regionID");
                return false;
            }

            m_Log.Info("Testing value passing override to regionID when UUID.Zero is defined");
            m_Service[regionID, "Param1"] = "param2";
            if (m_Service[regionID, "Param1", "test"] != "param2")
            {
                m_Log.Warn("Value not overriden by regionID");
                return false;
            }

            m_Log.Info("Testing value passing after deletion of regionID param when UUID.Zero is defined");
            m_Service.Remove(regionID, "Param1");
            if (m_Service[regionID, "Param1", "test"] != "param1")
            {
                m_Log.Warn("Value not passed to regionID");
                return false;
            }

            m_Log.Info("Testing deletion of UUID.zero parameter");
            m_Service.Remove(UUID.Zero, "Param1");
            if (m_Service[regionID, "Param1", "test"] != "test")
            {
                m_Log.Warn("Value not returning to defvalue");
                return false;
            }

            /*
                    public abstract string this[UUID regionID, string parameter, string defvalue]
                    {
                        get;
                    }

                    public abstract string this[UUID regionID, string parameter]
                    {
                        get;
                        set;
                    }
             */
            return true;
        }
    }
}
