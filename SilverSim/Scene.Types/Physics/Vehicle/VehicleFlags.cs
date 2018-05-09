// SilverSim is distributed under the terms of the
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

using System;

namespace SilverSim.Scene.Types.Physics.Vehicle
{
    [Flags]
    public enum VehicleFlags : uint
    {
        None = 0,
        /** <summary>Prevent linear Z deflection on vehicle</summary> */
        NoDeflectionUp = 0x0001,
        /** <summary>Limit vertical attractor to roll</summary> */
        LimitRollOnly = 0x0002,
        /** <summary>Only hover above water height. Ignore terrain height.</summary> */
        HoverWaterOnly = 0x0004,
        /** <summary>Only hover above terrain height. Ignore water height.</summary> */
        HoverTerrainOnly = 0x0008,
        /** <summary>Hover at global height. Ignore water and terrain height.</summary> */
        HoverGlobalHeight = 0x0010,
        /** <summary>Hover pushes only up. It does not push down.</summary> */
        HoverUpOnly = 0x0020,
        /** <summary>Limit motor direction vector from lifting the vehicle into sky.</summary> */
        LimitMotorUp = 0x0040,
        /** <summary>Steer the vehicle using the mouse. Angular motor follows client camera direction.</summary> */
        MouselookSteer = 0x0080,
        /** <summary>Similar to MouselookSteer except that the left-right motion of camera is mapped to roll angle</summary> */
        MouselookBank = 0x0100,
        /** <summary>Make mouselook camera rotating independently of vehicle</summary> */
        CameraDecoupled = 0x0200,

        /* halcyon based extensions */
        /** <summary>vehicle reacts to water currents</summary> */
        ReactToCurrents = 0x10000,
        /** <summary>vehicle reacts to wind force</summary> */
        ReactToWind = 0x20000,
        /** <summary>Limit motor dirction vector from pushing the vehicle down</summary> */
        LimitMotorDown = 0x40000,
        TorqueWorldZ = 0x80000,
        MousePointSteer = 0x100000,
        MousePointBank = 0x200000,
    }
}
