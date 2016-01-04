// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public BoundingBox BoundBox = new BoundingBox();
        public double Mass;

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
            Mass = obj.PhysicsActor.Mass;
            obj.GetBoundingBox(out BoundBox);
        }
    }
}
