// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.LL.Messages;
using SilverSim.LL.Messages.Names;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;

namespace SilverSim.LL.Core
{
    public partial class Circuit
    {
        [PacketHandler(MessageType.UUIDGroupNameRequest)]
        void GroupNameLookup(Message m)
        {
            UUIDGroupNameRequest req = (UUIDGroupNameRequest)m;
            UUIDGroupNameReply rep = new UUIDGroupNameReply();
            GroupsServiceInterface groupsService = Scene.GroupsService;

            foreach(UUID id in req.UUIDNameBlock)
            {
                try
                {
                    rep.UUIDNameBlock.Add(new UUIDGroupNameReply.Data(Scene.GroupsNameService[id]));
                }
                catch
                {
                    try
                    {
                        if (null != groupsService)
                        {
                            rep.UUIDNameBlock.Add(new UUIDGroupNameReply.Data(groupsService.Groups[Agent.Owner, id]));
                        }
                    }
                    catch
                    {
                    }
                }
            }
            if (rep.UUIDNameBlock.Count != 0)
            {
                SendMessage(rep);
            }
        }

        [PacketHandler(MessageType.UUIDNameRequest)]
        void UserNameLookup(Message m)
        {
            UUIDNameRequest req = (UUIDNameRequest)m;
            UUIDNameReply rep = new UUIDNameReply();

            foreach (UUID id in req.UUIDNameBlock)
            {
                try
                {
                    UUIDNameReply.Data d = new UUIDNameReply.Data();
                    UUI nd = Scene.AvatarNameService[id];
                    d.ID = nd.ID;
                    d.FirstName = nd.FirstName;
                    d.LastName = nd.LastName;
                    rep.UUIDNameBlock.Add(d);
                }
                catch
                {
                    /* TODO: eventually make up an AvatarName lookup based on ServiceURLs */
                }
            }
            if (rep.UUIDNameBlock.Count != 0)
            {
                SendMessage(rep);
            }
        }
    }
}
