// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Scene.Types.KeyframedMotion
{
    public class Keyframe
    {
        public Vector3 TargetPosition { get; set; }
        public Quaternion TargetRotation { get; set; }
        public double Duration { get; set; }

        public Keyframe()
        {

        }
    }
}
