using log4net;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Serialization;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        private static readonly ILog m_BakeLog = LogManager.GetLogger("AVATAR BAKING");

        UUID m_CurrentOutfitFolder = UUID.Zero;

        public UUID CurrentOutfitFolder
        {
            get
            {
                lock(this)
                {
                    return m_CurrentOutfitFolder;
                }
            }

            set
            {
                lock(this)
                {
                    m_CurrentOutfitFolder = value;
                }
            }
        }
    }
}
