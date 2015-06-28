/*

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
using SilverSim.Types;

namespace SilverSim.LL.Messages
{
    public abstract class Message
    {
        #region Message Type
        public enum MessagePriority
        {
            High,
            Medium,
            Low
        }

        public enum QueueOutType : uint
        {
            High,
            Resend,
            LandLayerData,
            WindLayerData,
            GenericLayerData,
            Medium,
            Low,
            Asset,
            TextureStart,
            Texture,
            Object,

            NumQueues, /* must be last */
        }

        public QueueOutType OutQueue = QueueOutType.Low;

        public UInt32 ReceivedOnCircuitCode;
        public delegate void Send(UInt32 circuitCode, Message m);
        public UUID CircuitSessionID = UUID.Zero;
        public UUID CircuitAgentID = UUID.Zero;
        public UUID CircuitSceneID = UUID.Zero;
        public UUI CircuitAgentOwner = UUI.Unknown;

        public MessagePriority Type
        {
            get
            {
                if((UInt32)Number <= 0xFE)
                {
                    return MessagePriority.High;
                }
                else if ((UInt32)Number <= 0xFFFE)
                {
                    return MessagePriority.Medium;
                }
                else
                {
                    return MessagePriority.Low;
                }
            }
        }
        #endregion

        public bool ForceZeroFlag = false;

        #region Overloaded methods
        public virtual string TypeDescription
        {
            get
            {
                return Number.ToString();
            }
        }

        public virtual bool IsReliable
        {
            get 
            {
                return false;
            }
        }

        public virtual bool ZeroFlag
        {
            get
            {
                return false;
            }
        }

        public abstract MessageType Number
        {
            get;
        }

        public virtual void Serialize(UDPPacket p)
        {
            throw new NotSupportedException();
        }

        public virtual Types.IValue SerializeEQG()
        {
            throw new NotSupportedException();
        }

        public virtual string NameEQG
        {
            get
            {
                throw new NotSupportedException();
            }
        }
        #endregion
    }
}
