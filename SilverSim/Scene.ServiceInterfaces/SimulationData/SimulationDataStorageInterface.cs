/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Scene.ServiceInterfaces.SimulationData
{
    public abstract class SimulationDataStorageInterface
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
    }
}
