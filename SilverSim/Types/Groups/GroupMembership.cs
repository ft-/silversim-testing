// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Types.Groups
{
    public class GroupMembership
    {
        public UGI Group = UGI.Unknown;
        public UUI Principal = UUI.Unknown;
        public GroupPowers GroupPowers = GroupPowers.None;
        public bool AcceptNotices;
        public UUID GroupInsigniaID = UUID.Zero;
        public Int32 Contribution;
        public string GroupTitle = string.Empty;
        public bool ListInProfile;

        public GroupMembership()
        {

        }
    }
}
