﻿// SilverSim is distributed under the terms of the
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

using SilverSim.Scene.Types.Agent;
using SilverSim.Types;
using SilverSim.Viewer.Messages.Region;
using System;

namespace SilverSim.Scene.Types.SceneEnvironment
{
    public partial class EnvironmentController
    {
        public class SunData
        {
            public UInt64 UsecSinceStart { get; internal set; }
            public UInt32 SecPerDay { get; internal set; }
            public UInt32 SecPerYear { get; internal set; }
            public Vector3 SunDirection { get; internal set; }
            public double SunPhase { get; internal set; }
            public double FixedSunPhase { get; internal set; }
            public Vector3 SunAngVelocity { get; internal set; }
            public bool IsSunFixed { get; internal set; }

            public SunData()
            {
                SecPerDay = 4 * 60 * 60;
                SecPerYear = 11 * SecPerDay;
            }
        }

        private readonly SunData m_SunData = new SunData();

        public Vector3 SunDirection
        {
            get
            {
                lock (m_EnvironmentLock)
                {
                    return m_SunData.SunDirection;
                }
            }
            set
            {
                lock (m_EnvironmentLock)
                {
                    m_SunData.SunDirection = value;
                }
                TriggerOnEnvironmentControllerChange();
            }
        }

        public double SunPhase
        {
            get
            {
                lock(m_EnvironmentLock)
                {
                    return m_SunData.SunPhase;
                }
            }
        }

        #region Update of sun direction
        /* source of algorithm is secondlifescripters mailing list */
        private double m_AverageSunTilt = -0.25 * Math.PI;
        private double m_SeasonalSunTilt = 0.03 * Math.PI;
        private double m_SunNormalizedOffset = 0.45;

        public double AverageSunTilt
        {
            get
            {
                lock(m_EnvironmentLock)
                {
                    return m_AverageSunTilt;
                }
            }
            set
            {
                lock(m_EnvironmentLock)
                {
                    m_AverageSunTilt = value.Clamp(-Math.PI, Math.PI);
                }
                TriggerOnEnvironmentControllerChange();
            }
        }

        public double SeasonalSunTilt
        {
            get
            {
                lock (m_EnvironmentLock)
                {
                    return m_SeasonalSunTilt;
                }
            }
            set
            {
                lock (m_EnvironmentLock)
                {
                    m_SeasonalSunTilt = value.Clamp(-Math.PI, Math.PI);
                }
                TriggerOnEnvironmentControllerChange();
            }
        }

        public double SunNormalizedOffset
        {
            get
            {
                lock (m_EnvironmentLock)
                {
                    return m_SunNormalizedOffset;
                }
            }
            set
            {
                lock (m_EnvironmentLock)
                {
                    m_SunNormalizedOffset = value.Clamp(-1, 1);
                }
                TriggerOnEnvironmentControllerChange();
            }
        }

        public void SetSunDurationParams(uint secperday, uint daysperyear)
        {
            lock (m_EnvironmentLock)
            {
                m_SunData.SecPerDay = secperday;
                m_SunData.SecPerYear = secperday * daysperyear;
            }
            TriggerOnEnvironmentControllerChange();
        }

        public void GetSunDurationParams(out uint secperday, out uint daysperyear)
        {
            lock (m_EnvironmentLock)
            {
                secperday = m_SunData.SecPerDay;
                daysperyear = m_SunData.SecPerYear / m_SunData.SecPerDay;
            }
        }

        public double TimeOfDay
        {
            get
            {
                ulong utctime = Date.GetUnixTime();
                return m_SunData.IsSunFixed ?
                    utctime :
                    utctime % (m_SunData.SecPerDay);
            }
        }

        public double ActualSunPosition
        {
            get
            {
                lock (m_EnvironmentLock)
                {
                    return (m_SunData.SunPhase * 12 / Math.PI) % 24;
                }
            }
        }

        public double FixedSunPosition
        {
            get
            {
                lock (m_EnvironmentLock)
                {
                    return m_SunData.FixedSunPhase * 12 / Math.PI;
                }
            }
            set
            {
                lock (m_EnvironmentLock)
                {
#if DEBUG
                    m_Log.DebugFormat("FixedSunPosition set to {0}h Position", value);
#endif
                    m_SunData.FixedSunPhase = (value * Math.PI / 12) % (2 * Math.PI);
                }
            }
        }

        private bool m_ImmediateSunUpdate;
        public bool IsSunFixed
        {
            get { return m_SunData.IsSunFixed; }

            set
            {
#if DEBUG
                m_Log.DebugFormat("IsSunFixed set to {0}", value);
#endif
                m_SunData.IsSunFixed = value;
                lock (m_EnvironmentLock)
                {
                    m_ImmediateSunUpdate = true;
                }
            }
        }

        public void UpdateSunDirection()
        {
            double DailyOmega;
            double YearlyOmega;
            lock (m_EnvironmentLock)
            {
                DailyOmega = 2f / m_SunData.SecPerDay;
                YearlyOmega = 2f / (m_SunData.SecPerYear);
            }
            ulong utctime = Date.GetUnixTime();
            bool sunFixed = m_SunData.IsSunFixed;

            double daily_phase = DailyOmega * utctime;
            double sun_phase = daily_phase % (2 * Math.PI);
            double yearly_phase = YearlyOmega * utctime;
            double tilt = m_AverageSunTilt + m_SeasonalSunTilt * Math.Sin(yearly_phase);

            if (sunFixed)
            {
                lock (m_EnvironmentLock)
                {
                    sun_phase = m_SunData.FixedSunPhase % (2 * Math.PI);
                }
            }

            var tiltRot = new Quaternion(tilt, 1, 0, 0);

            lock (m_LightShareLock)
            {
                if (m_WindlightValid)
                {
                    sun_phase = m_SkyWindlight.SunMoonPosition;
                    tiltRot = Quaternion.CreateFromEulers(0, 0, m_SkyWindlight.EastAngle);
                }
            }

            var sunDirection = new Vector3(Math.Cos(-sun_phase), Math.Sin(-sun_phase), 0);
            sunDirection *= tiltRot;
            var sunVelocity = new Vector3(0, 0, DailyOmega);
            if (sunFixed || m_WindlightValid)
            {
                sunVelocity = Vector3.Zero;
            }
            sunVelocity *= tiltRot;
            sunDirection.Z += m_SunNormalizedOffset;
            double radius = sunDirection.Length;
            sunDirection = sunDirection.Normalize();
            sunVelocity *= 1 / radius;
            lock (m_EnvironmentLock)
            {
                m_SunData.SunPhase = sun_phase;
                m_SunData.SunDirection = sunDirection;
                m_SunData.SunAngVelocity = sunVelocity;
                m_SunData.UsecSinceStart = utctime * 1000000;
            }
        }
        #endregion

        #region Viewer time message update
        private SimulatorViewerTimeMessage BuildTimeMessage() => new SimulatorViewerTimeMessage
        {
            SunPhase = m_SunData.SunPhase,
            UsecSinceStart = Date.GetUnixTime() * 1000000,
            SunDirection = m_SunData.SunDirection,
            SunAngVelocity = m_SunData.SunAngVelocity,
            SecPerYear = m_SunData.SecPerYear,
            SecPerDay = m_SunData.SecPerDay
        };

        private void SendSimulatorTimeMessageToAllClients()
        {
            SendToAllRootAgents(BuildTimeMessage());
        }

        public void SendSimulatorTimeMessageToClient(IAgent agent)
        {
            agent.SendMessageIfRootAgent(BuildTimeMessage(), m_Scene.ID);
        }
        #endregion
    }
}
