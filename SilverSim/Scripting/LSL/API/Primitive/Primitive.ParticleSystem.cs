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

using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using SilverSim.Types.Primitive;
using System;

namespace SilverSim.Scripting.LSL.API.Primitive
{
    public partial class Primitive_API
    {
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_INTERP_COLOR_MASK = 1;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_INTERP_SCALE_MASK = 2;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_BOUNCE_MASK = 4;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_WIND_MASK = 8;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_FOLLOW_SRC_MASK = 16;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_FOLLOW_VELOCITY_MASK = 32;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_TARGET_POS_MASK = 64;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_TARGET_LINEAR_MASK = 128;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_EMISSIVE_MASK = 256;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_RIBBON_MASK = 1024;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_FLAGS = 0;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_START_COLOR = 1;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_START_ALPHA = 2;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_END_COLOR = 3;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_END_ALPHA = 4;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_START_SCALE = 5;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_END_SCALE = 6;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_MAX_AGE = 7;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_ACCEL = 8;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_PATTERN = 9;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_INNERANGLE = 10;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_OUTERANGLE = 11;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_TEXTURE = 12;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_BURST_RATE = 13;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_BURST_PART_COUNT = 15;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_BURST_RADIUS = 16;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_BURST_SPEED_MIN = 17;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_BURST_SPEED_MAX = 18;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_MAX_AGE = 19;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_TARGET_KEY = 20;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_OMEGA = 21;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_ANGLE_BEGIN = 22;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_ANGLE_END = 23;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_BLEND_FUNC_SOURCE = 24;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_BLEND_FUNC_DEST = 25;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_START_GLOW = 26;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_END_GLOW = 27;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_BF_ONE = 0;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_BF_ZERO = 1;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_BF_DEST_COLOR = 2;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_BF_SOURCE_COLOR = 3;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_BF_ONE_MINUS_DEST_COLOR = 4;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_BF_ONE_MINUS_SOURCE_COLOR = 5;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_BF_SOURCE_ALPHA = 7;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_BF_ONE_MINUS_SOURCE_ALPHA = 9;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_PATTERN_DROP = 1;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_PATTERN_EXPLODE = 2;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_PATTERN_ANGLE = 4;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_PATTERN_ANGLE_CONE = 8;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_PATTERN_ANGLE_CONE_EMPTY = 16;

        private float ValidParticleScale(double value)
        {
            if (value > 4.0f) return 4.0f;
            if (value < 0f) return 0f;
            return (float)value;
        }

        [APILevel(APIFlags.LSL)]
        public void llLinkParticleSystem(int link, AnArray rules)
        {
            ParticleSystem ps = new ParticleSystem();

            float tempf = 0;
            int tmpi = 0;
            Vector3 tempv;

            for (int i = 0; i < rules.Count; i += 2)
            {
                int psystype;
                try
                {
                    psystype = rules[i].AsInteger;
                }
                catch (InvalidCastException)
                {
                    Instance.ShoutError(string.Format("Error running particle system params index #{0}: particle system parameter type must be integer", i));
                    return;
                }
                IValue value = rules[i + 1];
                switch (psystype)
                {
                    case PSYS_PART_FLAGS:
                        try
                        {
                            ps.PartDataFlags = (ParticleSystem.ParticleDataFlags)value.AsUInt;
                        }
                        catch (InvalidCastException)
                        {
                            Instance.ShoutError(string.Format("Error running rule PSYS_PART_FLAGS: arg #{0} - parameter 1 must be integer", i + 1));
                            return;
                        }
                        break;

                    case PSYS_PART_START_COLOR:
                        try
                        {
                            tempv = value.AsVector3;
                        }
                        catch (InvalidCastException)
                        {
                            Instance.ShoutError(string.Format("Error running rule PSYS_PART_START_COLOR: arg #{0} - parameter 1 must be vector", i + 1));
                            return;
                        }
                        ps.PartStartColor.R = tempv.X;
                        ps.PartStartColor.G = tempv.Y;
                        ps.PartStartColor.B = tempv.Z;
                        break;

                    case PSYS_PART_START_ALPHA:
                        try
                        {
                            ps.PartStartColor.A = value.AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            Instance.ShoutError(string.Format("Error running rule PSYS_PART_START_ALPHA: arg #{0} - parameter 1 must be float", i + 1));
                            return;
                        }
                        break;

                    case PSYS_PART_END_COLOR:
                        try
                        {
                            tempv = value.AsVector3;
                        }
                        catch (InvalidCastException)
                        {
                            Instance.ShoutError(string.Format("Error running rule PSYS_PART_END_COLOR: arg #{0} - parameter 1 must be vector", i + 1));
                            return;
                        }
                        ps.PartEndColor.R = tempv.X;
                        ps.PartEndColor.G = tempv.Y;
                        ps.PartEndColor.B = tempv.Z;
                        break;

                    case PSYS_PART_END_ALPHA:
                        try
                        {
                            ps.PartEndColor.A = value.AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            Instance.ShoutError(string.Format("Error running rule PSYS_PART_END_ALPHA: arg #{0} - parameter 1 must be float", i + 1));
                            return;
                        }
                        break;

                    case PSYS_PART_START_SCALE:
                        try
                        {
                            tempv = value.AsVector3;
                        }
                        catch (InvalidCastException)
                        {
                            Instance.ShoutError(string.Format("Error running rule PSYS_PART_START_SCALE: arg #{0} - parameter 1 must be vector", i + 1));
                            return;
                        }
                        ps.PartStartScaleX = ValidParticleScale(tempv.X);
                        ps.PartStartScaleY = ValidParticleScale(tempv.Y);
                        break;

                    case PSYS_PART_END_SCALE:
                        try
                        {
                            tempv = value.AsVector3;
                        }
                        catch (InvalidCastException)
                        {
                            Instance.ShoutError(string.Format("Error running rule PSYS_PART_END_SCALE: arg #{0} - parameter 1 must be vector", i + 1));
                            return;
                        }
                        ps.PartEndScaleX = ValidParticleScale(tempv.X);
                        ps.PartEndScaleY = ValidParticleScale(tempv.Y);
                        break;

                    case PSYS_PART_MAX_AGE:
                        try
                        {
                            ps.PartMaxAge = (float)value.AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            Instance.ShoutError(string.Format("Error running rule PSYS_PART_MAX_AGE: arg #{0} - parameter 1 must be float", i + 1));
                            return;
                        }
                        break;

                    case PSYS_SRC_ACCEL:
                        try
                        {
                            tempv = value.AsVector3;
                        }
                        catch (InvalidCastException)
                        {
                            Instance.ShoutError(string.Format("Error running rule PSYS_SRC_ACCEL: arg #{0} - parameter 1 must be vector", i + 1));
                            return;
                        }
                        ps.PartAcceleration.X = tempv.X;
                        ps.PartAcceleration.Y = tempv.Y;
                        ps.PartAcceleration.Z = tempv.Z;
                        break;

                    case PSYS_SRC_PATTERN:
                        try
                        {
                            ps.Pattern = (ParticleSystem.SourcePattern)value.AsInt;
                        }
                        catch (InvalidCastException)
                        {
                            Instance.ShoutError(string.Format("Error running rule PSYS_SRC_PATTERN: arg #{0} - parameter 1 must be integer", i + 1));
                            return;
                        }
                        break;

                    // PSYS_SRC_INNERANGLE and PSYS_SRC_ANGLE_BEGIN use the same variables. The
                    // PSYS_SRC_OUTERANGLE and PSYS_SRC_ANGLE_END also use the same variable. The
                    // client tells the difference between the two by looking at the 0x02 bit in
                    // the PartFlags variable.
                    case PSYS_SRC_INNERANGLE:
                        try
                        {
                            tempf = (float)value.AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            Instance.ShoutError(string.Format("Error running rule PSYS_SRC_INNERANGLE: arg #{0} - parameter 1 must be float", i + 1));
                            return;
                        }
                        ps.InnerAngle = tempf;
                        ps.PartFlags &= 0xFFFFFFFD; // Make sure new angle format is off.
                        break;

                    case PSYS_SRC_OUTERANGLE:
                        try
                        {
                            tempf = (float)value.AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            Instance.ShoutError(string.Format("Error running rule PSYS_SRC_OUTERANGLE: arg #{0} - parameter 1 must be float", i + 1));
                            return;
                        }
                        ps.OuterAngle = tempf;
                        ps.PartFlags &= 0xFFFFFFFD; // Make sure new angle format is off.
                        break;

                    case PSYS_PART_BLEND_FUNC_SOURCE:
                        try
                        {
                            tmpi = value.AsInt;
                        }
                        catch (InvalidCastException)
                        {
                            Instance.ShoutError(string.Format("Error running rule PSYS_PART_BLEND_FUNC_SOURCE: arg #{0} - parameter 1 must be integer", i + 1));
                            return;
                        }
                        ps.BlendFuncSource = (ParticleSystem.BlendFunc)tmpi;
                        break;

                    case PSYS_PART_BLEND_FUNC_DEST:
                        try
                        {
                            tmpi = value.AsInt;
                        }
                        catch (InvalidCastException)
                        {
                            Instance.ShoutError(string.Format("Error running rule PSYS_PART_BLEND_FUNC_DEST: arg #{0} - parameter 1 must be integer", i + 1));
                            return;
                        }
                        ps.BlendFuncDest = (ParticleSystem.BlendFunc)tmpi;
                        break;

                    case PSYS_PART_START_GLOW:
                        try
                        {
                            tempf = (float)value.AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            Instance.ShoutError(string.Format("Error running rule PSYS_PART_START_GLOW: arg #{0} - parameter 1 must be float", i + 1));
                            return;
                        }
                        ps.PartStartGlow = tempf;
                        break;

                    case PSYS_PART_END_GLOW:
                        try
                        {
                            tempf = (float)value.AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            Instance.ShoutError(string.Format("Error running rule PSYS_PART_END_GLOW: arg #{0} - parameter 1 must be float", i + 1));
                            return;
                        }
                        ps.PartEndGlow = tempf;
                        break;

                    case PSYS_SRC_TEXTURE:
                        try
                        {
                            ps.Texture = getTextureAssetID(value.ToString());
                        }
                        catch(InvalidOperationException)
                        {
                            Instance.ShoutError(string.Format("Error running rule PSYS_SRC_TEXTURE: arg #{0} - parameter 1 must be refering to a texture (either inventory name or texture ID)", i + 1));
                            return;
                        }
                        catch (InvalidCastException)
                        {
                            Instance.ShoutError(string.Format("Error running rule PSYS_SRC_TEXTURE: arg #{0} - parameter 1 must be string or key", i + 1));
                            return;
                        }
                        break;

                    case PSYS_SRC_BURST_RATE:
                        try
                        {
                            ps.BurstRate = (float)value.AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            Instance.ShoutError(string.Format("Error running rule PSYS_SRC_BURST_RATE: arg #{0} - parameter 1 must be float", i + 1));
                            return;
                        }
                        break;

                    case PSYS_SRC_BURST_PART_COUNT:
                        try
                        {
                            ps.BurstPartCount = (byte)value.AsInt;
                        }
                        catch (InvalidCastException)
                        {
                            Instance.ShoutError(string.Format("Error running rule PSYS_SRC_BURST_PART_COUNT: arg #{0} - parameter 1 must be integer", i + 1));
                            return;
                        }
                        break;

                    case PSYS_SRC_BURST_RADIUS:
                        try
                        {
                            ps.BurstRadius = (float)value.AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            Instance.ShoutError(string.Format("Error running rule PSYS_SRC_BURST_RADIUS: arg #{0} - parameter 1 must be float", i + 1));
                            return;
                        }
                        break;

                    case PSYS_SRC_BURST_SPEED_MIN:
                        try
                        {
                            ps.BurstSpeedMin = (float)value.AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            Instance.ShoutError(string.Format("Error running rule PSYS_SRC_BURST_SPEED_MIN: arg #{0} - parameter 1 must be float", i + 1));
                            return;
                        }
                        break;

                    case PSYS_SRC_BURST_SPEED_MAX:
                        try
                        {
                            ps.BurstSpeedMax = (float)value.AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            Instance.ShoutError(string.Format("Error running rule PSYS_SRC_BURST_SPEED_MAX: arg #{0} - parameter 1 must be float", i + 1));
                            return;
                        }
                        break;

                    case PSYS_SRC_MAX_AGE:
                        try
                        {
                            ps.MaxAge = (float)value.AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            Instance.ShoutError(string.Format("Error running rule PSYS_SRC_MAX_AGE: arg #{0} - parameter 1 must be float", i + 1));
                            return;
                        }
                        break;

                    case PSYS_SRC_TARGET_KEY:
                        UUID key = UUID.Zero;
                        if (UUID.TryParse(value.ToString(), out key))
                        {
                            ps.Target = key;
                        }
                        else
                        {
                            ps.Target = Part.ID;
                        }
                        break;

                    case PSYS_SRC_OMEGA:
                        try
                        {
                            ps.AngularVelocity = value.AsVector3;
                        }
                        catch (InvalidCastException)
                        {
                            Instance.ShoutError(string.Format("Error running rule PSYS_SRC_OMEGA: arg #{0} - parameter 1 must be vector", i + 1));
                            return;
                        }
                        break;

                    case PSYS_SRC_ANGLE_BEGIN:
                        try
                        {
                            tempf = (float)value.AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            Instance.ShoutError(string.Format("Error running rule PSYS_SRC_ANGLE_BEGIN: arg #{0} - parameter 1 must be float", i + 1));
                            return;
                        }
                        ps.InnerAngle = tempf;
                        ps.PartFlags |= 0x02; // Set new angle format.
                        break;

                    case PSYS_SRC_ANGLE_END:
                        try
                        {
                            tempf = (float)value.AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            Instance.ShoutError(string.Format("Error running rule PSYS_SRC_ANGLE_END: arg #{0} - parameter 1 must be float", i + 1));
                            return;
                        }
                        ps.OuterAngle = tempf;
                        ps.PartFlags |= 0x02; // Set new angle format.
                        break;
                }

            }
            ps.CRC = 1;

            lock (Instance)
            {
                foreach (ObjectPart part in GetLinkTargets(link))
                {
                    part.ParticleSystem = ps;
                }
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llParticleSystem(AnArray rules)
        {
            llLinkParticleSystem(LINK_THIS, rules);
        }
    }
}
