// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using System.Collections.Generic;
using ThreadedClasses;

namespace SilverSim.Scene.ServiceInterfaces.SimulationData
{
    public abstract class SimulationDataStorageInterface : ISceneListener
    {
        #region Constructor
        public SimulationDataStorageInterface()
        {
        }
        #endregion

        public abstract SimulationDataObjectStorageInterface Objects
        {
            get;
        }

        public abstract SimulationDataParcelStorageInterface Parcels
        {
            get;
        }

        public abstract SimulationDataScriptStateStorageInterface ScriptStates
        {
            get;
        }

        public abstract SimulationDataTerrainStorageInterface Terrains
        {
            get;
        }

        public abstract SimulationDataEnvSettingsStorageInterface EnvironmentSettings
        {
            get;
        }

        public void StoreScene(SceneInterface scene)
        {
            #region Store Objects
            List<UUID> objectsToDelete = Objects.ObjectsInRegion(scene.ID);
            List<UUID> primsToDelete = Objects.PrimitivesInRegion(scene.ID);
            foreach(ObjectGroup objgroup in scene.Objects)
            {
                if(objgroup.IsTemporary)
                {
                    /* Do not store temporary objects */
                    continue;
                }

                objectsToDelete.Remove(objgroup.ID);
                foreach (ObjectPart objpart in objgroup.Values)
                {
                    primsToDelete.Remove(objpart.ID);
                    if(!objgroup.IsChanged && objpart.IsChanged)
                    {
                        Objects.UpdateObjectPart(objpart);
                    }
                    else
                    {
                        Objects.UpdateObjectPartInventory(objpart);
                    }
                }

                Objects.UpdateObjectGroup(objgroup);
            }

            foreach(UUID id in primsToDelete)
            {
                Objects.DeleteObjectPart(id);
            }

            foreach(UUID id in objectsToDelete)
            {
                Objects.DeleteObjectGroup(id);
            }
            #endregion
        }

        readonly BlockingQueue<KeyValuePair<ObjectUpdateInfo, UUID>> m_StorageRequestQueue = new BlockingQueue<KeyValuePair<ObjectUpdateInfo, UUID>>();
        bool m_StopStorageThread = false;

        protected void StopStorageThread()
        {
            m_StopStorageThread = true;
        }

        protected void StorageThread()
        {
            while(!m_StopStorageThread || m_StorageRequestQueue.Count != 0)
            {
                /* thread always runs until queue is empty it does not stop before */
                KeyValuePair<ObjectUpdateInfo, UUID> req;
                ObjectUpdateInfo info;
                try
                {
                    req = m_StorageRequestQueue.Dequeue(1000);
                }
                catch
                {
                    continue;
                }

                info = req.Key;

                if(info.IsKilled)
                {
                    Objects.DeleteObjectPart(info.Part.ID);
                    Objects.DeleteObjectGroup(info.Part.ObjectGroup.ID);
                }
                else
                {
                    ObjectGroup grp = info.Part.ObjectGroup;
                    if(null != grp && !grp.IsTemporary)
                    {
                        Objects.UpdateObjectGroup(grp);
                        Objects.UpdateObjectPart(info.Part);
                    }
                }
            }
        }

        public void ScheduleUpdate(ObjectUpdateInfo info, UUID fromSceneID)
        {
            m_StorageRequestQueue.Enqueue(new KeyValuePair<ObjectUpdateInfo, UUID>(info, fromSceneID));
        }
    }
}
