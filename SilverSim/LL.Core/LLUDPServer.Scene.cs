// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using SilverSim.LL.Messages;
using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Types.Economy;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.Types;
using SilverSim.Main.Common;
using SilverSim.Types.IM;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ThreadedClasses;
using SilverSim.Scene.Types.Object;

namespace SilverSim.LL.Core
{
    public partial class LLUDPServer
    {
        public void ScheduleUpdate(ObjectUpdateInfo info)
        {
            m_Circuits.ForEach(delegate(Circuit circ)
            {
                circ.ScheduleUpdate(info);
            });
        }
    }
}
