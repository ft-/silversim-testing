// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Scene;
using System;
using System.Threading;

namespace SilverSim.Scene.Physics.Bullet.Implementation
{
    public partial class BulletScene : IPhysicsScene, ISceneListener, ITerrainListener
    {
        SceneInterface m_Scene;

        public BulletScene(SceneInterface scene)
        {
            m_Scene = scene;
            InitializeTerrainMesh();
            new Thread(BulletTerrainUpdateThread).Start();
            new Thread(BulletAddThread).Start();
            new Thread(BulletUpdateThread).Start();
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
