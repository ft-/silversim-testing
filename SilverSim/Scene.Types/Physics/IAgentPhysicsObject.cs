// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Scene.Types.Physics
{
    public interface IAgentPhysicsObject : IPhysicsObject
    {
        bool IsAgentCollisionActive { get; set; }
        void SetControlTargetVelocity(Vector3 value);
    }
}
