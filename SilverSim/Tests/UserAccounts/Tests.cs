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
using SilverSim.ServiceInterfaces.Account;
using SilverSim.Tests.Extensions;
using SilverSim.Types;
using SilverSim.Types.Account;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SilverSim.Tests.UserAccounts
{
    public class Tests : ITest
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        UserAccountServiceInterface m_Service;
        UserAccountServiceInterface m_Backend;

        public void Startup(ConfigurationLoader loader)
        {
            IConfig config = loader.Config.Configs[GetType().FullName];
            m_Service = loader.GetService<UserAccountServiceInterface>(config.GetString("ServiceUnderTest"));
            /* we need the backend service so that we can create the entries we need for testing */
            m_Backend = loader.GetService<UserAccountServiceInterface>(config.GetString("Backend"));
        }

        bool CheckTestData(UserAccount ua)
        {
            bool result = true;
            if(ua.Principal.ID != UUID.Parse("22334455-1122-1122-1122-112233445566"))
            {
                m_Log.WarnFormat("UserAccount.Principal.ID does not match {0} != {1}", ua.Principal.ID, "22334455-1122-1122-1122-112233445566");
                result = false;
            }
            if(ua.ScopeID != UUID.Parse("33445566-1122-1122-1122-112233445566"))
            {
                m_Log.WarnFormat("UserAccount.ScopeID does not match {0} != {!}", ua.ScopeID, "33445566-1122-1122-1122-112233445566");
                result = false;
            }
            if(ua.Principal.FirstName != "First")
            {
                m_Log.WarnFormat("UserAccount.First does not match {0} != First", ua.Principal.FirstName);
                result = false;
            }
            if (ua.Principal.LastName != "Last")
            {
                m_Log.WarnFormat("UserAccount.Last does not match {0} != Last", ua.Principal.LastName);
                result = false;
            }
            if (ua.Email != "email@example.com" && ua.Email != "")
            {
                m_Log.WarnFormat("UserAccount.Email does not match {0} != email@example.com", ua.Email);
                result = false;
            }
            if(!ua.IsLocalToGrid)
            {
                m_Log.Warn("UserAccount.IsLocalToGrid is set wrong");
                result = false;
            }

            return result;
        }
        public bool Run()
        {
            m_Log.Info("Setting up test data");
            UserAccount ua = new UserAccount();
            UUID userID = UUID.Parse("22334455-1122-1122-1122-112233445566");
            UUID scopeID = UUID.Parse("33445566-1122-1122-1122-112233445566");
            ua.Principal.ID = userID;
            ua.Principal.FirstName = "First";
            ua.Principal.LastName = "Last";
            ua.Email = "email@example.com";
            ua.ScopeID = scopeID;
            /* DO NOT test HomeURI or ServiceURLs here, these are generated in a completely different code location */

            m_Backend.Add(ua);

            m_Log.Info("Testing retrieval by email");
            ua = m_Service[scopeID, "email@example.com"];
            if(!CheckTestData(ua))
            {
                return false;
            }

            m_Log.Info("Testing retrieval by ID");
            ua = m_Service[scopeID, userID];
            if (!CheckTestData(ua))
            {
                return false;
            }

            m_Log.Info("Testing retrieval by name");
            ua = m_Service[scopeID, "First", "Last"];
            if (!CheckTestData(ua))
            {
                return false;
            }

            return true;
        }
    }
}
