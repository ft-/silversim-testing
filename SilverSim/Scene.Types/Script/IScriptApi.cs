// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using System;

namespace SilverSim.Scene.Types.Script
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class ScriptApiName : Attribute
    {
        public readonly string Name;
        public ScriptApiName(string name)
        {
            Name = name;
        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
    public class ScriptEngineUsage : Attribute
    {
        public string Name;
        public ScriptEngineUsage(string name)
        {
            Name = name;
        }
    }

    public interface IScriptApi
    {
    }
}
