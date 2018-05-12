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

using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using SilverSim.Types.StructuredData.Llsd;
using System.Collections.Generic;
using System.IO;

namespace SilverSim.Scene.Types.Physics.Vehicle
{
    public static class VehicleParamsSerialization
    {
        private readonly static Dictionary<string, VehicleRotationParamId> m_RotationParams = new Dictionary<string, VehicleRotationParamId>
        {
            ["ReferenceFrame"] = VehicleRotationParamId.ReferenceFrame
        };

        private readonly static Dictionary<string, VehicleVectorParamId> m_VectorParams = new Dictionary<string, VehicleVectorParamId>
        {
            ["AngularDeflectionEfficiency"] = VehicleVectorParamId.AngularDeflectionEfficiency,
            ["AngularDeflectionTimescale"] = VehicleVectorParamId.AngularDeflectionTimescale,
            ["LinearDeflectionEfficiency"] = VehicleVectorParamId.LinearDeflectionEfficiency,
            ["LinearDeflectionTimescale"] = VehicleVectorParamId.LinearDeflectionTimescale,
            ["VerticalAttractionEfficiency"] = VehicleVectorParamId.VerticalAttractionEfficiency,
            ["VerticalAttractionTimescale"] = VehicleVectorParamId.VerticalAttractionTimescale,
            ["AngularFrictionTimescale"] = VehicleVectorParamId.AngularFrictionTimescale,
            ["AngularMotorDirection"] = VehicleVectorParamId.AngularMotorDirection,
            ["LinearFrictionTimescale"] = VehicleVectorParamId.LinearFrictionTimescale,
            ["LinearMotorDirection"] = VehicleVectorParamId.LinearMotorDirection,
            ["LinearMotorOffset"] = VehicleVectorParamId.LinearMotorOffset,
            ["AngularMotorDecayTimescale"] = VehicleVectorParamId.AngularMotorDecayTimescale,
            ["AngularMotorTimescale"] = VehicleVectorParamId.AngularMotorTimescale,
            ["AngularMotorAccelPosTimescale"] = VehicleVectorParamId.AngularMotorAccelPosTimescale,
            ["AngularMotorDecelPosTimescale"] = VehicleVectorParamId.AngularMotorDecelPosTimescale,
            ["AngularMotorAccelNegTimescale"] = VehicleVectorParamId.AngularMotorAccelNegTimescale,
            ["AngularMotorDecelNegTimescale"] = VehicleVectorParamId.AngularMotorDecelNegTimescale,
            ["LinearMotorDecayTimescale"] = VehicleVectorParamId.LinearMotorDecayTimescale,
            ["LinearMotorTimescale"] = VehicleVectorParamId.LinearMotorTimescale,
            ["LinearMotorAccelPosTimescale"] = VehicleVectorParamId.LinearMotorAccelPosTimescale,
            ["LinearMotorDecelPosTimescale"] = VehicleVectorParamId.LinearMotorDecelPosTimescale,
            ["LinearMotorAccelNegTimescale"] = VehicleVectorParamId.LinearMotorAccelNegTimescale,
            ["LinearMotorDecelNegTimescale"] = VehicleVectorParamId.LinearMotorDecelNegTimescale,
            ["LinearWindEfficiency"] = VehicleVectorParamId.LinearWindEfficiency,
            ["AngularWindEfficiency"] = VehicleVectorParamId.AngularWindEfficiency,
            ["LinearMoveToTargetEfficiency"] = VehicleVectorParamId.LinearMoveToTargetEfficiency,
            ["LinearMoveToTargetTimescale"] = VehicleVectorParamId.LinearMoveToTargetTimescale,
            ["LinearMoveToTargetMaxOutput"] = VehicleVectorParamId.LinearMoveToTargetMaxOutput,
            ["AngularMoveToTargetEfficiency"] = VehicleVectorParamId.AngularMoveToTargetEfficiency,
            ["AngularMoveToTargetTimescale"] = VehicleVectorParamId.AngularMoveToTargetTimescale,
            ["AngularMoveToTargetMaxOutput"] = VehicleVectorParamId.AngularMoveToTargetMaxOutput
        };

        private readonly static Dictionary<string, VehicleFloatParamId> m_FloatParams = new Dictionary<string, VehicleFloatParamId>
        {
            ["BankingEfficiency"] = VehicleFloatParamId.BankingEfficiency,
            ["BankingMix"] = VehicleFloatParamId.BankingMix,
            ["BankingTimescale"] = VehicleFloatParamId.BankingTimescale,
            ["Buoyancy"] = VehicleFloatParamId.Buoyancy,
            ["HoverHeight"] = VehicleFloatParamId.HoverHeight,
            ["HoverEfficiency"] = VehicleFloatParamId.HoverEfficiency,
            ["HoverTimescale"] = VehicleFloatParamId.HoverTimescale,
            ["MouselookAzimuth"] = VehicleFloatParamId.MouselookAzimuth,
            ["MouselookAltitude"] = VehicleFloatParamId.MouselookAltitude,
            ["BankingAzimuth"] = VehicleFloatParamId.BankingAzimuth,
            ["DisableMotorsAbove"] = VehicleFloatParamId.DisableMotorsAbove,
            ["DisableMotorsAfter"] = VehicleFloatParamId.DisableMotorsAfter,
            ["InvertedBankingModifier"] = VehicleFloatParamId.InvertedBankingModifier,
            ["HeightExceededTime"] = VehicleFloatParamId.HeightExceededTime
        };

        public static void LoadFromVehicleSerialization(this ObjectPart part, byte[] data)
        {
            if(data == null || data.Length == 0)
            {
                return;
            }

            Map m;
            using (var ms = new MemoryStream(data))
            {
                m = (Map)LlsdBinary.Deserialize(ms);
            }

            int i;
            double f;
            Vector3 v;
            Quaternion q;
            if(m.TryGetValue("Type", out i))
            {
                part.VehicleType = (VehicleType)i;
            }
            if(m.TryGetValue("Flags", out i))
            {
                part.ClearVehicleFlags(~(VehicleFlags)i);
                part.SetVehicleFlags((VehicleFlags)i);
            }

            foreach (KeyValuePair<string, VehicleFloatParamId> kvp in m_FloatParams)
            {
                if(m.TryGetValue(kvp.Key, out f))
                {
                    part.VehicleParams[kvp.Value] = f;
                }
            }

            foreach(KeyValuePair<string, VehicleVectorParamId> kvp in m_VectorParams)
            {
                if(m.TryGetValue(kvp.Key, out v))
                {
                    part.VehicleParams[kvp.Value] = v;
                }
            }

            foreach (KeyValuePair<string, VehicleRotationParamId> kvp in m_RotationParams)
            {
                if(m.TryGetValue(kvp.Key, out q))
                {
                    part.VehicleParams[kvp.Value] = q;
                }
            }
        }
    }
}
