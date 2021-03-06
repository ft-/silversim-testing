﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using SilverSim.Scene.Types.Object;
using SilverSim.Types;

namespace SilverSim.Scene.Types.Physics
{
    public class PhysicsStateData
    {
        /* class here to improve update speed, (call by ref vs. call by value) */
        public UUID SceneID { get; protected set; }
        public Vector3 Position = Vector3.Zero;
        public Vector3 Velocity = Vector3.Zero;
        public Quaternion Rotation = Quaternion.Identity;
        public Vector3 AngularVelocity = Vector3.Zero;
        public Vector3 Acceleration = Vector3.Zero;
        public Vector3 AngularAcceleration = Vector3.Zero;

        /*
         * when reporting Vector4.UnitW, the viewer assumes to be on ground.
         * This will enable the feet to land logic
         * Collision plane is actually signaling whether the AV is standing on ground or somewhere else
         * Collision Plane contains a directional vector component for giving the feet a direction
         * and a height to define the distance of feet towards avatar.
         * X,Y,Z => Normal Vector Vector3.UnitZ => no angle
         * W => distance of feet toward avatar
         */
        public Vector4 CollisionPlane = new Vector4(0, 0, 1, -1);

        public BoundingBox BoundBox = new BoundingBox();

        /* inputs for mouselook steer */
        public Quaternion CameraRotation;
        public bool IsCameraDataValid;
        public bool IsAgentInMouselook;

        public PhysicsStateData(IObject obj, UUID sceneID)
        {
            SceneID = sceneID;
            Position = obj.Position;
            Rotation = obj.Rotation;
            Velocity = obj.Velocity;
            AngularVelocity = obj.AngularVelocity;
            Acceleration = obj.Acceleration;
            AngularAcceleration = obj.AngularAcceleration;
            obj.GetBoundingBox(out BoundBox);
        }
    }
}
