// SilverSim is distributed under the terms of the
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

        readonly SunData m_SunData = new SunData();

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
        double m_AverageSunTilt = -0.25 * Math.PI;
        double m_SeasonalSunTilt = 0.03 * Math.PI;
        double m_SunNormalizedOffset = 0.45;

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
            double tilt = m_AverageSunTilt + m_SeasonalSunTilt * Math.Sin(yearly_phase);

            if (sunFixed)
            {
                lock (m_EnvironmentLock)
                {
                    sun_phase = m_SunData.FixedSunPhase % (2 * Math.PI);
                }
            }

            Quaternion tiltRot = new Quaternion(tilt, 1, 0, 0);

            lock (m_LightShareLock)
            {
                if (m_WindlightValid)
                {
                    sun_phase = m_SkyWindlight.SunMoonPosition;
                    tiltRot = Quaternion.CreateFromEulers(0, 0, m_SkyWindlight.EastAngle);
                }
            }

            Vector3 sunDirection = new Vector3(Math.Cos(-sun_phase), Math.Sin(-sun_phase), 0);
            sunDirection *= tiltRot;
            Vector3 sunVelocity = new Vector3(0, 0, DailyOmega);
            if (sunFixed || m_WindlightValid)
            {
                sunVelocity = Vector3.Zero;
            }
            sunVelocity *= tiltRot;
            sunDirection.Z += m_SunNormalizedOffset;
            double radius = sunDirection.Length;
            sunDirection = sunDirection.Normalize();
            sunVelocity *= (1 / radius);
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
