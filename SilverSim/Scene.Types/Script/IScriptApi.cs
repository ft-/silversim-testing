// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Types.Script
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ScriptApiNameAttribute : Attribute
    {
        public string Name { get; private set; }

        public ScriptApiNameAttribute(string name)
        {
            Name = name;
        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ScriptEngineNameAttribute : Attribute
    {
        public string Name { get; private set; }

        public ScriptEngineNameAttribute(string name)
        {
            Name = name;
        }
    }

    public interface IScriptApi
    {
    }
}
