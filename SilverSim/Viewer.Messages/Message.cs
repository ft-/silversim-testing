// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using SilverSim.Types;
using SilverSim.Types.IM;
using System.Text;

namespace SilverSim.Viewer.Messages
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ReliableAttribute : Attribute
    {
        public ReliableAttribute()
        {

        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ZerocodedAttribute : Attribute
    {
        public ZerocodedAttribute()
        {

        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class TrustedAttribute : Attribute
    {
        public TrustedAttribute()
        {

        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class NotTrustedAttribute : Attribute
    {
        public NotTrustedAttribute()
        {

        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class UDPDeprecatedAttribute : Attribute
    {
        public UDPDeprecatedAttribute()
        {

        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class UDPMessageAttribute : Attribute
    {
        public MessageType Number { get; private set; }

        public UDPMessageAttribute(MessageType number)
        {
            Number = number;
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class EventQueueGetAttribute : Attribute
    {
        public string Name { get; private set; }
        
        public EventQueueGetAttribute(string name)
        {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public sealed class PacketHandlerAttribute : Attribute
    {
        public MessageType Number { get; private set; }
        public PacketHandlerAttribute(MessageType number)
        {
            Number = number;
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public sealed class GenericMessageHandlerAttribute : Attribute
    {
        public string Method { get; private set; }
        public GenericMessageHandlerAttribute(string method)
        {
            Method = method;
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public sealed class IMMessageHandlerAttribute : Attribute
    {
        public GridInstantMessageDialog Dialog { get; private set; }
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

        public UInt32 ReceivedOnCircuitCode;
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
            var ev = OnSendCompletion;
            if (null != ev)
            {
                foreach (Action<bool> del in ev.GetInvocationList())
                {
                    del(flag);
                }
            }
        }

        protected static readonly UTF8Encoding UTF8NoBOM = new UTF8Encoding(false);

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
                return GetType().GetCustomAttributes(typeof(ReliableAttribute), false).Length != 0;
            }
        }

        public virtual bool ZeroFlag
        {
            get
            {
                return GetType().GetCustomAttributes(typeof(ZerocodedAttribute), false).Length != 0;
            }
        }

        public virtual MessageType Number
        {
            get
            {
                UDPMessageAttribute a = (UDPMessageAttribute)Attribute.GetCustomAttribute(GetType(), typeof(UDPMessageAttribute));
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
                EventQueueGetAttribute a = (EventQueueGetAttribute)Attribute.GetCustomAttribute(GetType(), typeof(EventQueueGetAttribute));
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
