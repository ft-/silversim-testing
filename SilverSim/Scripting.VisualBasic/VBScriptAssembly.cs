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

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using System;
using System.Reflection;

namespace SilverSim.Scripting.VisualBasic
{
    public class VBScriptAssembly : IScriptAssembly
    {
        Assembly m_Assembly;
        Type m_ScriptType;

        public VBScriptAssembly(Assembly assembly, Type scriptType)
        {
            m_Assembly = assembly;
            m_ScriptType = scriptType;
        }

        public ScriptInstance Instantiate(ObjectPart objpart, ObjectPartInventoryItem item)
        {
            ConstructorInfo ci = m_ScriptType.GetConstructor(new Type[2] { typeof(ObjectPart), typeof(ObjectPartInventoryItem) });
            return (ScriptInstance)ci.Invoke(new object[] { objpart, item });
        }
    }
}
