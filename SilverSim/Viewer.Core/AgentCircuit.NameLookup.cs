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

using SilverSim.Types;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Names;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
    {
        [PacketHandler(MessageType.UUIDGroupNameRequest)]
        public void GroupNameLookup(Message m)
        {
            var req = (UUIDGroupNameRequest)m;
            var rep = new UUIDGroupNameReply();
            var groupsService = Scene.GroupsService;

            foreach(var id in req.UUIDNameBlock)
            {
                try
                {
                    rep.UUIDNameBlock.Add(new UUIDGroupNameReply.Data(Scene.GroupsNameService[id]));
                }
                catch
                {
                    try
                    {
                        if (groupsService != null)
                        {
                            rep.UUIDNameBlock.Add(new UUIDGroupNameReply.Data(groupsService.Groups[Agent.Owner, id]));
                        }
                    }
                    catch
                    {
                        /* no action possible */
                    }
                }
            }
            if (rep.UUIDNameBlock.Count != 0)
            {
                SendMessage(rep);
            }
        }

        [PacketHandler(MessageType.UUIDNameRequest)]
        public void UserNameLookup(Message m)
        {
            var req = (UUIDNameRequest)m;
            var rep = new UUIDNameReply();

            foreach (var id in req.UUIDNameBlock)
            {
                try
                {
                    var d = new UUIDNameReply.Data();
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
