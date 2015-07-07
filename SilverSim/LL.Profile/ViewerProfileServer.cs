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

using Nini.Config;
using SilverSim.LL.Core;
using SilverSim.LL.Messages;
using SilverSim.Main.Common;
using System.Collections.Generic;
using System.Threading;
using ThreadedClasses;

namespace SilverSim.LL.Profile
{
    public class ViewerProfileServer : IPlugin, IPacketHandlerExtender, ICapabilityExtender, IPluginShutdown
    {
        [PacketHandler(MessageType.DirClassifiedQuery)]
        [PacketHandler(MessageType.ClassifiedInfoRequest)]
        [PacketHandler(MessageType.ClassifiedInfoUpdate)]
        [PacketHandler(MessageType.ClassifiedDelete)]
        [PacketHandler(MessageType.ClassifiedGodDelete)]
        [PacketHandler(MessageType.AvatarPropertiesRequest)]
        [PacketHandler(MessageType.AvatarPropertiesUpdate)]
        [PacketHandler(MessageType.AvatarInterestsUpdate)]
        [PacketHandler(MessageType.AvatarNotesUpdate)]
        [PacketHandler(MessageType.PickInfoUpdate)]
        [PacketHandler(MessageType.PickDelete)]
        [PacketHandler(MessageType.PickGodDelete)]
        [PacketHandler(MessageType.UserInfoRequest)]
        [PacketHandler(MessageType.UpdateUserInfo)]
        BlockingQueue<KeyValuePair<Circuit, Message>> RequestQueue = new BlockingQueue<KeyValuePair<Circuit, Message>>();
        bool m_ShutdownProfile = false;

        public ViewerProfileServer()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {
            new Thread(HandlerThread).Start();
        }

        public void HandlerThread()
        {
            Thread.CurrentThread.Name = "Profile Handler Thread";

            while (!m_ShutdownProfile)
            {
                KeyValuePair<Circuit, Message> req;
                try
                {
                    req = RequestQueue.Dequeue(1000);
                }
                catch
                {
                    continue;
                }

                Message m = req.Value;

            }
        }

        public ShutdownOrder ShutdownOrder
        {
            get 
            {
                return ShutdownOrder.LogoutRegion;
            }
        }

        public void Shutdown()
        {
            m_ShutdownProfile = true;
        }
    }

    [PluginName("ViewerProfileServer")]
    public class Factory : IPluginFactory
    {
        public Factory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new ViewerProfileServer();
        }
    }
}
