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

using log4net;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Viewer.Messages;

namespace SilverSim.Scene.Types.SceneEnvironment
{
    public partial class EnvironmentController
    {
        private static readonly ILog m_Log = LogManager.GetLogger("ENVIRONMENT CONTROLLER");

        private readonly object m_EnvironmentLock = new object();

        public IWindModel Wind { get; }

        private readonly SceneInterface m_Scene;
        private readonly System.Timers.Timer m_Timer = new System.Timers.Timer(1000 / 60f);

        private int m_SunUpdateEveryMsecs = 10000;
        private uint m_SendSimTimeAfterNSunUpdates = 10 - 1;
        private int m_UpdateWindModelEveryMsecs = 10000;
        private int m_UpdateTidalModelEveryMsecs = 60000;

        private uint m_SunUpdatesUntilSendSimTime;

        #region Update Rate Control
        public int UpdateTidalModelEveryMsecs
        {
            get { return m_UpdateTidalModelEveryMsecs; }

            set
            {
                m_UpdateTidalModelEveryMsecs = value;
                TriggerOnEnvironmentControllerChange();
            }
        }

        public int SunUpdateEveryMsecs
        {
            get { return m_SunUpdateEveryMsecs; }

            set
            {
                m_SunUpdateEveryMsecs = value;
                TriggerOnEnvironmentControllerChange();
            }
        }

        public uint SendSimTimeEveryNthSunUpdate
        {
            get { return m_SendSimTimeAfterNSunUpdates + 1; }

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
            get { return m_UpdateWindModelEveryMsecs; }

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

        public EnvironmentController(SceneInterface scene, IWindModelFactory factory = null)
        {
            m_Scene = scene;
            if (factory != null)
            {
                Wind = factory.Instantiate(scene);
            }
            else
            {
                Wind = new NoWindModel();
            }
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
                    m_LastFpsTickCount = StopWatchTime.TickCount;
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

        private long m_LastFpsTickCount;
        private long m_LastWindModelUpdateTickCount;
        private long m_LastSunUpdateTickCount;
        private long m_LastTidalModelUpdateTickCount;
        private long m_CountedTicks;
        private double m_EnvironmentFps;

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
            long newTickCount = StopWatchTime.TickCount;
            if (newTickCount - m_LastFpsTickCount >= 1000)
            {
                long timeDiff = newTickCount - m_LastFpsTickCount;
                m_LastFpsTickCount = newTickCount;
                lock (m_EnvironmentLock)
                {
                    m_EnvironmentFps = m_CountedTicks * StopWatchTime.Frequency / (double)timeDiff;
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

            long lastwinddt = newTickCount - m_LastWindModelUpdateTickCount;
            if (lastwinddt >= m_UpdateWindModelEveryMsecs)
            {
                m_LastWindModelUpdateTickCount = newTickCount;
                Wind?.UpdateModel(m_SunData, lastwinddt / 1000.0);
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

        private void SendToAllRootAgents(Message m)
        {
            foreach (IAgent agent in m_Scene.Agents)
            {
                agent.SendMessageIfRootAgent(m, m_Scene.ID);
            }
        }
    }
}
