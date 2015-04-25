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
using SilverSim.Main.Common.CmdIO;
using SilverSim.Scene.ServiceInterfaces.RegionLoader;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SilverSim.Tests.Extensions
{
    #region Implementation
    class TestRunner : IPlugin, IRegionLoaderInterface, IPluginSubFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("TEST RUNNER");
        List<ITest> m_Tests = new List<ITest>();
        TTY m_Console;

        public TestRunner()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {
            m_Console = loader.GetServicesByValue<TTY>()[0];
            if(loader.GetServicesByValue<TestRunner>().Count != 1)
            {
                throw new Exception("Too many TestRunner instances");
            }
            m_Tests = loader.GetServicesByValue<ITest>();
        }

        public void LoadRegions()
        {
        }

        public void AllRegionsLoaded()
        {
            bool failed = false;
            foreach(ITest test in m_Tests)
            {
                m_Log.Info("********************************************************************************");
                m_Log.InfoFormat("Executing test {0}", test.GetType().FullName);
                m_Log.Info("********************************************************************************");
                try
                {
                    if (test.Run())
                    {
                        m_Log.Info("********************************************************************************");
                        m_Log.InfoFormat("Executed test {0} with SUCCESS", test.GetType().FullName);
                        m_Log.Info("********************************************************************************");
                    }
                    else
                    {
                        m_Log.Info("********************************************************************************");
                        m_Log.ErrorFormat("Executed test {0} with FAILURE", test.GetType().FullName);
                        m_Log.Info("********************************************************************************");
                        failed = true;
                    }
                }
                catch(Exception e)
                {
                    m_Log.Info("********************************************************************************");
                    m_Log.InfoFormat("Executed test {0} with FAILURE", test.GetType().FullName);
                    m_Log.ErrorFormat("Exception {0}: {1}\n{2}", e.GetType().FullName, e.ToString(), e.StackTrace.ToString());
                    m_Log.Info("********************************************************************************");
                    failed = true;
                }
            }
            if(failed)
            {
                Thread.Sleep(100);
                throw new ConfigurationLoader.TestingError();
            }
            else
            {
                CommandRegistry.ExecuteCommand(new List<string> {"shutdown"}, m_Console);
            }
        }

        public void AddPlugins(ConfigurationLoader loader)
        {
            IConfig config = loader.Config.Configs["Tests"];
            if(config == null)
            {
                m_Log.Fatal("Nothing to test");
                throw new ConfigurationLoader.TestingError();
            }

            foreach(string testname in config.GetKeys())
            {
                Type t = GetType().Assembly.GetType(testname);
                if(t == null)
                {
                    m_Log.FatalFormat("Missing test {0}", testname);
                    throw new ConfigurationLoader.TestingError();
                }
                loader.AddPlugin(testname, (IPlugin)Activator.CreateInstance(t));
            }
        }
    }
    #endregion

    #region Factory
    [PluginName("TestRunner")]
    class TestRunnerFactory : IPluginFactory
    {
        public TestRunnerFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new TestRunner();
        }
    }
    #endregion
}
