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
using SilverSim.ServiceInterfaces.Avatar;
using SilverSim.Tests.Extensions;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SilverSim.Tests.Avatar
{
    public class ServiceTests : ITest
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        AvatarServiceInterface m_AvatarService;
        UUID m_UserID;

        public void Startup(ConfigurationLoader loader)
        {
            IConfig config = loader.Config.Configs[GetType().FullName];
            m_AvatarService = loader.GetService<AvatarServiceInterface>(config.GetString("AvatarService"));
            m_UserID = config.GetString("UserID");
        }

        public bool Run()
        {
            m_Log.Info("Testing that we get an empty set");
            Dictionary<string, string> result;

            result = m_AvatarService[m_UserID];
            if(result.Count != 0)
            {
                m_Log.Error("The result set is not empty");
                return false;
            }

            #region Add test data
            m_Log.Info("Add test data");
            m_AvatarService[m_UserID, "AvatarSerial"] = "1";
            m_AvatarService[m_UserID, "AvatarHeight"] = "1.8";

            m_Log.Info("Test that we get two items");
            result = m_AvatarService[m_UserID];
            if(result.Count != 2)
            {
                m_Log.ErrorFormat("Result set is not containing test data correctly. Count mismatch {0} != {1}", result.Count, 2);
                return false;
            }

            if (!result.ContainsKey("AvatarSerial") || !result.ContainsKey("AvatarHeight"))
            {
                m_Log.Error("Result set is not containing test data correctly. Key mismatch");
                return false;
            }

            if(result["AvatarSerial"] != "1")
            {
                m_Log.Error("Test data value mismatch. AvatarSerial");
                return false;
            }

            if (result["AvatarHeight"] != "1.8")
            {
                m_Log.Error("Test data value mismatch. AvatarHeight");
                return false;
            }
            #endregion

            #region Delete key test
            m_Log.Info("Delete one key");
            m_AvatarService.Remove(m_UserID, "AvatarHeight");
            result = m_AvatarService[m_UserID];
            if (result.Count != 1)
            {
                m_Log.ErrorFormat("Result set is not containing test data correctly. Count mismatch {0} != {1}", result.Count, 1);
                return false;
            }

            if (!result.ContainsKey("AvatarSerial") || result.ContainsKey("AvatarHeight"))
            {
                m_Log.Error("Result set is not containing test data correctly. Key mismatch");
                return false;
            }

            if (result["AvatarSerial"] != "1")
            {
                m_Log.Error("Test data value mismatch. AvatarSerial");
                return false;
            }
            #endregion

            #region Replace test 1
            m_Log.Info("Replacing complete set of data (Test 1)");
            result.Clear();
            result["AvatarSerial"] = "5";
            result["Avatar Weight"] = "1";
            m_AvatarService[m_UserID] = result;

            m_AvatarService.Remove(m_UserID, "AvatarHeight");
            result = m_AvatarService[m_UserID];
            if (result.Count != 2)
            {
                m_Log.ErrorFormat("Result set is not containing test data correctly. Count mismatch {0} != {1}", result.Count, 2);
                return false;
            }

            if (!result.ContainsKey("AvatarSerial") || result.ContainsKey("AvatarHeight") ||
                !result.ContainsKey("Avatar Weight"))
            {
                m_Log.Error("Result set is not containing test data correctly. Key mismatch");
                return false;
            }

            if (result["AvatarSerial"] != "5")
            {
                m_Log.Error("Test data value mismatch. AvatarSerial");
                return false;
            }

            if (result["Avatar Weight"] != "1")
            {
                m_Log.Error("Test data value mismatch. Avatar Weight");
                return false;
            }
            #endregion

            #region Replace test 2
            m_Log.Info("Replacing complete set of data (Test 2)");
            result.Clear();
            result["AvatarValue1"] = "Value1";
            result["AvatarValue2"] = "Value2";
            m_AvatarService[m_UserID] = result;

            m_AvatarService.Remove(m_UserID, "AvatarHeight");
            result = m_AvatarService[m_UserID];
            if (result.Count != 2)
            {
                m_Log.ErrorFormat("Result set is not containing test data correctly. Count mismatch {0} != {1}", result.Count, 2);
                return false;
            }

            if (result.ContainsKey("AvatarSerial") || result.ContainsKey("AvatarHeight") ||
                 result.ContainsKey("Avatar Weight") ||
                !result.ContainsKey("AvatarValue1") || !result.ContainsKey("AvatarValue2"))
            {
                m_Log.Error("Result set is not containing test data correctly. Key mismatch");
                return false;
            }

            if (result["AvatarValue1"] != "Value1")
            {
                m_Log.Error("Test data value mismatch. AvatarValue1");
                return false;
            }

            if (result["AvatarValue2"] != "Value2")
            {
                m_Log.Error("Test data value mismatch. AvatarValue2");
                return false;
            }
            #endregion

            #region Test Set items
            result.Clear();
            result["AvatarFirst"] = "First";
            result["AvatarLast"] = "Last";
            m_AvatarService[m_UserID, new List<string>(result.Keys)] = new List<string>(result.Values);

            result = m_AvatarService[m_UserID];
            if (result.Count != 4)
            {
                m_Log.ErrorFormat("Result set is not containing test data correctly. Count mismatch {0} != {1}", result.Count, 4);
                return false;
            }

            if (result.ContainsKey("AvatarSerial") || result.ContainsKey("AvatarHeight") ||
                 result.ContainsKey("Avatar Weight") ||
                !result.ContainsKey("AvatarValue1") || !result.ContainsKey("AvatarValue2") ||
                !result.ContainsKey("AvatarFirst") || !result.ContainsKey("AvatarLast"))
            {
                m_Log.Error("Result set is not containing test data correctly. Key mismatch");
                return false;
            }

            if (result["AvatarValue1"] != "Value1")
            {
                m_Log.Error("Test data value mismatch. AvatarValue1");
                return false;
            }

            if (result["AvatarValue2"] != "Value2")
            {
                m_Log.Error("Test data value mismatch. AvatarValue2");
                return false;
            }

            if (result["AvatarFirst"] != "First")
            {
                m_Log.Error("Test data value mismatch. AvatarFirst");
                return false;
            }

            if (result["AvatarLast"] != "Last")
            {
                m_Log.Error("Test data value mismatch. AvatarLast");
                return false;
            }
            #endregion

            #region Explicit get of items
            m_Log.Info("Test some explicit field gets");
            List<string> itemresult = m_AvatarService[m_UserID, new List<string>(new string[] { "AvatarFirst", "AvatarLast" })];
            if(itemresult.Count != 2)
            {
                m_Log.ErrorFormat("Test data value mismatch. Count mismatch {0} != {1}", itemresult.Count, 2);
                return false;
            }

            if (itemresult[0] != "First")
            {
                m_Log.Error("Test data value mismatch. AvatarFirst");
                return false;
            }

            if (itemresult[1] != "Last")
            {
                m_Log.Error("Test data value mismatch. AvatarLast");
                return false;
            }

            #endregion

            #region Explicit set of items
            m_Log.Info("Test some explicit set of items");
            m_AvatarService[m_UserID, new List<string>(new string[] { "AvatarSerial", "AvatarHeight" })] = new List<string>(new string[] { "2", "2.0"});

            result = m_AvatarService[m_UserID];
            if (result.Count != 6)
            {
                m_Log.ErrorFormat("Result set is not containing test data correctly. Count mismatch {0} != {1}", result.Count, 6);
                return false;
            }

            if (!result.ContainsKey("AvatarSerial") || !result.ContainsKey("AvatarHeight") ||
                 result.ContainsKey("Avatar Weight") ||
                !result.ContainsKey("AvatarValue1") || !result.ContainsKey("AvatarValue2") ||
                !result.ContainsKey("AvatarFirst") || !result.ContainsKey("AvatarLast"))
            {
                m_Log.Error("Result set is not containing test data correctly. Key mismatch");
                return false;
            }

            if (result["AvatarSerial"] != "2")
            {
                m_Log.Error("Test data value mismatch. AvatarSerial");
                return false;
            }

            if (result["AvatarHeight"] != "2.0")
            {
                m_Log.Error("Test data value mismatch. AvatarHeight");
                return false;
            }

            if (result["AvatarValue1"] != "Value1")
            {
                m_Log.Error("Test data value mismatch. AvatarValue1");
                return false;
            }

            if (result["AvatarValue2"] != "Value2")
            {
                m_Log.Error("Test data value mismatch. AvatarValue2");
                return false;
            }

            if (result["AvatarFirst"] != "First")
            {
                m_Log.Error("Test data value mismatch. AvatarFirst");
                return false;
            }

            if (result["AvatarLast"] != "Last")
            {
                m_Log.Error("Test data value mismatch. AvatarLast");
                return false;
            }
            #endregion

            #region Deleting all data
            m_Log.Info("Delete data");
            m_AvatarService[m_UserID] = null;

            result = m_AvatarService[m_UserID];
            if (result.Count != 0)
            {
                m_Log.ErrorFormat("Result set is not containing test data correctly. Count mismatch {0} != {1}", result.Count, 0);
                return false;
            }

            #endregion

            return true;
        }
    }
}
