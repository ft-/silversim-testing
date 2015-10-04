// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.Viewer.Messages
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
