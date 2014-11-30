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
using SilverSim.Main.Common;
using Nini.Config;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types.Asset;
using System.Reflection;

namespace SilverSim.Scripting.LSL
{
    public class LSLCompiler : IScriptCompiler, IPlugin, IPluginSubFactory
    {
        List<ScriptApiFactory> m_Apis = new List<ScriptApiFactory>();

        public LSLCompiler()
        {

        }

        public void AddPlugins(ConfigurationLoader loader)
        {
            Type[] types = GetType().Assembly.GetTypes();
            foreach(Type type in types)
            {
                if (type.IsSubclassOf(typeof(ScriptApiFactory)))
                {
                    foreach(System.Attribute attr in System.Attribute.GetCustomAttributes(type))
                    {
                        if(attr is ScriptApiName)
                        {
                            ScriptApiFactory factory = (ScriptApiFactory)Activator.CreateInstance(type);
                            loader.AddPlugin(((ScriptApiName)attr).Name, factory);
                        }
                    }
                }
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
            List<ScriptApiFactory> apis = loader.GetServicesByValue<ScriptApiFactory>();
            foreach (ScriptApiFactory api in apis)
            {
                foreach (System.Attribute attr in System.Attribute.GetCustomAttributes(api.GetType()))
                {
                    if (attr is APILevel && !m_Apis.Contains(api))
                    {
                        m_Apis.Add(api);
                    }
                }
            }
        }

        public IScriptAssembly Compile(AssetData asset)
        {
            throw new NotImplementedException();
        }

    }

    public class Factory : IPluginFactory
    {
        public Factory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new LSLCompiler();
        }
    }
}
