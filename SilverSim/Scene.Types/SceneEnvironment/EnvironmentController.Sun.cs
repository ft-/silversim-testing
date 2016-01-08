﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Types;
using SilverSim.Viewer.Messages.Region;
using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Types.SceneEnvironment
{
    public partial class EnvironmentController
    {
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidLargeStructureRule")]
        public struct SunData
        {
            public UInt64 UsecSinceStart;
            public UInt32 SecPerDay;
            public UInt32 SecPerYear;
            public Vector3 SunDirection;
            public double SunPhase;
            public double FixedSunPhase;
            public Vector3 SunAngVelocity;
            public bool IsSunFixed;
        }

        SunData m_SunData = new SunData();

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
            }
        }


        #region Update of sun direction
        /* source of algorithm is secondlifescripters mailing list */
        double AverageSunTilt = -0.25 * Math.PI;
        double SeasonalSunTilt = 0.03 * Math.PI;
        double SunNormalizedOffset = 0.45;

        public void SetSunDurationParams(uint secperday, uint daysperyear)
        {
            lock (m_EnvironmentLock)
            {
                m_SunData.SecPerDay = secperday;
                m_SunData.SecPerYear = secperday * daysperyear;
            }
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

        bool m_ImmediateSunUpdate;
        public bool IsSunFixed
        {
            get
            {
                return m_SunData.IsSunFixed;
            }
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
            double tilt = AverageSunTilt + SeasonalSunTilt * Math.Sin(yearly_phase);

            if (sunFixed)
            {
                lock (m_EnvironmentLock)
                {
                    sun_phase = m_SunData.FixedSunPhase % (2 * Math.PI);
                }
            }

            lock(m_LightShareLock)
            {
                if (m_WindlightValid)
                {
                    sun_phase = m_SkyWindlight.SunMoonPosition;
                }
            }

            Vector3 sunDirection = new Vector3(Math.Cos(-sun_phase), Math.Sin(-sun_phase), 0);
            Quaternion tiltRot = new Quaternion(tilt, 1, 0, 0);

            sunDirection *= tiltRot;
            Vector3 sunVelocity = new Vector3(0, 0, DailyOmega);
            if (sunFixed || m_WindlightValid)
            {
                sunVelocity = Vector3.Zero;
            }
            sunVelocity *= tiltRot;
            sunDirection.Z += SunNormalizedOffset;
            double radius = sunDirection.Length;
            sunDirection = sunDirection.Normalize();
            sunVelocity *= (1 / radius);
            lock (this)
            {
                m_SunData.SunPhase = sun_phase;
                m_SunData.SunDirection = sunDirection;
                m_SunData.SunAngVelocity = sunVelocity;
                m_SunData.UsecSinceStart = utctime * 1000000;
            }
        }
        #endregion

        #region Viewer time message update
        private void SendSimulatorTimeMessageToAllClients()
        {
            SimulatorViewerTimeMessage m = new SimulatorViewerTimeMessage();
            m.SunPhase = m_SunData.SunPhase;
            m.UsecSinceStart = m_SunData.UsecSinceStart;
            m.SunDirection = m_SunData.SunDirection;
            m.SunAngVelocity = m_SunData.SunAngVelocity;
            m.SecPerYear = m_SunData.SecPerYear;
            m.SecPerDay = m_SunData.SecPerDay;
            SendToAllClients(m);
        }

        public void SendSimulatorTimeMessageToClient(IAgent agent)
        {
            SimulatorViewerTimeMessage m = new SimulatorViewerTimeMessage();
            m.SunPhase = m_SunData.SunPhase;
            m.UsecSinceStart = Date.GetUnixTime() * 1000000;
            m.SunDirection = m_SunData.SunDirection;
            m.SunAngVelocity = m_SunData.SunAngVelocity;
            m.SecPerYear = m_SunData.SecPerYear;
            m.SecPerDay = m_SunData.SecPerDay;
            agent.SendMessageAlways(m, m_Scene.ID);
        }
        #endregion
    }
}
