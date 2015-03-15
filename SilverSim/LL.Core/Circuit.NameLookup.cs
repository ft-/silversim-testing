﻿/*

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

using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.Types;

namespace SilverSim.LL.Core
{
    public partial class Circuit
    {
        public void GroupNameLookup(Messages.Names.UUIDGroupNameRequest req)
        {
            Messages.Names.UUIDGroupNameReply rep = new Messages.Names.UUIDGroupNameReply();

            foreach(UUID id in req.UUIDNameBlock)
            {
                try
                {
                    rep.UUIDNameBlock.Add(new Messages.Names.UUIDGroupNameReply.Data(Scene.GroupsNameService[id]));
                }
                catch
                {
                    try
                    {
                        if (null != Agent.GroupsService)
                        {
                            rep.UUIDNameBlock.Add(new Messages.Names.UUIDGroupNameReply.Data(Agent.GroupsService.Groups[id]));
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

        public void UserNameLookup(Messages.Names.UUIDNameRequest req)
        {
            Messages.Names.UUIDNameReply rep = new Messages.Names.UUIDNameReply();

            foreach (UUID id in req.UUIDNameBlock)
            {
                try
                {
                    Messages.Names.UUIDNameReply.Data d = new Messages.Names.UUIDNameReply.Data();
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
