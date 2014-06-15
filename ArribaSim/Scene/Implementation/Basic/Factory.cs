/*

ArribaSim is distributed under the terms of the
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

using ArribaSim.Main.Common;
using ArribaSim.Scene.ServiceInterfaces.Chat;
using ArribaSim.Scene.ServiceInterfaces.Scene;
using ArribaSim.Scene.Types.Scene;
using ArribaSim.ServiceInterfaces.Asset;
using ArribaSim.ServiceInterfaces.Avatar;
using ArribaSim.ServiceInterfaces.Grid;
using ArribaSim.ServiceInterfaces.GridUser;
using ArribaSim.ServiceInterfaces.Groups;
using ArribaSim.ServiceInterfaces.IM;
using ArribaSim.ServiceInterfaces.Presence;
using ArribaSim.Types;
using Nini.Config;
using System.Net;

namespace ArribaSim.Scene.Implementation.Basic
{
    class SceneFactory : SceneFactoryInterface, IPlugin
    {
        public ChatServiceFactoryInterface m_ChatFactory;
        public string m_ChatFactoryName;
        public string m_PresenceServiceName;
        public string m_AvatarServiceName;
        public string m_GroupsServiceName;
        public string m_AssetServiceName;
        public string m_GridServiceName;
        public string m_GridUserServiceName;
        public string m_IMServiceName;

        public PresenceServiceInterface m_PresenceService;
        public AvatarServiceInterface m_AvatarService;
        public GroupsServiceInterface m_GroupsService = null;
        public AssetServiceInterface m_AssetService;
        public GridServiceInterface m_GridService;
        public GridUserServiceInterface m_GridUserService;
        public IMServiceInterface m_IMService;

        public SceneFactory(IConfig ownConfig)
        {
            m_ChatFactoryName = ownConfig.GetString("ChatService", "Chat");
            m_PresenceServiceName = ownConfig.GetString("PresenceService", "PresenceService");
            m_AvatarServiceName = ownConfig.GetString("AvatarService", "AvatarService");
            m_GroupsServiceName = ownConfig.GetString("GroupsService", "GroupsService");
            m_AssetServiceName = ownConfig.GetString("AssetService", "AssetService");
            m_GridServiceName = ownConfig.GetString("GridService", "GridService");
            m_GridUserServiceName = ownConfig.GetString("GridUserService", "GridUserService");
            m_IMServiceName = ownConfig.GetString("IMService", "IMService");
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_ChatFactory = loader.GetService<ChatServiceFactoryInterface>(m_ChatFactoryName);
            m_PresenceService = loader.GetService<PresenceServiceInterface>(m_PresenceServiceName);
            m_AvatarService = loader.GetService<AvatarServiceInterface>(m_AvatarServiceName);
            m_GroupsService = loader.GetService<GroupsServiceInterface>(m_GroupsServiceName);
            m_AssetService = loader.GetService<AssetServiceInterface>(m_AssetServiceName);
            m_GridService = loader.GetService<GridServiceInterface>(m_GridServiceName);
            m_GridUserService = loader.GetService<GridUserServiceInterface>(m_GridUserServiceName);
            m_IMService = loader.GetService<IMServiceInterface>(m_IMServiceName);
        }

        public override SceneInterface Instantiate(UUID id, GridVector position, uint sizeX, uint sizeY, IPAddress address, int port)
        {
            return new BasicScene(m_ChatFactory.Instantiate(), m_IMService, id, position, sizeX, sizeY, m_PresenceService, m_AvatarService, m_GroupsService, m_AssetService, m_GridService, m_GridUserService, address, port);
        }
    }

    public class Factory : IPluginFactory
    {
        public Factory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownConfig)
        {
            return new SceneFactory(ownConfig);
        }
    }
}
