// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using System;
using System.Timers;

namespace SilverSim.Scene.Types.KeyframedMotion
{
    public class KeyframedMotionController : IDisposable
    {
        const double KEYFRAME_TIME_STEP = 1f / 10;
        Timer m_KeyframeTimer = new Timer(KEYFRAME_TIME_STEP);
        object m_KeyframeLock = new object();
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

        #region Serialization Helpers
        public void PlayIfWasRunning()
        {
            lock(m_KeyframeLock)
            {
                if(m_Program.IsRunning)
                {
                    m_KeyframeTimer.Enabled = true;
                }
            }
        }

        public void PauseForData()
        {
            lock (m_KeyframeLock)
            {
                m_KeyframeTimer.Enabled = false;
            }
        }
        #endregion

        #region Normal controls
        public void Play()
        {
            lock(m_KeyframeLock)
            {
                m_Program.IsRunning = true;
                m_KeyframeTimer.Enabled = true;
            }
        }

        public void Pause()
        {
            lock (m_KeyframeLock)
            {
                ObjectGroup.Velocity = Vector3.Zero;
                ObjectGroup.AngularVelocity = Vector3.Zero;
                m_Program.IsRunning = false;
                m_KeyframeTimer.Enabled = false;
            }
        }

        public void Stop()
        {
            lock (m_KeyframeLock)
            {
                ObjectGroup.Velocity = Vector3.Zero;
                ObjectGroup.AngularVelocity = Vector3.Zero;
                m_Program.IsRunning = false;
                m_KeyframeTimer.Enabled = false;
                /* reset program */
                m_Program.CurrentFrame = -1;
            }
        }
        #endregion

        void KeyframeTimer(object o, ElapsedEventArgs args)
        {
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
                        Vector3 distance = curFrame.TargetPosition - ObjectGroup.Position;
                        ObjectGroup.Velocity = distance / curFrame.Duration;
                    }

                    if((flags & KeyframedMotion.DataFlags.Rotation) != 0)
                    {
                        Vector3 angularDistance = (curFrame.TargetRotation / ObjectGroup.Rotation).GetEulerAngles();
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
