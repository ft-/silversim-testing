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
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace SilverSim.Scene.Types.KeyframedMotion
{
    public class KeyframedMotion : List<Keyframe>
    {
        [Flags]
        public enum DataFlags
        {
            Rotation = 1,
            Translation = 2,
        }

        public enum Mode
        {
            Forward = 0,
            Loop = 1,
            PingPong = 2,
            Reverse = 3
        }

        public DataFlags Flags { get; set; }
        public Mode PlayMode { get; set; }
        public int CurrentFrame { get; set; }
        public double CurrentTimePosition { get; set; }
        public bool IsRunning { get; set; }
        public bool IsRunningReverse { get; set; } /* needed for Ping Pong */

        public KeyframedMotion()
        {
            Flags = 0;
            CurrentFrame = -1;
            CurrentTimePosition = 0;
            PlayMode = Mode.Forward;
            IsRunning = false;
            IsRunningReverse = false;
        }

        public KeyframedMotion(KeyframedMotion m)
            : base(m)
        {
            CurrentFrame = m.CurrentFrame;
            CurrentTimePosition = m.CurrentTimePosition;
            Flags = m.Flags;
            PlayMode = m.PlayMode;
            IsRunning = m.IsRunning;
            IsRunningReverse = false;
        }

        public void Serialize(Stream o)
        {
            Map r = new Map();
            AnArray pos = null;
            AnArray rot = null;
            AnArray durations = new AnArray();
            r.Add("durations", durations);
            r.Add("mode", (int)PlayMode);
            r.Add("currentframe", CurrentFrame);
            r.Add("currenttimeposition", CurrentTimePosition);
            r.Add("running", IsRunning);
            r.Add("runningreverse", IsRunningReverse);

            if((DataFlags.Rotation & Flags) != 0)
            {
                rot = new AnArray();
                r.Add("rotations", rot);
            }

            if ((DataFlags.Translation & Flags) != 0)
            {
                pos = new AnArray();
                r.Add("positions", pos);
            }

            foreach (Keyframe frame in this)
            {
                if((DataFlags.Rotation & Flags) != 0)
                {
                    rot.Add(frame.TargetRotation);
                }
                if((DataFlags.Translation & Flags) != 0)
                {
                    pos.Add(frame.TargetPosition);
                }
                durations.Add(frame.Duration);
            }
            LlsdBinary.Serialize(r, o);
        }

        [Serializable]
        public class KeyframeFormatException : Exception
        {
            public KeyframeFormatException()
            {
            }

            public KeyframeFormatException(string message)
                : base(message)
            {
            }

            public KeyframeFormatException(string message, Exception innerException)
                : base(message, innerException)
            {
            }

            protected KeyframeFormatException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }
        }

        public static KeyframedMotion Deserialize(Stream input)
        {
            var data = (Map)LlsdBinary.Deserialize(input);
            var m = new KeyframedMotion();
            AnArray pos = null;
            AnArray rot = null;
            AnArray durations;
            m.PlayMode = (Mode)data["mode"].AsInt;
            m.IsRunning = data["running"].AsBoolean;
            m.IsRunningReverse = data["runningreverse"].AsBoolean;

            durations = (AnArray)data["durations"];
            if(data.ContainsKey("positions"))
            {
                pos = data["positions"] as AnArray;
                m.Flags |= DataFlags.Translation;
                if(durations.Count != pos.Count)
                {
                    throw new KeyframeFormatException("Positions.Count does not match Durations.Count");
                }
            }

            if (data.ContainsKey("rotations"))
            {
                rot = data["rotations"] as AnArray;
                m.Flags |= DataFlags.Rotation;
                if (durations.Count != rot.Count)
                {
                    throw new KeyframeFormatException("Rotations.Count does not match Durations.Count");
                }
            }

            for(int i = 0; i < durations.Count; ++i)
            {
                var frame = new Keyframe();
                frame.Duration = durations[i].AsReal;
                if(rot != null)
                {
                    frame.TargetRotation = rot[i].AsQuaternion;
                }
                if(pos != null)
                {
                    frame.TargetPosition = pos[i].AsVector3;
                }
                m.Add(frame);
            }

            return m;
        }
    }
}
