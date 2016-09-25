// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Collections.Generic;

namespace SilverSim.Scene.Types.Script
{
    public interface IScriptCompilerRegistry
    {
        IScriptCompiler this[string name]
        {
            get;
            set;
        }

        IList<string> Names
        {
            get;
        }
    }
}
