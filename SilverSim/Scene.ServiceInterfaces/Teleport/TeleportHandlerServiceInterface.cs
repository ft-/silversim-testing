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

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.Types;
using SilverSim.Types.Grid;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace SilverSim.ServiceInterfaces.Teleport
{
    public abstract class TeleportHandlerServiceInterface : IAgentTeleportServiceInterface
    {
        private IAgent m_Agent;
        public IAgent Agent
        {
            get
            {
                return m_Agent;
            }
            set
            {
                if(null != Interlocked.CompareExchange(ref m_Agent, value, null))
                {
                    throw new ArgumentException(nameof(value));
                }
            }
        }

        public abstract void Cancel();
        /* <summary>this is the local call for active teleport</summary> */
        [Description("Local call from remote call handlers")]
        public abstract void ReleaseAgent(UUID fromSceneID);
        public abstract void CloseAgentOnRelease(UUID fromSceneID);
        public abstract void EnableSimulator(UUID fromSceneID, DestinationInfo destinationRegion);
        public abstract void DisableSimulator(UUID fromSceneID, RegionInfo regionInfo);
        /* <summary>this is the remote call</summary> */
        [Description("Remote call to other simulators")]
        public abstract void ReleaseAgent(UUID fromSceneID, RegionInfo regionInfo);
        public abstract GridType GridType { get; }

        public abstract bool TeleportTo(SceneInterface sceneInterface, string regionName, Vector3 position, Vector3 lookAt, TeleportFlags flags);

        public virtual bool TeleportTo(SceneInterface sceneInterface, GridVector location, Vector3 position, Vector3 lookAt, TeleportFlags flags) =>
            TeleportTo(sceneInterface, sceneInterface.GatekeeperURI, location, position, lookAt, flags);

        public abstract bool TeleportTo(SceneInterface sceneInterface, string gatekeeperURI, GridVector location, Vector3 position, Vector3 lookAt, TeleportFlags flags);

        public virtual bool TeleportTo(SceneInterface sceneInterface, UUID regionID, Vector3 position, Vector3 lookAt, TeleportFlags flags) =>
            TeleportTo(sceneInterface, sceneInterface.GatekeeperURI, regionID, position, lookAt, flags);

        public abstract bool TeleportTo(SceneInterface sceneInterface, string gatekeeperURI, UUID regionID, Vector3 position, Vector3 lookAt, TeleportFlags flags);

        /* following function returns true if it accepts a teleport request or if it wants to distribute more specific error message except home location not available */
        public abstract bool TeleportHome(SceneInterface sceneInterface);

        public abstract string Name { get; }
        public abstract bool IsProtocolSupported(string url);
        public abstract bool IsProtocolSupported(string url, Dictionary<string, string> cachedheaders);
    }
}
