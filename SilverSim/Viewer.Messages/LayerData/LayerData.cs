// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.Viewer.Messages.LayerData
{
    [UDPMessage(MessageType.LayerData)]
    [Reliable]
    [Trusted]
    public class LayerData : Message
    {
        public enum LayerDataType : byte
        {
            Invalid = 0,
            Land = 0x4C,
            LandExtended = 0x4D,
            Water = 0x57,
            WaterExtended = 0x58,
            Wind = 0x37,
            WindExtended = 0x39,
            Cloud = 0x38,
            CloudExtended = 0x3A
        }

        public LayerDataType LayerType;
        public byte[] Data = new byte[0];

        public LayerData()
        {

        }

        public override string TypeDescription
        {
            get
            {
                return Number.ToString()+ "." + LayerType.ToString();
            }
        }

        public override bool ZeroFlag
        {
            get
            {
                /* determine zero flag efficiency */
                bool zerflag = false;
                int zercnt = 0;
                int bytecnt = 0;
                for (int idx = 0; idx < Data.Length; ++idx)
                {
                    if(Data[idx] == 0)
                    {
                        if(!zerflag)
                        {
                            bytecnt += 2;
                        }
                        else if(zercnt == 255)
                        {
                            bytecnt += 2;
                            zercnt = 0;
                        }
                        ++zercnt;
                        zerflag = true;
                    }
                    else
                    {
                        zercnt = 0;
                        zerflag = false;
                        ++bytecnt;
                    }
                }

                return (bytecnt < Data.Length);
            }
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUInt8((byte)LayerType);
            p.WriteUInt16((UInt16)Data.Length);
            p.WriteBytes(Data);
        }
    }
}
