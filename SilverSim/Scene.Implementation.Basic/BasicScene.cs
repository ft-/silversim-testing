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
using SilverSim.Scene.Implementation.Common;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Scripting.Common;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Grid;
using SilverSim.Types.Parcel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SilverSim.Scene.Implementation.Basic
{
    public class BasicScene : SceneImplementation
    {
        private static readonly ILog m_Log = LogManager.GetLogger("BASIC SCENE");

        #region Fields
        protected internal readonly RwLockedDoubleDictionary<UUID, UInt32, ObjectPart> m_Primitives = new RwLockedDoubleDictionary<UUID, UInt32, ObjectPart>();
        protected internal readonly RwLockedDictionary<UUID, IObject> m_Objects = new RwLockedDictionary<UUID, IObject>();
        protected internal readonly RwLockedDictionary<UUID, IAgent> m_Agents = new RwLockedDictionary<UUID, IAgent>();
        #endregion

        #region Interface wrappers
        public sealed class BasicSceneObjectsCollection : ISceneObjects
        {
            private readonly BasicScene m_Scene;

            internal BasicSceneObjectsCollection(BasicScene scene)
            {
                m_Scene = scene;
            }

            public IObject this[UUID id] => m_Scene.m_Objects[id];

            public bool TryGetValue(UUID id, out IObject obj) => m_Scene.m_Objects.TryGetValue(id, out obj);

            public int Count => m_Scene.m_ObjectCount;

            public void ForEach(Vector3 pos, double maxdistance, Action<IObject> d)
            {
                double maxDistanceSquared = maxdistance * maxdistance;
                foreach(IObject obj in m_Scene.Objects)
                {
                    if ((obj.GlobalPosition - pos).LengthSquared <= maxDistanceSquared && obj.IsInScene(m_Scene))
                    {
                        d(obj);
                    }
                }
            }

            public IEnumerator<IObject> GetEnumerator() => m_Scene.m_Objects.Values.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public sealed class BasicSceneObjectPartsCollection : ISceneObjectParts
        {
            private readonly BasicScene m_Scene;

            internal BasicSceneObjectPartsCollection(BasicScene scene)
            {
                m_Scene = scene;
            }

            public ObjectPart this[UUID id] => m_Scene.m_Primitives[id];

            public ObjectPart this[UInt32 localId] => m_Scene.m_Primitives[localId];

            public bool TryGetValue(UUID id, out ObjectPart part) => m_Scene.m_Primitives.TryGetValue(id, out part);

            public bool TryGetValue(UInt32 localid, out ObjectPart part) => m_Scene.m_Primitives.TryGetValue(localid, out part);

            public bool TryGetValueByName(string name, out ObjectPart part)
            {
                part = null;
                foreach(ObjectPart p in this)
                {
                    if(p.Name == name)
                    {
                        part = p;
                        return true;
                    }
                }
                return false;
            }

            public int Count => m_Scene.m_PrimitiveCount;

            public IEnumerator<ObjectPart> GetEnumerator() => m_Scene.m_Primitives.Values.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public sealed class BasicSceneParcelsCollection : ISceneParcels
        {
            private readonly BasicScene m_Scene;
            private readonly object m_SceneUpdateLock = new object();

            internal BasicSceneParcelsCollection(BasicScene scene)
            {
                m_Scene = scene;
            }

            public IParcelAccessList WhiteList => m_Scene.m_SimulationDataStorage.Parcels.WhiteList;

            public IParcelAccessList BlackList => m_Scene.m_SimulationDataStorage.Parcels.BlackList;

            public IParcelExperienceList Experiences => m_Scene.m_SimulationDataStorage.Parcels.Experiences;

            public ParcelInfo this[UUID id] => m_Scene.m_Parcels[id];

            public ParcelInfo this[Vector3 pos]
            {
                get
                {
                    var x = (int)pos.X;
                    var y = (int)pos.Y;
                    if(pos.X < 0 || pos.Y < 0 || x < 0 || y < 0 || x >= m_Scene.SizeX || y >= m_Scene.SizeY)
                    {
                        throw new KeyNotFoundException();
                    }
                    foreach(ParcelInfo p in m_Scene.m_Parcels.Values)
                    {
                        if(p.LandBitmap[x / 4, y / 4])
                        {
                            return p;
                        }
                    }
                    throw new KeyNotFoundException();
                }
            }

            public ParcelInfo this[int localID] => m_Scene.Parcels[localID];

            public bool TryGetValue(UUID id, out ParcelInfo pinfo) => m_Scene.m_Parcels.TryGetValue(id, out pinfo);

            public bool TryGetValue(Vector3 pos, out ParcelInfo pinfo)
            {
                pinfo = null;
                var x = (int)pos.X;
                var y = (int)pos.Y;
                if (pos.X < 0 || pos.Y < 0 || x < 0 || y < 0 || x >= m_Scene.SizeX || y >= m_Scene.SizeY)
                {
                    return false;
                }
                foreach (ParcelInfo p in m_Scene.m_Parcels.Values)
                {
                    if (p.LandBitmap[x / 4, y / 4])
                    {
                        pinfo = p;
                        return true;
                    }
                }
                return false;
            }

            public bool TryGetValue(int localID, out ParcelInfo pinfo) => m_Scene.m_Parcels.TryGetValue(localID, out pinfo);

            public IEnumerator<ParcelInfo> GetEnumerator() => m_Scene.m_Parcels.Values.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public void Add(ParcelInfo parcelInfo)
            {
                lock (m_SceneUpdateLock)
                {
                    m_Scene.AddParcel(parcelInfo);
                    m_Scene.m_SimulationDataStorage.Parcels.Store(m_Scene.ID, parcelInfo);
                }
            }

            public void Store(UUID parcelID)
            {
                lock(m_SceneUpdateLock)
                {
                    ParcelInfo pInfo;
                    if (m_Scene.m_Parcels.TryGetValue(parcelID, out pInfo))
                    {
                        m_Scene.m_SimulationDataStorage.Parcels.Store(m_Scene.ID, pInfo);
                    }
                }
            }

            public bool Remove(UUID parcelID)
            {
                lock (m_SceneUpdateLock)
                {
                    bool res = m_Scene.m_Parcels.Remove(parcelID);
                    res = res && m_Scene.m_SimulationDataStorage.Parcels.Remove(m_Scene.ID, parcelID);
                    return res;
                }
            }

            public void ResetParcels()
            {
            }
        }

        public sealed class BasicSceneAgentsCollection : ISceneAgents
        {
            private readonly BasicScene m_BasicScene;

            internal BasicSceneAgentsCollection(BasicScene scene)
            {
                m_BasicScene = scene;
            }

            public int Count => m_BasicScene.m_AgentCount;

            public IAgent this[UUID id] => m_BasicScene.m_Agents[id];

            public bool TryGetValue(UUID id, out IAgent obj) => m_BasicScene.m_Agents.TryGetValue(id, out obj);

            public IEnumerator<IAgent> GetEnumerator() => m_BasicScene.m_Agents.Values.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public sealed class BasicSceneRootAgentsCollection : ISceneAgents
        {
            private readonly BasicScene m_BasicScene;

            internal BasicSceneRootAgentsCollection(BasicScene scene)
            {
                m_BasicScene = scene;
            }

            public int Count
            {
                get
                {
                    int count = 0;
                    foreach(IAgent agent in this)
                    {
                        if (agent.IsInScene(m_BasicScene))
                        {
                            ++count;
                        }
                    }
                    return count;
                }
            }

            public IAgent this[UUID id]
            {
                get
                {
                    IAgent ag = m_BasicScene.m_Agents[id];
                    if(!ag.IsInScene(m_BasicScene))
                    {
                        throw new KeyNotFoundException();
                    }
                    return ag;
                }
            }

            public bool TryGetValue(UUID id, out IAgent obj)
            {
                if(!m_BasicScene.m_Agents.TryGetValue(id, out obj))
                {
                    return false;
                }
                if(!obj.IsInScene(m_BasicScene))
                {
                    obj = null;
                    return false;
                }
                return true;
            }

            public IEnumerator<IAgent> GetEnumerator()
            {
                ICollection<IAgent> agents = m_BasicScene.m_Agents.Values;
                List<IAgent> rootAgents = new List<IAgent>();
                foreach(IAgent agent in agents)
                {
                    if(agent.IsInScene(m_BasicScene))
                    {
                        rootAgents.Add(agent);
                    }
                }
                return rootAgents.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
        #endregion

        #region Services
        private readonly BasicSceneObjectsCollection m_SceneObjects;
        private readonly BasicSceneParcelsCollection m_SceneParcels;
        private readonly BasicSceneObjectPartsCollection m_SceneObjectParts;
        private readonly DefaultSceneObjectGroupInterface m_SceneObjectGroups;
        private readonly BasicSceneAgentsCollection m_SceneAgents;
        private readonly BasicSceneRootAgentsCollection m_SceneRootAgents;

        #endregion

        #region Constructor
        internal BasicScene(
            SceneFactory sceneParams,
            RegionInfo ri)
        : base(sceneParams, ri)
        {
            m_SceneObjects = new BasicSceneObjectsCollection(this);
            m_SceneObjectParts = new BasicSceneObjectPartsCollection(this);
            m_SceneObjectGroups = new DefaultSceneObjectGroupInterface(this);
            m_SceneAgents = new BasicSceneAgentsCollection(this);
            m_SceneRootAgents = new BasicSceneRootAgentsCollection(this);
            m_SceneParcels = new BasicSceneParcelsCollection(this);

            StartScene(sceneParams);
        }
        #endregion

        #region Properties

        public override List<ObjectUpdateInfo> UpdateInfos
        {
            get
            {
                var infos = new List<ObjectUpdateInfo>();
                foreach(IObject obj in m_Objects.Values)
                {
                    var objgrp = obj as ObjectGroup;
                    if (objgrp != null)
                    {
                        foreach (ObjectPart part in objgrp.ValuesByKey1)
                        {
                            infos.Add(part.UpdateInfo);
                        }
                    }
                }
                return infos;
            }
        }

        public override ISceneObjects Objects => m_SceneObjects;

        public override ISceneAgents Agents => m_SceneAgents;

        public override ISceneAgents RootAgents => m_SceneRootAgents;

        public override ISceneObjectGroups ObjectGroups => m_SceneObjectGroups;

        public override ISceneObjectParts Primitives => m_SceneObjectParts;

        public override ISceneParcels Parcels => m_SceneParcels;
        #endregion

        #region Add and Remove
        internal int m_ObjectCount;
        internal int m_PrimitiveCount;
        internal int m_AgentCount;

        public override void AddObjectGroupOnly(IObject obj)
        {
            var objgroup = obj as ObjectGroup;
            if (objgroup != null)
            {
                objgroup.Scene = this;
                m_Objects.Add(objgroup.ID, objgroup);
                Interlocked.Increment(ref m_ObjectCount);
            }
        }

        public override void Add(IObject obj)
        {
            var objgroup = obj as ObjectGroup;
            if (objgroup != null)
            {
                var removeAgain = new List<ObjectPart>();

                AddLegacyMaterials(objgroup);

                try
                {
                    objgroup.Scene = this;
                    foreach (ObjectPart objpart in objgroup.Values)
                    {
                        AddNewLocalID(objpart);
                        m_Primitives.Add(objpart.ID, objpart.LocalID, objpart);
                        removeAgain.Add(objpart);
                    }
                    m_Objects.Add(objgroup.ID, objgroup);

                    foreach(ObjectPart objpart in objgroup.Values)
                    {
                        Interlocked.Increment(ref m_PrimitiveCount);
                        objpart.SendObjectUpdate();
                    }
                    Interlocked.Increment(ref m_ObjectCount);
                }
                catch(Exception e)
                {
                    m_Log.DebugFormat("Failed to add object: {0}: {1}\n{2}", e.GetType().FullName, e.Message, e.StackTrace);
                    m_Objects.Remove(objgroup.ID);
                    foreach (ObjectPart objpart in removeAgain)
                    {
                        m_Primitives.Remove(objpart.ID);
                        RemoveLocalID(objpart);
                    }
                    objgroup.Scene = null;
                }
            }
            else
            {
                AddNewLocalID(obj);
                try
                {
                    m_Objects.Add(obj.ID, obj);
                    if (obj.GetType().GetInterfaces().Contains(typeof(IAgent)))
                    {
                        var agent = (IAgent)obj;
                        m_Agents.Add(obj.ID, agent);
                        Interlocked.Increment(ref m_AgentCount);
                        PhysicsScene.Add(obj);
                        foreach (IAgentListener aglistener in AgentListeners)
                        {
                            try
                            {
                                aglistener.AddedAgent(agent);
                            }
                            catch(Exception e)
                            {
                                m_Log.DebugFormat("Exception {0}\n{1}", e.Message, e.StackTrace);
                            }
                        }
                    }
                }
                catch
                {
                    if(m_Agents.Remove(obj.ID))
                    {
                        Interlocked.Decrement(ref m_AgentCount);
                    }
                    m_Objects.Remove(obj.ID);
                    RemoveLocalID(obj);
                }
            }
        }

        public override bool RemoveObjectGroupOnly(UUID objID)
        {
            if (m_Objects.Remove(objID))
            {
                Interlocked.Decrement(ref m_ObjectCount);
                return true;
            }
            return false;
        }

        public override bool Remove(IObject obj, ScriptInstance instance = null)
        {
            if(!m_Objects.ContainsValue(obj))
            {
                return false;
            }
            var objgroup = obj as ObjectGroup;
            IAgent agent;
            if (objgroup != null)
            {
                foreach (ObjectPart objpart in objgroup.Values)
                {
                    Interlocked.Decrement(ref m_PrimitiveCount);
                    foreach(ObjectPartInventoryItem item in objpart.Inventory.Values)
                    {
                        ScriptInstance removeinstance = item.ScriptInstance;
                        if (removeinstance != instance)
                        {
                            removeinstance = item.RemoveScriptInstance;
                            if(removeinstance != null)
                            {
                                ScriptThreadPool.AbortScript(removeinstance);
                                removeinstance.Abort();
                                removeinstance.Remove();
                                ScriptLoader.Remove(item.AssetID, removeinstance);
                            }
                        }
                    }
                    m_Primitives.Remove(objpart.ID);
                    objpart.SendKillObject();
                    RemoveLocalID(objpart);
                }
                
                if(m_Objects.Remove(objgroup.ID))
                {
                    Interlocked.Decrement(ref m_ObjectCount);
                }
            }
            else if((agent = obj as IAgent) != null)
            {
                foreach (IAgentListener aglistener in AgentListeners)
                {
                    try
                    {
                        aglistener.RemovedAgent(agent);
                    }
                    catch (Exception e)
                    {
                        m_Log.DebugFormat("Exception {0}\n{1}", e.Message, e.StackTrace);
                    }
                }

                if (agent.IsInScene(this))
                {
                    /* only detach if agent is at our scene */
                    agent.DetachAllAttachments();
                }
                PhysicsScene.Remove(agent);
                m_Objects.Remove(agent.ID);
                if(m_Agents.Remove(agent.ID))
                {
                    Interlocked.Decrement(ref m_AgentCount);
                }
                SendKillObjectToAgents(agent.LocalID);
                RemoveLocalID(agent);
                Interlocked.Decrement(ref m_AgentCount);
            }
            else
            {
                m_Objects.Remove(obj.ID);
                SendKillObjectToAgents(obj.LocalID);
                RemoveLocalID(obj);
            }

            return true;
        }
        #endregion

    }
}
