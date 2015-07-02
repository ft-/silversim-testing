﻿/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using System;

namespace SilverSim.LL.Messages.LayerData
{
    [UDPMessage(MessageType.LayerData)]
    [Reliable]
    public class LayerData : Message
    {
        public enum LayerDataType : byte
        {
            Land = 0x4C,
            LandExtended = 0x4D,
            Water = 0x57,
            WaterExtended = 0x58,
            Wind = 0x37,
            WindExtended = 0x39,
            Cloud = 0x38,
            CloudExtended = 0x3A
        }

        public LayerDataType LayerType = 0;
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
            p.WriteMessageType(Number);
            p.WriteUInt8((byte)LayerType);
            p.WriteUInt16((UInt16)Data.Length);
            p.WriteBytes(Data);
        }
    }
}
