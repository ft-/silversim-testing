// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using SilverSim.Viewer.Messages;
using System;

namespace SilverSim.Scene.Types.SceneEnvironment
{
    public partial class EnvironmentController
    {
        private static readonly ILog m_Log = LogManager.GetLogger("ENVIRONMENT CONTROLLER");

        readonly object m_EnvironmentLock = new object();

        public IWindModel Wind
        {
            get; private set;
        }

        readonly SceneInterface m_Scene;
        readonly System.Timers.Timer m_Timer = new System.Timers.Timer(1000 / 60f);

        int m_SunUpdateEveryMsecs = 10000;
        uint m_SendSimTimeAfterNSunUpdates = 10 - 1;
        int m_UpdateWindModelEveryMsecs = 10000;
        int m_UpdateTidalModelEveryMsecs = 60000;

        uint m_SunUpdatesUntilSendSimTime;

        #region Update Rate Control
        public int UpdateTidalModelEveryMsecs
        {
            get
            {
                return m_UpdateTidalModelEveryMsecs;
            }
            set
            {
                m_UpdateTidalModelEveryMsecs = value;
                TriggerOnEnvironmentControllerChange();
            }
        }

        public int SunUpdateEveryMsecs
        {
            get
            {
                return m_SunUpdateEveryMsecs;
            }
            set
            {
                m_SunUpdateEveryMsecs = value;
                TriggerOnEnvironmentControllerChange();
            }
        }

        public uint SendSimTimeEveryNthSunUpdate
        {
            get
            {
                return m_SendSimTimeAfterNSunUpdates + 1;
            }
            set
            {
                if (value >= 1)
                {
                    m_SendSimTimeAfterNSunUpdates = value - 1;
                }
                TriggerOnEnvironmentControllerChange();
            }
        }

        public int UpdateWindModelEveryMsecs
        {
            get
            {
                return m_UpdateWindModelEveryMsecs;
            }
            set
            {
                if (value >= 1)
                {
                    m_UpdateWindModelEveryMsecs = value;
                }
                TriggerOnEnvironmentControllerChange();
            }
        }
        #endregion

        public EnvironmentController(SceneInterface scene)
        {
            m_Scene = scene;
            Wind = new NoWindModel();
            m_SunData.SunDirection = new Vector3();
            ResetToDefaults();
        }

        #region Start/Stop
        public void Start()
        {
            lock (m_EnvironmentLock)
            {
                if (!m_Timer.Enabled)
                {
                    EnvironmentTimer(this, null);
                    m_Timer.Elapsed += EnvironmentTimer;
                    m_LastFpsTickCount = System.Environment.TickCount;
                    m_LastWindModelUpdateTickCount = m_LastFpsTickCount;
                    m_LastSunUpdateTickCount = m_LastFpsTickCount;
                    m_LastTidalModelUpdateTickCount = m_LastFpsTickCount;
                    m_CountedTicks = 0;
                    m_Timer.Start();
                }
            }
        }

        public void Stop()
        {
            lock (m_EnvironmentLock)
            {
                if (m_Timer.Enabled)
                {
                    m_Timer.Stop();
                    m_Timer.Elapsed -= EnvironmentTimer;
                }
            }
        }
        #endregion

        int m_LastFpsTickCount;
        int m_LastWindModelUpdateTickCount;
        int m_LastSunUpdateTickCount;
        int m_LastTidalModelUpdateTickCount;
        int m_CountedTicks;
        double m_EnvironmentFps;

        public double EnvironmentFps
        {
            get
            {
                lock (m_EnvironmentLock)
                {
                    return m_EnvironmentFps;
                }
            }
        }

        private void EnvironmentTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            ++m_CountedTicks;
            int newTickCount = System.Environment.TickCount;
            if (newTickCount - m_LastFpsTickCount >= 1000)
            {
                int timeDiff = newTickCount - m_LastFpsTickCount;
                m_LastFpsTickCount = System.Environment.TickCount;
                lock (m_EnvironmentLock)
                {
                    m_EnvironmentFps = m_CountedTicks * 1000f / (double)timeDiff;
                }
                m_CountedTicks = 0;
            }

            if (newTickCount - m_LastSunUpdateTickCount >= m_SunUpdateEveryMsecs || m_ImmediateSunUpdate)
            {
                bool immedSendSimTime;
                lock (m_EnvironmentLock)
                {
                    immedSendSimTime = m_ImmediateSunUpdate;
                    m_ImmediateSunUpdate = false;
                }
                m_LastSunUpdateTickCount = newTickCount;
                UpdateSunDirection();
                if (m_SunUpdatesUntilSendSimTime-- == 0 || immedSendSimTime)
                {
                    m_SunUpdatesUntilSendSimTime = m_SendSimTimeAfterNSunUpdates;
                    SendSimulatorTimeMessageToAllClients();
                }
            }
            if (null != Wind && newTickCount - m_LastWindModelUpdateTickCount >= m_UpdateWindModelEveryMsecs)
            {
                m_LastWindModelUpdateTickCount = newTickCount;
                Wind.UpdateModel(m_SunData);
            }

            UpdateMoonPhase();

            if (newTickCount - m_LastTidalModelUpdateTickCount >= m_UpdateTidalModelEveryMsecs)
            {
                m_LastTidalModelUpdateTickCount = newTickCount;
                TidalTimer();
            }
        }

        private void SendToAllClients(Message m)
        {
            foreach (IAgent agent in m_Scene.Agents)
            {
                agent.SendMessageAlways(m, m_Scene.ID);
            }
        }
    }
}
