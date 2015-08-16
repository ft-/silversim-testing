// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Scene;
using System;
using System.Threading;
using BulletSharp;
using SilverSim.Types;
using log4net;
using ThreadedClasses;
using System.Timers;

namespace SilverSim.Scene.Physics.Bullet.Implementation
{
    public partial class BulletScene : IPhysicsScene, ISceneListener, ITerrainListener
    {
        private static readonly ILog m_Log = LogManager.GetLogger("BULLET SCENE");

        SceneInterface m_Scene;
        DefaultCollisionConfiguration m_CollisionConfiguration;
        CollisionDispatcher m_CollisionDispatcher;
        BroadphaseInterface m_BroadphaseInterface;
        SequentialImpulseConstraintSolver m_ConstraintSolver;
        DiscreteDynamicsWorld m_DynamicsWorld;
        System.Timers.Timer m_PhysicsTimer = new System.Timers.Timer(1 / 60f); /* 60 Hz is the right thing to do */
        bool m_PhysicsStarted = false;

        public BulletScene(SceneInterface scene)
        {
            m_Scene = scene;
            m_CollisionConfiguration = new DefaultCollisionConfiguration();
            m_CollisionDispatcher = new CollisionDispatcher(m_CollisionConfiguration);
            m_BroadphaseInterface = new DbvtBroadphase();
            m_ConstraintSolver = new SequentialImpulseConstraintSolver(); /* there are others we can use */
            m_DynamicsWorld = new DiscreteDynamicsWorld(m_CollisionDispatcher, m_BroadphaseInterface, m_ConstraintSolver, m_CollisionConfiguration);
            m_DynamicsWorld.Gravity = new Vector3(0, 0, -9.81); /* be nice play real numbers here */
            
            InitializeTerrainMesh();
            scene.Terrain.TerrainListeners.Add(this);
            scene.SceneListeners.Add(this);
            try
            {
                new Thread(BulletTerrainUpdateThread).Start();
                new Thread(BulletAddThread).Start();
                new Thread(BulletUpdateThread).Start();
            }
            catch
            {
                m_StopBulletThreads = true;
                scene.Terrain.TerrainListeners.Remove(this);
                scene.SceneListeners.Remove(this);
            }
            m_PhysicsTimer.Elapsed += PhysicsTrigger;
        }

        public void Shutdown()
        {
            m_PhysicsTimer.Elapsed -= PhysicsTrigger;
            m_Scene.Terrain.TerrainListeners.Remove(this);
            m_Scene.SceneListeners.Remove(this);
            m_StopBulletThreads = true;
        }

        void EnablePhysicsInternally()
        {
            lock (this)
            {
                if (!m_PhysicsStarted)
                {
                    m_PhysicsStarted = true;
                    m_PhysicsTimer.Start();
                    new Thread(PhysicsProcess).Start();
                }
            }
        }

        public void RemoveAll()
        {
        }

        public void Add(IObject obj)
        {

        }

        void UpdateObject(IObject obj)
        {

        }

        public void Remove(IObject obj)
        {

        }

        public double PhysicsFPS 
        { 
            get
            {
                int counter = m_LastTimeStepCounter;
                if (counter == 0)
                {
                    return 0;
                }

                int timesteps = 0;
                for (int i = 0; i < counter; ++i)
                {
                    timesteps += m_LastTimeSteps[i];
                }
                
                return 1000f * counter / timesteps;
            }
        }

        public double PhysicsDilationTime 
        {
            get
            {
                return m_PhysicsDilation / 1000f;
            }
        }

        public double PhysicsExecutionTime
        {
            get
            {
                return m_LastPhysicsExecutionTime / 1000f;
            }
        }

        int m_LastPhysicsTime;
        int[] m_LastTimeSteps = new int[60]; /* we have to do averaging due to round off errors */
        int m_LastTimeStepCounter = 0;
        bool m_First = true;
        BlockingQueue<int> m_NextFrameSignal = new BlockingQueue<int>();
        int m_PhysicsDilation = 0;
        int m_LastPhysicsSignalTime;
        int m_LastPhysicsExecutionTime;

        void PhysicsTrigger(object sender, ElapsedEventArgs e)
        {
            int newTickCount = Environment.TickCount;
            if(m_NextFrameSignal.Count > 0)
            {
                m_PhysicsDilation += (newTickCount - m_LastPhysicsSignalTime);
            }
            else
            {
                m_NextFrameSignal.Enqueue(0);
                m_PhysicsDilation = 0;
            }
            m_LastPhysicsSignalTime = newTickCount;
        }

        void PhysicsProcess()
        {
            int timeStepPos = 0;
            int lastTimeStep;
            while (!m_StopBulletThreads)
            {
                try
                {
                    m_NextFrameSignal.Dequeue(1000);
                }
                catch
                {
                    continue;
                }
                int NewPhysicsTime = Environment.TickCount;
                m_LastTimeSteps[timeStepPos++] = lastTimeStep = NewPhysicsTime - m_LastPhysicsTime;
                if (timeStepPos >= m_LastTimeSteps.Length)
                {
                    timeStepPos = 0;
                }
                if(m_LastTimeStepCounter < m_LastTimeSteps.Length)
                {
                    ++m_LastTimeStepCounter;
                }
                m_LastPhysicsTime = NewPhysicsTime;

                if (!m_First)
                {
                    m_DynamicsWorld.StepSimulation(lastTimeStep / 1000f);
                    m_LastPhysicsExecutionTime = Environment.TickCount - NewPhysicsTime;
                }
                m_First = false;
            }
        }
    }
}
