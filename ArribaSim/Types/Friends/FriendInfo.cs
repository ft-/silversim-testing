using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArribaSim.Types.Friends
{
    public class FriendInfo
    {
        public UUI User = UUI.Unknown;
        public UUI Friend = UUI.Unknown;
        public int UserGivenFlags = 0;
        public int FriendGivenFlags = 0;

        public FriendInfo()
        {

        }
    }
}
