// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Types.Script
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    public sealed class ScriptApiNameAttribute : Attribute
    {
        public string Name { get; private set; }

        public ScriptApiNameAttribute(string name)
        {
            Name = name;
        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
    [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    public sealed class ScriptEngineUsageAttribute : Attribute
    {
        public string Name { get; private set; }

        public ScriptEngineUsageAttribute(string name)
        {
            Name = name;
        }
    }

    public interface IScriptApi
    {
    }
}
