/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Physics.Vehicle;
using SilverSim.Scene.Types.Object;
using SilverSim.Types;

namespace SilverSim.Scene.Physics.Common
{
    public abstract class ObjectController : IPhysicsObject
    {
        protected ObjectPart m_Part;
        bool m_Phantom;
        bool m_ContributesToCollisionSurfaceAsChild;
        bool m_VolumeDetect;

        public ObjectController(ObjectPart part)
        {
            m_Part = part;
            m_Phantom = true;
            m_ContributesToCollisionSurfaceAsChild = false;
            m_VolumeDetect = false;
        }

        public abstract void UpdateCollisionInfo();

        public abstract Vector3 LinearVelocity { set; }
        public abstract Vector3 AngularVelocity { set; }
        public abstract bool IsPhysicsActive { get; set; } /* disables updates of object */
        public bool IsPhantom
        {
            get
            {
                return m_Phantom;
            }
            set
            {
                m_Phantom = value;
                UpdateCollisionInfo();
            }
        }

        public bool IsVolumeDetect 
        {
            get
            {
                return m_VolumeDetect;
            }
            set
            {
                m_VolumeDetect = value;
                UpdateCollisionInfo();
            }
        }

        public bool IsAgentCollisionActive 
        {
            get
            {
                return true;
            }

            set
            {

            }
        }

        public bool ContributesToCollisionSurfaceAsChild
        {
            get
            {
                return m_ContributesToCollisionSurfaceAsChild;
            }
            set
            {
                m_ContributesToCollisionSurfaceAsChild = value;
                UpdateCollisionInfo();
            }
        }

        #region Vehicle Calculation
        struct VehicleParams
        {
            public VehicleType VehicleType;

            public Quaternion ReferenceFrame;

            public Vector3 AngularFrictionTimescale;
            public Vector3 AngularMotorDirection;
            public Vector3 LinearFrictionTimescale;
            public Vector3 LinearMotorDirection;
            public Vector3 MotorOffset;

            public double AngularDeflectionEfficiency;
            public double AngularDeflectionTimescale;
            public double AngularMotorDecayTimescale;
            public double AngularMotorTimescale;
            public double BankingEfficiency;
            public double BankingMix;
            public double BankingTimescale;
            public double Buoyancy;
            public double HoverHeight;
            public double HoverEfficiency;
            public double HoverTimescale;
            public double LinearDeflectionEfficiency;
            public double LinearDeflectionTimescale;
            public double LinearMotorDecayTimescale;
            public double LinearMotorTimescale;
            public double VerticalAttractionEfficiency;
            public double VerticalAttractionTimescale;
            public VehicleFlags Flags;
        }
        VehicleParams m_Vehicle = new VehicleParams();

        public VehicleType VehicleType
        {
            get
            {
                return VehicleType.None;
            }
            set
            {
                lock(this)
                {
                    switch (value)
                    {
                        case VehicleType.None:
                            break;

                        case VehicleType.Sled:
                            m_Vehicle.LinearFrictionTimescale = new Vector3(30, 1, 1000);
                            m_Vehicle.AngularFrictionTimescale = new Vector3(1000, 1000, 1000);
                            m_Vehicle.LinearMotorDirection = new Vector3(0, 0, 0);
                            m_Vehicle.LinearMotorTimescale = 1000;
                            m_Vehicle.LinearMotorDecayTimescale = 120;
                            m_Vehicle.AngularMotorDirection = new Vector3(0, 0, 0);
                            m_Vehicle.AngularMotorTimescale = 1000;
                            m_Vehicle.AngularMotorDecayTimescale = 120;
                            m_Vehicle.HoverHeight = 0;
                            m_Vehicle.HoverEfficiency = 10;
                            m_Vehicle.HoverTimescale = 10;
                            m_Vehicle.Buoyancy = 0;
                            m_Vehicle.LinearDeflectionEfficiency = 1;
                            m_Vehicle.LinearDeflectionTimescale = 1;
                            m_Vehicle.AngularDeflectionEfficiency = 0;
                            m_Vehicle.AngularDeflectionTimescale = 10;
                            m_Vehicle.VerticalAttractionEfficiency = 1;
                            m_Vehicle.VerticalAttractionTimescale = 1000;
                            m_Vehicle.BankingEfficiency = 0;
                            m_Vehicle.BankingMix = 1;
                            m_Vehicle.BankingTimescale = 10;
                            m_Vehicle.ReferenceFrame = Quaternion.Identity;
                            m_Vehicle.Flags = VehicleFlags.NoDeflectionUp | VehicleFlags.LimitRollOnly | VehicleFlags.LimitMotorUp;
                            break;

                        case VehicleType.Car:
                            m_Vehicle.LinearFrictionTimescale = new Vector3(100, 2, 1000);
                            m_Vehicle.AngularFrictionTimescale = new Vector3(1000, 1000, 1000);
                            m_Vehicle.LinearMotorDirection = new Vector3(0, 0, 0);
                            m_Vehicle.LinearMotorTimescale = 1;
                            m_Vehicle.LinearMotorDecayTimescale = 60;
                            m_Vehicle.AngularMotorDirection = new Vector3(0, 0, 0);
                            m_Vehicle.AngularMotorTimescale = 1;
                            m_Vehicle.AngularMotorDecayTimescale = 0.8;
                            m_Vehicle.HoverHeight = 0;
                            m_Vehicle.HoverEfficiency = 0;
                            m_Vehicle.HoverTimescale = 1000;
                            m_Vehicle.Buoyancy = 0;
                            m_Vehicle.LinearDeflectionEfficiency = 1;
                            m_Vehicle.LinearDeflectionTimescale = 2;
                            m_Vehicle.AngularDeflectionEfficiency = 0;
                            m_Vehicle.AngularDeflectionTimescale = 10;
                            m_Vehicle.VerticalAttractionEfficiency = 1;
                            m_Vehicle.VerticalAttractionTimescale = 10;
                            m_Vehicle.BankingEfficiency = -0.2;
                            m_Vehicle.BankingMix = 1;
                            m_Vehicle.BankingTimescale = 1;
                            m_Vehicle.ReferenceFrame = Quaternion.Identity;
                            m_Vehicle.Flags = Types.Physics.Vehicle.VehicleFlags.NoDeflectionUp | Types.Physics.Vehicle.VehicleFlags.LimitRollOnly | Types.Physics.Vehicle.VehicleFlags.HoverUpOnly | Types.Physics.Vehicle.VehicleFlags.LimitMotorUp;
                            break;

                        case VehicleType.Boat:
                            m_Vehicle.LinearFrictionTimescale = new Vector3(10, 3, 2);
                            m_Vehicle.AngularFrictionTimescale = new Vector3(10, 10, 10);
                            m_Vehicle.LinearMotorDirection = new Vector3(0, 0, 0);
                            m_Vehicle.LinearMotorTimescale = 5;
                            m_Vehicle.LinearMotorDecayTimescale = 60;
                            m_Vehicle.AngularMotorDirection = new Vector3(0, 0, 0);
                            m_Vehicle.AngularMotorTimescale = 4;
                            m_Vehicle.AngularMotorDecayTimescale = 4;
                            m_Vehicle.HoverHeight = 0;
                            m_Vehicle.HoverEfficiency = 0.4;
                            m_Vehicle.HoverTimescale = 2;
                            m_Vehicle.Buoyancy = 1;
                            m_Vehicle.LinearDeflectionEfficiency = 0.5;
                            m_Vehicle.LinearDeflectionTimescale = 3;
                            m_Vehicle.AngularDeflectionEfficiency = 0.5;
                            m_Vehicle.AngularDeflectionTimescale = 5;
                            m_Vehicle.VerticalAttractionEfficiency = 0.5;
                            m_Vehicle.VerticalAttractionTimescale = 5;
                            m_Vehicle.BankingEfficiency = -0.3;
                            m_Vehicle.BankingMix = 0.8;
                            m_Vehicle.BankingTimescale = 1;
                            m_Vehicle.ReferenceFrame = Quaternion.Identity;
                            m_Vehicle.Flags = Types.Physics.Vehicle.VehicleFlags.NoDeflectionUp | Types.Physics.Vehicle.VehicleFlags.HoverWaterOnly | Types.Physics.Vehicle.VehicleFlags.HoverUpOnly | Types.Physics.Vehicle.VehicleFlags.LimitMotorUp;
                            break;

                        case VehicleType.Airplane:
                            m_Vehicle.LinearFrictionTimescale = new Vector3(200, 10, 5);
                            m_Vehicle.AngularFrictionTimescale = new Vector3(20, 20, 20);
                            m_Vehicle.LinearMotorDirection = new Vector3(0, 0, 0);
                            m_Vehicle.LinearMotorTimescale = 2;
                            m_Vehicle.LinearMotorDecayTimescale = 60;
                            m_Vehicle.AngularMotorDirection = new Vector3(0, 0, 0);
                            m_Vehicle.AngularMotorTimescale = 4;
                            m_Vehicle.AngularMotorDecayTimescale = 8;
                            m_Vehicle.HoverHeight = 0;
                            m_Vehicle.HoverEfficiency = 0.5;
                            m_Vehicle.HoverTimescale = 1000;
                            m_Vehicle.Buoyancy = 0;
                            m_Vehicle.LinearDeflectionEfficiency = 0.5;
                            m_Vehicle.LinearDeflectionTimescale = 0.5;
                            m_Vehicle.AngularDeflectionEfficiency = 1;
                            m_Vehicle.AngularDeflectionTimescale = 2;
                            m_Vehicle.VerticalAttractionEfficiency = 0.9;
                            m_Vehicle.VerticalAttractionTimescale = 2;
                            m_Vehicle.BankingEfficiency = 1;
                            m_Vehicle.BankingMix = 0.7;
                            m_Vehicle.BankingTimescale = 2;
                            m_Vehicle.ReferenceFrame = Quaternion.Identity;
                            m_Vehicle.Flags = Types.Physics.Vehicle.VehicleFlags.LimitRollOnly;
                            break;

                        case Types.Physics.Vehicle.VehicleType.Balloon:
                            m_Vehicle.LinearFrictionTimescale = new Vector3(5, 5, 5);
                            m_Vehicle.AngularFrictionTimescale = new Vector3(10, 10, 10);
                            m_Vehicle.LinearMotorDirection = new Vector3(0, 0, 0);
                            m_Vehicle.LinearMotorTimescale = 5;
                            m_Vehicle.LinearMotorDecayTimescale = 60;
                            m_Vehicle.AngularMotorDirection = new Vector3(0, 0, 0);
                            m_Vehicle.AngularMotorTimescale = 6;
                            m_Vehicle.AngularMotorDecayTimescale = 10;
                            m_Vehicle.HoverHeight = 5;
                            m_Vehicle.HoverEfficiency = 0.8;
                            m_Vehicle.HoverTimescale = 10;
                            m_Vehicle.Buoyancy = 1;
                            m_Vehicle.LinearDeflectionEfficiency = 0;
                            m_Vehicle.LinearDeflectionTimescale = 5;
                            m_Vehicle.AngularDeflectionEfficiency = 0;
                            m_Vehicle.AngularDeflectionTimescale = 5;
                            m_Vehicle.VerticalAttractionEfficiency = 1;
                            m_Vehicle.VerticalAttractionTimescale = 1000;
                            m_Vehicle.BankingEfficiency = 0;
                            m_Vehicle.BankingMix = 0.7;
                            m_Vehicle.BankingTimescale = 5;
                            m_Vehicle.ReferenceFrame = Quaternion.Identity;
                            m_Vehicle.Flags = Types.Physics.Vehicle.VehicleFlags.None;
                            break;

                        default:
                            throw new InvalidOperationException();
                    }
                    m_Vehicle.VehicleType = value;
                }
            }
        }

        public VehicleFlags VehicleFlags
        {
            get
            {
                return m_Vehicle.Flags;
            }
            set
            {
                lock (this)
                {
                    m_Vehicle.Flags = value;
                }
            }
        }

        public VehicleFlags SetVehicleFlags
        {
            set
            {
                lock(this)
                {
                    m_Vehicle.Flags |= value;
                }
            }
        }

        public VehicleFlags ClearVehicleFlags
        {
            set
            {
                lock(this)
                {
                    m_Vehicle.Flags &= (~value);
                }
            }
        }

        public Quaternion this[VehicleRotationParamId id]
        {
            get
            {
                return Quaternion.Identity;
            }
            set
            {

            }
        }

        public Vector3 this[VehicleVectorParamId id]
        {
            get
            {
                return Vector3.Zero;
            }
            set
            {
            }
        }

        public double this[VehicleFloatParamId id]
        {
            get
            {
                return 0;
            }
            set
            {
            }
        }

        public void Process(double dt)
        {
            if(IsPhysicsActive)
            {
                switch(m_Vehicle.VehicleType)
                {
                    case Types.Physics.Vehicle.VehicleType.None:
                        return;

                    case Types.Physics.Vehicle.VehicleType.Sled:
                        break;

                    case Types.Physics.Vehicle.VehicleType.Car:
                        break;

                    case Types.Physics.Vehicle.VehicleType.Boat:
                        break;

                    case Types.Physics.Vehicle.VehicleType.Airplane:
                        break;

                    case Types.Physics.Vehicle.VehicleType.Balloon:
                        break;

                    default:
                        break;
                }
            }
        }
        #endregion
    }
}
