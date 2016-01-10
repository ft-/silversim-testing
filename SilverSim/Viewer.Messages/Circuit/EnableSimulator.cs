// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Net;

namespace SilverSim.Viewer.Messages.Circuit
{
    [UDPMessage(MessageType.EnableSimulator)]
    [Reliable]
    [EventQueueGet("EnableSimulator")]
    [Trusted]
    [UDPDeprecated]
    public class EnableSimulator : Message
    {
        public GridVector GridPosition;
        public IPAddress SimIP;
        public UInt16 SimPort;

        /* EQG extension */
        public GridVector RegionSize;

        public EnableSimulator()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUInt64(GridPosition.RegionHandle);
            p.WriteBytes(SimIP.GetAddressBytes());
            p.WriteUInt16(SimPort);
        }

        public override IValue SerializeEQG()
        {
            Types.Map i = new Types.Map();
            i.Add("Handle", new BinaryData(GridPosition.AsBytes));
            i.Add("IP", new BinaryData(SimIP.GetAddressBytes()));
            i.Add("Port", SimPort);
            i.Add("RegionSizeX", RegionSize.X);
            i.Add("RegionSizeY", RegionSize.Y);

            AnArray arr = new AnArray();
            arr.Add(i);

            SilverSim.Types.Map m = new SilverSim.Types.Map();
            m.Add("SimulatorInfo", arr);

            return m;
        }
    }
}
