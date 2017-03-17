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
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System;
using System.Timers;

namespace SilverSim.Scene.Types.KeyframedMotion
{
    public class KeyframedMotionController : IDisposable
    {
        const double KEYFRAME_TIME_STEP = 1f / 45;
        readonly Timer m_KeyframeTimer = new Timer(KEYFRAME_TIME_STEP);
        readonly object m_KeyframeLock = new object();
        KeyframedMotion m_Program = new KeyframedMotion();

        public ObjectGroup ObjectGroup { get; private set; }

        public KeyframedMotion Program
        {
            get
            {
                lock(m_KeyframeLock)
                {
                    return new KeyframedMotion(m_Program);
                }
            }

            set
            {
                lock(m_KeyframeLock)
                {
                    if(value.Count == 0)
                    {
                        throw new ArgumentException("KeyframedMotion has no keyframes");
                    }
                    m_Program = new KeyframedMotion(value);
                    m_KeyframeTimer.Enabled = m_Program.IsRunning;
                }
            }
        }

        public KeyframedMotionController(ObjectGroup group)
        {
            ObjectGroup = group;
            m_KeyframeTimer.Elapsed += KeyframeTimer;
        }

        public void Dispose()
        {
            m_KeyframeTimer.Elapsed -= KeyframeTimer;
            m_KeyframeTimer.Dispose();
        }
        
        #region Normal controls
        public void Play()
        {
            bool wasRunning;
            lock(m_KeyframeLock)
            {
                wasRunning = m_Program.IsRunning;
                m_Program.IsRunning = true;
                m_KeyframeTimer.Enabled = true;
            }
            if(!wasRunning)
            {
                ObjectGroup.PostEvent(new MovingStartEvent());
            }
        }

        public void Pause()
        {
            bool wasRunning;
            lock (m_KeyframeLock)
            {
                ObjectGroup.Velocity = Vector3.Zero;
                ObjectGroup.AngularVelocity = Vector3.Zero;
                wasRunning = m_Program.IsRunning;
                m_Program.IsRunning = false;
                m_KeyframeTimer.Enabled = false;
            }
            if (!wasRunning)
            {
                ObjectGroup.PostEvent(new MovingStartEvent());
            }
        }

        public void Stop()
        {
            bool wasRunning;
            lock (m_KeyframeLock)
            {
                ObjectGroup.Velocity = Vector3.Zero;
                ObjectGroup.AngularVelocity = Vector3.Zero;
                wasRunning = m_Program.IsRunning;
                m_Program.IsRunning = false;
                m_KeyframeTimer.Enabled = false;
                /* reset program */
                m_Program.CurrentFrame = -1;
            }
            if (wasRunning)
            {
                ObjectGroup.PostEvent(new MovingEndEvent());
            }
        }
        #endregion

        void KeyframeTimer(object o, ElapsedEventArgs args)
        {
            SceneInterface scene = ObjectGroup.Scene;
            if(!scene.IsKeyframedMotionEnabled)
            {
                return;
            }

            lock(m_KeyframeLock)
            {
                bool newKeyframe = false;
                if(m_Program.CurrentFrame == -1)
                {
                    m_Program.IsRunningReverse = false;
                    m_Program.CurrentTimePosition = 0;
                    m_Program.CurrentFrame = (m_Program.PlayMode == KeyframedMotion.Mode.Reverse) ?
                        m_Program.Count - 1 :
                        0;
                    newKeyframe = true;
                }
                else
                {
                    m_Program.CurrentTimePosition += KEYFRAME_TIME_STEP;
                }

                Keyframe curFrame = m_Program[m_Program.CurrentFrame];
                KeyframedMotion.DataFlags flags = m_Program.Flags;

                if (curFrame.Duration < m_Program.CurrentTimePosition)
                {
                    if(m_Program.IsRunningReverse)
                    {
                        if(--m_Program.CurrentFrame < 0)
                        {
                            m_Program.CurrentFrame = Math.Min(1, m_Program.Count);

                            if (m_Program.PlayMode != KeyframedMotion.Mode.Reverse)
                            {
                                m_Program.IsRunningReverse = false;
                            }
                            else
                            {
                                if ((flags & KeyframedMotion.DataFlags.Translation) != 0)
                                {
                                    ObjectGroup.Velocity = Vector3.Zero;
                                    ObjectGroup.Position = curFrame.TargetPosition;
                                }
                                if ((flags & KeyframedMotion.DataFlags.Rotation) != 0)
                                {
                                    ObjectGroup.AngularVelocity = Vector3.Zero;
                                    ObjectGroup.Rotation = curFrame.TargetRotation;
                                }
                                m_Program.CurrentFrame = -1;
                                m_Program.IsRunning = false;
                                m_KeyframeTimer.Enabled = false;
                                ObjectGroup.PostEvent(new MovingEndEvent());
                                return;
                            }
                        }
                    }
                    else
                    {
                        if(++m_Program.CurrentFrame == m_Program.Count)
                        {
                            switch(m_Program.PlayMode)
                            {
                                case KeyframedMotion.Mode.Forward:
                                    if ((flags & KeyframedMotion.DataFlags.Translation) != 0)
                                    {
                                        ObjectGroup.Velocity = Vector3.Zero;
                                        ObjectGroup.Position = curFrame.TargetPosition;
                                    }
                                    if ((flags & KeyframedMotion.DataFlags.Rotation) != 0)
                                    {
                                        ObjectGroup.AngularVelocity = Vector3.Zero;
                                        ObjectGroup.Rotation = curFrame.TargetRotation;
                                    }
                                    m_Program.CurrentFrame = -1;
                                    m_Program.IsRunning = false;
                                    m_KeyframeTimer.Enabled = false;
                                    ObjectGroup.PostEvent(new MovingEndEvent());
                                    return;

                                case KeyframedMotion.Mode.Loop:
                                    m_Program.CurrentFrame = 0;
                                    break;

                                case KeyframedMotion.Mode.PingPong:
                                    if(--m_Program.CurrentFrame < 0)
                                    {
                                        m_Program.CurrentFrame = 0;
                                    }
                                    m_Program.IsRunningReverse = true;
                                    break;

                                default:
                                    break;
                            }
                        }
                    }
                    newKeyframe = true;
                    m_Program.CurrentTimePosition = 0f;
                }

                curFrame = m_Program[m_Program.CurrentFrame];
                if (newKeyframe)
                {
                    if((flags & KeyframedMotion.DataFlags.Translation) != 0)
                    {
                        Vector3 distance = curFrame.TargetPosition;
                        ObjectGroup.Velocity = distance / curFrame.Duration;
                    }

                    if((flags & KeyframedMotion.DataFlags.Rotation) != 0)
                    {
                        Vector3 angularDistance = curFrame.TargetRotation.GetEulerAngles();
                        ObjectGroup.AngularVelocity = angularDistance / curFrame.Duration;
                    }
                }
                else
                {
                    if ((flags & KeyframedMotion.DataFlags.Translation) != 0)
                    {
                        ObjectGroup.Position += ObjectGroup.Velocity;
                    }

                    if ((flags & KeyframedMotion.DataFlags.Rotation) != 0)
                    {
                        ObjectGroup.Rotation *= Quaternion.CreateFromEulers(ObjectGroup.AngularVelocity);
                    }
                }
            }
        }
    }
}
