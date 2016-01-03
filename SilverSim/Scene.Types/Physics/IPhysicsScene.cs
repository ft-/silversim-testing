// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Scene.Types.Object;

namespace SilverSim.Scene.Types.Physics
{
    public interface IPhysicsScene
    {
        void RemoveAll();
        void Add(IObject obj);
        void Remove(IObject obj);

        void Shutdown();

        double PhysicsFPS { get; }

        double PhysicsDilationTime { get; } /* in seconds */

        double PhysicsExecutionTime { get; } /* in seconds */

        uint PhysicsFrameNumber { get; }
    }
}
