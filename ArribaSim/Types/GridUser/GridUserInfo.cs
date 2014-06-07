using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArribaSim.Types.GridUser
{
    public class GridUserInfo
    {
        public GridUserInfo()
        {

        }

        public UUI UserID = UUI.Unknown;
        public UUID HomeRegionID = UUID.Zero;
        public Vector3 HomePosition = Vector3.Zero;
        public Vector3 HomeLookAt = Vector3.Zero;
        public UUID LastRegionID = UUID.Zero;
        public Vector3 LastPosition = Vector3.Zero;
        public Vector3 LastLookAt = Vector3.Zero;
        public bool Online = false;
        public Date LastLogin = new Date();
        public Date LastLogout = new Date();
    }
}
