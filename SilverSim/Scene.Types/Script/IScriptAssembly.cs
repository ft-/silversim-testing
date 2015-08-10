// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;

namespace SilverSim.Scene.Types.Script
{
    public interface IScriptAssembly
    {
        ScriptInstance Instantiate(ObjectPart objpart, ObjectPartInventoryItem item);
    }
}
