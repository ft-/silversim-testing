// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Types;

namespace SilverSim.Scene.Types.Script.Events
{
    public interface IScriptEvent
    {
    }

    public struct DetectInfo 
    {
        public IObject Object;
        public Vector3 GrabOffset;
        public int LinkNumber;
        public Vector3 TouchBinormal;
        public int TouchFace;
        public Vector3 TouchNormal;
        public Vector3 TouchPosition;
        public Vector3 TouchST;
        public Vector3 TouchUV;
    }
}
