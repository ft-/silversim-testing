// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Names;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
    {
        [PacketHandler(MessageType.UUIDGroupNameRequest)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
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
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
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
