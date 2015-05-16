using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.LL.Messages
{
    public class CancelTxThread : Message
    {
        public CancelTxThread()
        {

        }

        public override MessageType Number
        {
            get
            {
                return 0;
            }
        }
    }
}
