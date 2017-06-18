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

using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace SilverSim.Scene.Types.KeyframedMotion.Serialization
{
    public static class KfOpenSim
    {
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        private struct OsVector3
        {
            /// <summary>X value</summary>
            public float X;
            /// <summary>Y value</summary>
            public float Y;
            /// <summary>Z value</summary>
            public float Z;

            public static implicit operator Vector3(OsVector3 v) =>
                new Vector3(v.X, v.Y, v.Z);

            public static explicit operator OsVector3(Vector3 v) =>
                new OsVector3 { X = (float)v.X, Y = (float)v.Y, Z = (float)v.Z };
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        private struct OsQuaternion
        {
            /// <summary>X value</summary>
            public float X;
            /// <summary>Y value</summary>
            public float Y;
            /// <summary>Z value</summary>
            public float Z;
            /// <summary>W value</summary>
            public float W;

            public static implicit operator Quaternion(OsQuaternion q) =>
                new Quaternion(q.X, q.Y, q.Z, q.W);

            public static explicit operator OsQuaternion(Quaternion q) =>
                new OsQuaternion { X = (float)q.X, Y = (float)q.Y, Z = (float)q.Z, W = (float)q.W };

            public static readonly OsQuaternion Identity = new OsQuaternion { X = 0, Y = 0, Z = 0, W = 1 };
        }

        [Serializable]
        private sealed class OsKeyframeMotion
        {
            //private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            public enum PlayMode : int
            {
                Forward = 0,
                Reverse = 1,
                Loop = 2,
                PingPong = 3
            };

            [Flags]
            public enum DataFormat : int
            {
                Translation = 2,
                Rotation = 1
            }

            [Serializable]
            public struct Keyframe
            {
                public OsVector3? Position;
                public OsQuaternion? Rotation;
                public OsQuaternion StartRotation;
                public int TimeMS;
                public int TimeTotal;
                public OsVector3 AngularVelocity;
                public OsVector3 StartPosition;
            };

            public OsVector3 m_serializedPosition;
            public OsVector3 m_basePosition;
            public OsQuaternion m_baseRotation;

            public Keyframe m_currentFrame;

            public List<Keyframe> m_frames = new List<Keyframe>();

            public Keyframe[] m_keyframes;

            public PlayMode m_mode = PlayMode.Forward;
            public DataFormat m_data = DataFormat.Translation | DataFormat.Rotation;

            public bool m_running = false;

            public int m_iterations = 0;

            public int m_skipLoops = 0;
        }

        private sealed class OsKfSerializationBinder : SerializationBinder
        {
            public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                assemblyName = "OpenSim.Region.Framework";
                if (serializedType == typeof(OsVector3))
                {
                    assemblyName = "OpenMetaverse";
                    typeName = "OpenMetaverse.Vector3";
                }
                else if (serializedType == typeof(OsQuaternion))
                {
                    assemblyName = "OpenMetaverse";
                    typeName = "OpenMetaverse.Quaternion";
                }
                else if (serializedType == typeof(OsKeyframeMotion))
                {
                    typeName = "OpenSim.Region.Framework.Scenes.KeyframeMotion";
                }
                else if (serializedType == typeof(OsKeyframeMotion.Keyframe))
                {
                    typeName = "OpenSim.Region.Framework.Scenes.KeyframeMotion+Keyframe";
                }
                else if (serializedType == typeof(OsKeyframeMotion.DataFormat))
                {
                    typeName = "OpenSim.Region.Framework.Scenes.KeyframeMotion+DataFormat";
                }
                else if (serializedType == typeof(OsKeyframeMotion.PlayMode))
                {
                    typeName = "OpenSim.Region.Framework.Scenes.KeyframeMotion+PlayMode";
                }
                else if (serializedType == typeof(List<OsKeyframeMotion.Keyframe>))
                {
                    assemblyName = "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
                    typeName = "System.Collections.Generic.List`1[[OpenSim.Region.Framework.Scenes.KeyframeMotion+Keyframe, OpenSim.Region.Framework, Culture=neutral, PublicKeyToken=null]]";
                }
                else
                {
                    base.BindToName(serializedType, out assemblyName, out typeName);
                }
            }
            public override Type BindToType(string assemblyName, string typeName)
            {
                if(typeName == "OpenMetaverse.Vector3")
                {
                    return typeof(OsVector3);
                }
                else if(typeName == "OpenMetaverse.Quaternion")
                {
                    return typeof(OsQuaternion);
                }
                else if(typeName == "OpenSim.Region.Framework.Scenes.KeyframeMotion")
                {
                    return typeof(OsKeyframeMotion);
                }
                else if(typeName == "OpenSim.Region.Framework.Scenes.KeyframeMotion+Keyframe")
                {
                    return typeof(OsKeyframeMotion.Keyframe);
                }
                else if(typeName == "OpenSim.Region.Framework.Scenes.KeyframeMotion+DataFormat")
                {
                    return typeof(OsKeyframeMotion.DataFormat);
                }
                else if(typeName == "OpenSim.Region.Framework.Scenes.KeyframeMotion+PlayMode")
                {
                    return typeof(OsKeyframeMotion.PlayMode);
                }
                else if(typeName.StartsWith("System.Collections.Generic.List`1[[OpenSim.Region.Framework.Scenes.KeyframeMotion+Keyframe"))
                {
                    return typeof(List<OsKeyframeMotion.Keyframe>);
                }
                else
                {
                    return null;
                }
            }
        }

        private static OsKeyframeMotion OsDeserialize(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Binder = new OsKfSerializationBinder();

                return (OsKeyframeMotion)bf.Deserialize(ms);
            }
        }

        public static KeyframedMotion Deserialize(byte[] data)
        {
            OsKeyframeMotion oskf = OsDeserialize(data);
            KeyframedMotion kf = new KeyframedMotion();

            kf.IsRunning = oskf.m_running;
            kf.IsRunningReverse = (oskf.m_iterations & 1) != 0;
            kf.Flags = KeyframedMotion.DataFlags.None;
            if((oskf.m_data & OsKeyframeMotion.DataFormat.Rotation) != 0)
            {
                kf.Flags = KeyframedMotion.DataFlags.Rotation;
            }
            if((oskf.m_data & OsKeyframeMotion.DataFormat.Translation) != 0)
            {
                kf.Flags = KeyframedMotion.DataFlags.Translation;
            }

            switch(oskf.m_mode)
            {
                case OsKeyframeMotion.PlayMode.Forward:
                    kf.PlayMode = KeyframedMotion.Mode.Forward;
                    kf.IsRunningReverse = false;
                    break;

                case OsKeyframeMotion.PlayMode.Loop:
                    kf.PlayMode = KeyframedMotion.Mode.Loop;
                    kf.IsRunningReverse = false;
                    break;

                case OsKeyframeMotion.PlayMode.PingPong:
                    kf.PlayMode = KeyframedMotion.Mode.PingPong;
                    break;

                case OsKeyframeMotion.PlayMode.Reverse:
                    kf.PlayMode = KeyframedMotion.Mode.Reverse;
                    kf.IsRunningReverse = true;
                    break;
            }

            foreach(OsKeyframeMotion.Keyframe osf in oskf.m_keyframes)
            {
                Keyframe f = new Keyframe();
                f.Duration = osf.TimeTotal / 1000.0;
                f.TargetPosition = osf.Position.GetValueOrDefault();
                f.TargetRotation = osf.Rotation.GetValueOrDefault(OsQuaternion.Identity);
                kf.Add(f);
            }

            kf.CurrentTimePosition = oskf.m_currentFrame.TimeMS / 1000.0;
            if (kf.IsRunningReverse)
            {
                kf.CurrentFrame = oskf.m_frames.Count - 1;
            }
            else
            { 
                kf.CurrentFrame = oskf.m_keyframes.Length - oskf.m_frames.Count;
            }

            return null;
        }

        private static byte[] Serialize(this OsKeyframeMotion oskf)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Binder = new OsKfSerializationBinder();

                bf.Serialize(ms, oskf);
                return ms.ToArray();
            }
        }

        public static byte[] Serialize(this KeyframedMotion kf, Vector3 curPos, Quaternion curRot)
        {
            OsKeyframeMotion oskf = new OsKeyframeMotion();
            oskf.m_running = kf.IsRunning;
            oskf.m_iterations = kf.IsRunningReverse ? 1 : 0;
            oskf.m_data = 0;
            if((kf.Flags & KeyframedMotion.DataFlags.Translation) != 0)
            {
                oskf.m_data |= OsKeyframeMotion.DataFormat.Translation;
            }
            if((kf.Flags & KeyframedMotion.DataFlags.Rotation) != 0)
            {
                oskf.m_data |= OsKeyframeMotion.DataFormat.Rotation;
            }

            List<OsKeyframeMotion.Keyframe> list = new List<OsKeyframeMotion.Keyframe>();
            foreach(Keyframe f in kf)
            {
                OsKeyframeMotion.Keyframe osf = new OsKeyframeMotion.Keyframe();
                osf.TimeTotal = (int)(f.Duration * 1000);
                if ((oskf.m_data & OsKeyframeMotion.DataFormat.Translation) != 0)
                {
                    osf.Position = (OsVector3)f.TargetPosition;
                }
                if ((oskf.m_data & OsKeyframeMotion.DataFormat.Rotation) != 0)
                {
                    osf.Rotation = (OsQuaternion)f.TargetRotation;
                }
                list.Add(osf);
            }
            oskf.m_keyframes = list.ToArray();
            if (kf.CurrentFrame < 0)
            {
                return oskf.Serialize();
            }

            Keyframe cf;
            try
            {
                cf = kf[kf.CurrentFrame];
            }
            catch
            {
                return oskf.Serialize();
            }

            if(kf.IsRunningReverse)
            {
                OsVector3 startPos = (OsVector3)curPos;
                OsQuaternion startRot = (OsQuaternion)curRot;
                oskf.m_basePosition = (OsVector3)kf[0].TargetPosition;
                oskf.m_baseRotation = (OsQuaternion)kf[0].TargetRotation;

                for(int i = 0; i <= kf.CurrentFrame; ++i)
                {
                    OsKeyframeMotion.Keyframe osf = oskf.m_keyframes[i];
                    osf.StartPosition = startPos;
                    osf.StartRotation = startRot;
                    startPos = osf.Position.GetValueOrDefault();
                    startRot = osf.Rotation.GetValueOrDefault();
                    oskf.m_frames.Add(osf);
                }
            }
            else
            {
                OsVector3 startPos = (OsVector3)curPos;
                OsQuaternion startRot = (OsQuaternion)curRot;
                oskf.m_basePosition = (OsVector3)kf[kf.Count - 1].TargetPosition;
                oskf.m_baseRotation = (OsQuaternion)kf[kf.Count - 1].TargetRotation;

                for (int i = oskf.m_keyframes.Length; i-- >= kf.CurrentFrame;)
                {
                    if (i >= 0)
                    {
                        OsKeyframeMotion.Keyframe osf = oskf.m_keyframes[i];
                        osf.StartPosition = startPos;
                        osf.StartRotation = startRot;
                        startPos = osf.Position.GetValueOrDefault();
                        startRot = osf.Rotation.GetValueOrDefault();
                        oskf.m_frames.Add(osf);
                    }
                }
            }

            return oskf.Serialize();
        }
    }
}
