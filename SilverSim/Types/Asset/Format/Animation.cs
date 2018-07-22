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
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SilverSim.Types.Asset.Format
{
    public sealed class Animation
    {
        private const short KEYFRAME_MOTION_VERSION = 1;
        private const short KEYFRAME_MOTION_SUBVERSION = 0;
        private const double MAX_PELVIS_OFFSET = 5.0;
        public const uint CHARACTER_MAX_ANIMATED_JOINTS = 216;

        public enum JointPriority
        {
            UseMotionPriority = -1,
            LowPriority = 0,
            MediumPriority = 1,
            HighPriority = 2,
            HigherPriority = 3,
            HighestPriority = 4,
            AdditivePriority = 5
        };

        public enum HandmotionType : uint
        {
            Spread,
            Relaxed,
            Point,
            Fist,
            RelaxedL,
            PointL,
            FistL,
            RelaxedR,
            PointR,
            FistR,
            SaluteR,
            Typing,
            PeaceR,
            PalmR
        }

        public class JointMotion
        {
            public JointPriority Priority;

            public struct RotationEntry
            {
                public double Timepoint;

                public Quaternion Rotation;
            }

            public struct PositionEntry
            {
                public double Timepoint;

                public Vector3 Position;
            }

            public readonly List<RotationEntry> Rotations = new List<RotationEntry>();
            public readonly List<PositionEntry> Positions = new List<PositionEntry>();
        }

        public readonly Dictionary<string, JointMotion> Motions = new Dictionary<string, JointMotion>();

        public class Constraint
        {
            public byte ChainLength;
            public byte ConstraintType;
            public byte[] SourceVolume = new byte[16];
            public byte[] TargetVolume = new byte[16];
            public Vector3 TargetOffset;
            public Vector3 TargetDirection;
            public double EaseInStart;
            public double EaseInStop;
            public double EaseOutStart;
            public double EaseOutStop;
        }

        public readonly List<Constraint> Constraints = new List<Constraint>();

        public JointPriority BasePriority { get; private set; }
        /** <summary>duration in seconds</summary> */
        public double Duration { get; private set; }
        public string EmoteName { get; private set; }
        public double LoopInPoint { get; private set; }
        public double LoopOutPoint { get; private set; }
        public int Loop { get; private set; }
        public double EaseInDuration { get; private set; }
        public double EaseOutDuration { get; private set; }
        public HandmotionType Handpose { get; private set; }

        public Animation(AssetData asset)
        {
            using (Stream s = asset.InputStream)
            {
                short version = ReadInt16(s);
                short subversion = ReadInt16(s);
                bool isOldVersion = false;

                if(version == 0 && subversion == 1)
                {
                    isOldVersion = true;
                }
                else if(version != KEYFRAME_MOTION_VERSION || subversion != KEYFRAME_MOTION_SUBVERSION)
                {
                    throw new NotAnAnimationFormatException();
                }

                int basePrio = ReadInt32(s);
                if(basePrio < (int)JointPriority.UseMotionPriority)
                {
                    throw new NotAnAnimationFormatException();
                }
                if(basePrio > (int)JointPriority.AdditivePriority)
                {
                    BasePriority = JointPriority.AdditivePriority - 1;
                }
                else
                {
                    BasePriority = (JointPriority)basePrio;
                }

                Duration = ReadFloat(s);
                EmoteName = ReadString(s);
                LoopInPoint = ReadFloat(s);
                LoopOutPoint = ReadFloat(s);
                Loop = ReadInt32(s);
                EaseInDuration = ReadFloat(s);
                EaseOutDuration = ReadFloat(s);
                uint handpose = ReadUInt32(s);
                if(!Enum.IsDefined(typeof(HandmotionType), handpose))
                {
                    throw new NotAnAnimationFormatException();
                }
                Handpose = (HandmotionType)handpose;

                uint num_motions = ReadUInt32(s);
                if(num_motions == 0 || num_motions > CHARACTER_MAX_ANIMATED_JOINTS)
                {
                    throw new NotAnAnimationFormatException();
                }

                for(uint motionidx = 0; motionidx < num_motions; ++motionidx)
                {
                    JointMotion motion = new JointMotion();
                    Motions.Add(ReadString(s), motion);
                    int jointPriority = ReadInt32(s);
                    if(jointPriority < (int)JointPriority.UseMotionPriority)
                    {
                        throw new NotAnAnimationFormatException();
                    }
                    motion.Priority = (JointPriority)jointPriority;

                    int num_rot_keys = ReadInt32(s);
                    if (num_rot_keys < 0)
                    {
                        throw new NotAnAnimationFormatException();
                    }

                    while(num_rot_keys-- != 0)
                    {
                        JointMotion.RotationEntry r = new JointMotion.RotationEntry();
                        double x;
                        double y;
                        double z;

                        if(isOldVersion)
                        {
                            r.Timepoint = ReadFloat(s);
                            x = ReadFloat(s) * Math.PI / 180.0;
                            y = ReadFloat(s) * Math.PI / 180.0;
                            z = ReadFloat(s) * Math.PI / 180.0;
                            Quaternion qx = Quaternion.CreateFromAxisAngle(Vector3.UnitX, x);
                            Quaternion qy = Quaternion.CreateFromAxisAngle(Vector3.UnitY, y);
                            Quaternion qz = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, z);
                            r.Rotation = qz * qy * qx;
                        }
                        else
                        {
                            r.Timepoint = 0.0.Lerp(Duration, ReadUInt16(s) / 65535.0);
                            x = (-1.0).Lerp(1.0, ReadUInt16(s) / 65535.0);
                            y = (-1.0).Lerp(1.0, ReadUInt16(s) / 65535.0);
                            z = (-1.0).Lerp(1.0, ReadUInt16(s) / 65535.0);
                            r.Rotation = new Quaternion(x, y, z);
                        }
                        motion.Rotations.Add(r);
                    }

                    int num_pos_keys = ReadInt32(s);
                    if(num_pos_keys < 0)
                    {
                        throw new NotAnAnimationFormatException();
                    }

                    while(num_pos_keys-- != 0)
                    {
                        JointMotion.PositionEntry p = new JointMotion.PositionEntry();
                        if(isOldVersion)
                        {
                            p.Timepoint = ReadFloat(s);
                            p.Position = ReadVector(s);
                        }
                        else
                        {
                            p.Timepoint = 0.0.Lerp(Duration, ReadUInt16(s) / 65535.0);
                            p.Position.X = (-MAX_PELVIS_OFFSET).Lerp(MAX_PELVIS_OFFSET, ReadUInt16(s) / 65535.0);
                            p.Position.Y = (-MAX_PELVIS_OFFSET).Lerp(MAX_PELVIS_OFFSET, ReadUInt16(s) / 65535.0);
                            p.Position.Z = (-MAX_PELVIS_OFFSET).Lerp(MAX_PELVIS_OFFSET, ReadUInt16(s) / 65535.0);
                        }
                        motion.Positions.Add(p);
                    }
                }

                int numconstraints = ReadInt32(s);
                if(numconstraints < 0)
                {
                    throw new NotAnAnimationFormatException();
                }

                while(numconstraints-- != 0)
                {
                    Constraints.Add(new Constraint
                    {
                        ChainLength = ReadByte(s),
                        ConstraintType = ReadByte(s),
                        SourceVolume = ReadBytes(s, 16),
                        TargetVolume = ReadBytes(s, 16),
                        TargetOffset = ReadVector(s),
                        TargetDirection = ReadVector(s),
                        EaseInStart = ReadFloat(s),
                        EaseInStop = ReadFloat(s),
                        EaseOutStart = ReadFloat(s),
                        EaseOutStop = ReadFloat(s)
                    });
                }
            }
        }

        private static byte[] ReadBytes(Stream s, int length)
        {
            byte[] b = new byte[length];
            if(b.Length != s.Read(b, 0, b.Length))
            {
                throw new NotAnAnimationFormatException();
            }
            return b;
        }

        private static byte ReadByte(Stream s)
        {
            int c = s.ReadByte();
            if(c < 0)
            {
                throw new NotAnAnimationFormatException();
            }
            return (byte)c;
        }

        private static string ReadString(Stream s)
        {
            var sb = new StringBuilder();
            for(; ;)
            {
                byte c = ReadByte(s);
                if (c == 0)
                {
                    return sb.ToString();
                }
                sb.Append((char)c);
            }
            throw new NotAnAnimationFormatException();
        }

        private static int ReadInt32(Stream s)
        {
            byte[] data = new byte[4];
            if (data.Length != s.Read(data, 0, data.Length))
            {
                throw new NotAnAnimationFormatException();
            }
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }
            return BitConverter.ToInt32(data, 0);
        }

        private static uint ReadUInt32(Stream s)
        {
            byte[] data = new byte[4];
            if (data.Length != s.Read(data, 0, data.Length))
            {
                throw new NotAnAnimationFormatException();
            }
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }
            return BitConverter.ToUInt32(data, 0);
        }

        private static short ReadInt16(Stream s)
        {
            byte[] data = new byte[2];
            if(data.Length != s.Read(data, 0, data.Length))
            {
                throw new NotAnAnimationFormatException();
            }
            if(!BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }
            return BitConverter.ToInt16(data, 0);
        }

        private static ushort ReadUInt16(Stream s)
        {
            byte[] data = new byte[2];
            if (data.Length != s.Read(data, 0, data.Length))
            {
                throw new NotAnAnimationFormatException();
            }
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }
            return BitConverter.ToUInt16(data, 0);
        }

        private static float ReadFloat(Stream s)
        {
            byte[] data = new byte[4];
            if(data.Length != s.Read(data, 0, data.Length))
            {
                throw new NotAnAnimationFormatException();
            }
            if(!BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }
            return BitConverter.ToSingle(data, 0);
        }

        private static Vector3 ReadVector(Stream s)
        {
            float x = ReadFloat(s);
            float y = ReadFloat(s);
            float z = ReadFloat(s);
            return new Vector3(x, y, z);
        }
    }
}
