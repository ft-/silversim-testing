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

using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Types;
using SilverSim.Viewer.Core;
using System.ComponentModel;
using System.Net;

namespace SilverSim.UserCaps.FetchInventory2
{
    [PluginName("SimFetchInventory2")]
    [Description("FetchInventory2 support")]
    [ServerParam("GridLibraryOwner", ParameterType = typeof(UUID), Type = ServerParamType.GlobalOnly, DefaultValue = "11111111-1111-0000-0000-000100bba000")]
    public sealed class SimFetchInventory2 : FetchInventory2Base, IPlugin, ICapabilityExtender
    {
        private readonly object m_ConfigUpdateLock = new object();
        private UUID m_GridLibraryOwner = new UUID("11111111-1111-0000-0000-000100bba000");

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [CapabilityHandler("FetchInventory2")]
        public void HandleFetchInventory2(ViewerAgent agent, AgentCircuit circuit, HttpRequest req)
        {
            if (req.CallerIP != circuit.RemoteIP)
            {
                req.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }

            HandleHttpRequest(req, agent.InventoryService, agent.Owner.ID, agent.Owner.ID);
        }

        [CapabilityHandler("FetchLib2")]
        public void HandleFetchLib2(ViewerAgent agent, AgentCircuit circuit, HttpRequest req)
        {
            if (req.CallerIP != circuit.RemoteIP)
            {
                req.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }

            UUID libraryOwner;
            lock(m_ConfigUpdateLock)
            {
                libraryOwner = m_GridLibraryOwner;
            }

            HandleHttpRequest(req, agent.InventoryService, agent.Owner.ID, libraryOwner);
        }

        [ServerParam("GridLibraryOwner")]
        public void HandleGridLibraryOwner(UUID regionid, string value)
        {
            if (regionid != UUID.Zero)
            {
                return;
            }
            lock (m_ConfigUpdateLock)
            {
                if (string.IsNullOrEmpty(value))
                {
                    m_GridLibraryOwner = new UUID("11111111-1111-0000-0000-000100bba000");
                }
                else if (!UUID.TryParse(value, out m_GridLibraryOwner))
                {
                    m_GridLibraryOwner = UUID.Zero;
                }
            }
        }
    }
}
