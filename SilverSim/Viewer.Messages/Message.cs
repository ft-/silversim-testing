// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using SilverSim.Types;
using SilverSim.Types.IM;
using System;
using System.Linq;

namespace SilverSim.Viewer.Messages
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ReliableAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ZerocodedAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class TrustedAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class NotTrustedAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class UDPDeprecatedAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class UDPMessageAttribute : Attribute
    {
        public MessageType Number { get; }

        public UDPMessageAttribute(MessageType number)
        {
            Number = number;
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class EventQueueGetAttribute : Attribute
    {
        public string Name { get; }

        public EventQueueGetAttribute(string name)
        {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public sealed class PacketHandlerAttribute : Attribute
    {
        public MessageType Number { get; }
        public PacketHandlerAttribute(MessageType number)
        {
            Number = number;
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public sealed class GenericMessageHandlerAttribute : Attribute
    {
        public string Method { get; }
        public GenericMessageHandlerAttribute(string method)
        {
            Method = method;
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public sealed class GodlikeMessageHandlerAttribute : Attribute
    {
        public string Method { get; }
        public GodlikeMessageHandlerAttribute(string method)
        {
            Method = method;
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public sealed class IMMessageHandlerAttribute : Attribute
    {
        public GridInstantMessageDialog Dialog { get; }
        public IMMessageHandlerAttribute(GridInstantMessageDialog dialog)
        {
            Dialog = dialog;
        }
    }

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

        public bool ForceZeroFlag;

        /* only used rarely for certain things like teleport protocol */
        /* reliable is triggered when the message is acknowledged by viewer with true */
        /* unreliable is immediately acknowledged with true */
        /* reliable not being delivered is acknowledged locally with false */
        public event Action<bool> OnSendCompletion;

        public void OnSendComplete(bool flag)
        {
            OnSendCompletion?.Invoke(flag);
        }

        #region Overloaded methods
        public virtual string TypeDescription => Number.ToString();

        public virtual bool IsReliable => GetType().GetCustomAttributes(typeof(ReliableAttribute), false).Length != 0;

        public virtual bool ZeroFlag => GetType().GetCustomAttributes(typeof(ZerocodedAttribute), false).Length != 0;

        public virtual MessageType Number
        {
            get
            {
                var a = (UDPMessageAttribute)Attribute.GetCustomAttribute(GetType(), typeof(UDPMessageAttribute));
                if(a == null)
                {
                    return 0;
                }
                return a.Number;
            }
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
                var a = (EventQueueGetAttribute)Attribute.GetCustomAttribute(GetType(), typeof(EventQueueGetAttribute));
                if (a == null)
                {
                    throw new NotSupportedException();
                }
                return a.Name;
            }
        }
        #endregion
    }
}
