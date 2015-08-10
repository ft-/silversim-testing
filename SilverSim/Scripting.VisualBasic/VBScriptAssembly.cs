// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
