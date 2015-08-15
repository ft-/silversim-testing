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
        }

        public void Shutdown()
        {
            m_StopBulletThreads = true;
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
    }
}
