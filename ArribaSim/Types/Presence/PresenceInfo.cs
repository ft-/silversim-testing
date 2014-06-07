using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArribaSim.Types.Presence
{
    public class PresenceInfo
    {
        public UUI UserID = UUI.Unknown;
        public UUID RegionID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public PresenceInfo()
        {

        }
    }
}
