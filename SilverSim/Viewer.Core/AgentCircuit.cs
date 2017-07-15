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

#pragma warning disable IDE0018
#pragma warning disable RCS1029

using log4net;
using SilverSim.Main.Common.Caps;
using SilverSim.Main.Common.CmdIO;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Grid;
using SilverSim.Types.IM;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Chat;
using SilverSim.Viewer.Messages.Generic;
using SilverSim.Viewer.Messages.IM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit : Circuit
    {
        internal readonly RwLockedList<UUID> SelectedObjects = new RwLockedList<UUID>();
        private static readonly ILog m_Log = LogManager.GetLogger("LL AGENT CIRCUIT");
        internal static readonly UDPPacketDecoder m_PacketDecoder = new UDPPacketDecoder();
        public UUID SessionID = UUID.Zero;
        public UUID AgentID = UUID.Zero;
        public ViewerAgent Agent;
        private SceneInterface m_Scene;
        private readonly RwLockedDictionary<string, UUID> m_RegisteredCapabilities = new RwLockedDictionary<string, UUID>();
        private readonly CapsHttpRedirector m_CapsRedirector;
        private readonly object m_SceneSetLock = new object();
        private ChatServiceInterface m_ChatService;
        private ChatServiceInterface.Listener m_ChatListener;
        private ChatServiceInterface.Listener m_DebugChannelListener;

        private readonly Dictionary<MessageType, Action<Message>> m_MessageRouting = new Dictionary<MessageType, Action<Message>>();
        private readonly Dictionary<string, Action<Message>> m_GenericMessageRouting = new Dictionary<string, Action<Message>>();
        private readonly Dictionary<string, Action<Message>> m_GodlikeMessageRouting = new Dictionary<string, Action<Message>>();
        private readonly Dictionary<GridInstantMessageDialog, Action<Message>> m_IMMessageRouting = new Dictionary<GridInstantMessageDialog, Action<Message>>();

        private Thread m_TextureDownloadThread;
        private bool m_TextureDownloadThreadRunning;
        private readonly BlockingQueue<Message> m_TextureDownloadQueue = new BlockingQueue<Message>();
        internal List<ITriggerOnRootAgentActions> m_TriggerOnRootAgentActions = new List<ITriggerOnRootAgentActions>();

        private Thread m_InventoryThread;
        private bool m_InventoryThreadRunning;
        private readonly BlockingQueue<Message> m_InventoryRequestQueue = new BlockingQueue<Message>();
        public string GatekeeperURI { get; protected set; }

        private Thread m_ObjectUpdateThread;
        private bool m_ObjectUpdateThreadRunning;
        private readonly CommandRegistry m_Commands;

        private int m_AgentUpdatesReceived;

        /* storage for last teleport flags */
        public TeleportFlags LastTeleportFlags { get; set; }

        #region Wait For Root
        internal readonly RwLockedList<KeyValuePair<Action<object, bool>, object>> WaitForRootList = new RwLockedList<KeyValuePair<Action<object, bool>, object>>();
        #endregion

        #region Scene Changing Property
        public SceneInterface Scene
        {
            get { return m_Scene; }

            set
            {
                lock (m_SceneSetLock) /* scene change serialization */
                {
                    var oldScene = m_Scene;
                    if (m_Scene != null)
                    {
                        if (m_ChatListener != null)
                        {
                            m_ChatListener.Remove();
                            m_ChatListener = null;
                        }
                        if (m_DebugChannelListener != null)
                        {
                            m_DebugChannelListener.Remove();
                            m_DebugChannelListener = null;
                        }
                        m_ChatService = null;

                        foreach (var kvp in m_Scene.SceneCapabilities)
                        {
                            if (kvp.Value is ICapabilityInterface)
                            {
                                var iface = (ICapabilityInterface)(kvp.Value);
                                RemoveCapability(iface.CapabilityName);
                            }
                        }
                    }
                    m_Scene = value;
                    oldScene?.TriggerAgentChangedScene(Agent);
                    if (m_Scene != null)
                    {
                        var sceneCapID = UUID.Random;
                        foreach (var kvp in m_Scene.SceneCapabilities)
                        {
                            if (kvp.Value is ICapabilityInterface)
                            {
                                var iface = (ICapabilityInterface)kvp.Value;
                                AddCapability(iface.CapabilityName, sceneCapID, iface.HttpRequestHandler);
                            }
                        }

                        m_ChatService = m_Scene.GetService<ChatServiceInterface>();
                        if (m_ChatService != null)
                        {
                            try
                            {
                                m_ChatListener = m_ChatService.AddAgentListen(PUBLIC_CHANNEL, string.Empty, UUID.Zero, string.Empty, ChatGetAgentUUID, ChatGetAgentPosition, ChatListenerAction);
                            }
                            catch
                            {
                                m_ChatService = null;
                            }
                            try
                            {
                                m_DebugChannelListener = m_ChatService.AddAgentListen(DEBUG_CHANNEL, string.Empty, UUID.Zero, string.Empty, ChatGetAgentUUID, ChatGetAgentPosition, ChatListenerAction);
                            }
                            catch
                            {
                                m_DebugChannelListener = null;
                            }
                        }
                        m_Scene.TriggerAgentChangedScene(Agent);
                    }
                }
            }
        }
        #endregion

        #region Chat Listener
        private Vector3 ChatGetAgentPosition()
        {
            var thisRegionPos = Scene.GridPosition;
            var rootAgentRegionPos = Agent.GetRootAgentGridPosition(thisRegionPos);
            var diff = rootAgentRegionPos - thisRegionPos;
            var agentPos = Agent.GlobalPosition;
            agentPos.X += diff.X;
            agentPos.Y += diff.Y;

            return agentPos;
        }

        private UUID ChatGetAgentUUID() => AgentID;

        private const int PUBLIC_CHANNEL = 0;
        private const int DEBUG_CHANNEL = 0x7FFFFFFF;

        private void ChatListenerAction(ListenEvent le)
        {
            var cfs = new ChatFromSimulator()
            {
                Audible = ChatAudibleLevel.Fully,
                ChatType = (ChatType)(byte)le.Type,
                FromName = le.Name,
                Position = le.GlobalPosition,
                SourceID = le.ID,
                SourceType = (ChatSourceType)(byte)le.SourceType,
                OwnerID = le.OwnerID
            };
            if (le.Localization != null)
            {
                cfs.Message = le.Localization.Localize(le, Agent.CurrentCulture);
            }
            else
            {
                cfs.Message = le.Message;
            }
            if (le.Channel == DEBUG_CHANNEL)
            {
                /* limit debug channel to root agents */
                if(Agent.IsInScene(Scene))
                {
                    SendMessage(cfs);
                }
            }
            else
            {
                SendMessage(cfs);
            }
        }
        #endregion

        #region Message Handler Definitions
        private sealed class MessageHandlerExtenderKeyValuePairCircuitQueue
        {
            private readonly WeakReference m_Circuit;
            private readonly Queue<KeyValuePair<AgentCircuit, Message>> m_Queue;

            public MessageHandlerExtenderKeyValuePairCircuitQueue(AgentCircuit circuit, Queue<KeyValuePair<AgentCircuit, Message>> q)
            {
                m_Circuit = new WeakReference(circuit, false);
                m_Queue = q;
            }

            public void Handler(Message m)
            {
                var circuit = m_Circuit.Target as AgentCircuit;
                if (circuit != null)
                {
                    m_Queue.Enqueue(new KeyValuePair<AgentCircuit, Message>(circuit, m));
                }
            }
        }

        private sealed class MessageHandlerExtenderViewerAgent
        {
            private readonly WeakReference m_Agent;
            private readonly WeakReference m_Circuit;
            private readonly Action<ViewerAgent, AgentCircuit, Message> m_Delegate;

            // for documentation
            //public delegate void HandlerDelegate(ViewerAgent agent, AgentCircuit circuit, Message m);

            public MessageHandlerExtenderViewerAgent(ViewerAgent agent, AgentCircuit circuit, Action<ViewerAgent, AgentCircuit, Message> del)
            {
                m_Agent = new WeakReference(agent, false);
                m_Circuit = new WeakReference(circuit, false);
                m_Delegate = del;
            }

            public void Handler(Message m)
            {
                var agent = m_Agent.Target as ViewerAgent;
                var circuit = m_Circuit.Target as AgentCircuit;
                if (agent != null && circuit != null)
                {
                    m_Delegate(agent, circuit, m);
                }
            }
        }

        private sealed class MessageHandlerExtenderIAgent
        {
            private readonly WeakReference m_Agent;
            private readonly Action<IAgent, Message> m_Delegate;

            //for documentation
            //public delegate void HandlerDelegate(IAgent agent, Message m);

            public MessageHandlerExtenderIAgent(IAgent agent, Action<IAgent, Message> del)
            {
                m_Agent = new WeakReference(agent, false);
                m_Delegate = del;
            }

            public void Handler(Message m)
            {
                var agent = m_Agent.Target as IAgent;
                if (agent != null)
                {
                    m_Delegate(agent, m);
                }
            }
        }

        #endregion

        #region Message Handler Initialization
        private Action<Message> DeriveActionDelegateFromFieldInfo(FieldInfo fi, Type t, object o, string info)
        {
            if (typeof(Queue<Message>).IsAssignableFrom(fi.FieldType))
            {
                var mi = fi.FieldType.GetMethod("Enqueue", new Type[] { typeof(Message) });
                if (mi == null)
                {
                    m_Log.FatalFormat("Field {0} of {1} has no Enqueue method we can use for {2}", fi.Name, t.GetType(), info);
                }
                else
                {
#if EXPLICITDEBUG
                    m_Log.InfoFormat("Field {0} of {1} registered for {2}", fi.Name, t.Name, info);
#endif
                    return (Action<Message>)Delegate.CreateDelegate(typeof(Action<Message>), fi.GetValue(o), mi);
                }
            }
            else if (typeof(Queue<KeyValuePair<AgentCircuit, Message>>).IsAssignableFrom(fi.FieldType))
            {
                var mi = fi.FieldType.GetMethod("Enqueue", new Type[] { typeof(KeyValuePair<AgentCircuit, Message>) });
                if (mi == null)
                {
                    m_Log.FatalFormat("Field {0} of {1} has no Enqueue method we can use for {2}", fi.Name, t.GetType(), info);
                }
                else
                {
#if EXPLICITDEBUG
                    m_Log.InfoFormat("Field {0} of {1} registered for {2}", fi.Name, t.Name, info);
#endif
                    return new MessageHandlerExtenderKeyValuePairCircuitQueue(this, (Queue<KeyValuePair<AgentCircuit, Message>>)fi.GetValue(o)).Handler;
                }
            }
            else
            {
                m_Log.FatalFormat("Field {0} of {1} is not derived from Queue<Message> or Queue<KeyValuePair<Circuit, Message>> for {2}", fi.Name, t.GetType(), info);
            }

            throw new ArgumentException("Handler resolver error");
        }

        private Action<Message> DeriveActionDelegateFromMethodInfo(MethodInfo mi, object o, string info)
        {
            if (mi.ReturnType != typeof(void))
            {
                m_Log.FatalFormat("Method {0} return type is not void for {1}", mi.Name, info);
            }
            else if (mi.GetParameters().Length == 3)
            {
                if (mi.GetParameters()[0].ParameterType != typeof(ViewerAgent) ||
                    mi.GetParameters()[1].ParameterType != typeof(AgentCircuit) ||
                    mi.GetParameters()[2].ParameterType != typeof(Message))
                {
                    m_Log.FatalFormat("Method {0} parameter types do not match for {1}", mi.Name, info);
                }
                else
                {
#if EXPLICITDEBUG
                    m_Log.InfoFormat("Method {0} of {1} registered for {2}", mi.Name, t.Name, info);
#endif
                    return new MessageHandlerExtenderViewerAgent(Agent, this,
                        (Action<ViewerAgent, AgentCircuit, Message>)Delegate.CreateDelegate(typeof(Action<ViewerAgent, AgentCircuit, Message>), o, mi)).Handler;
                }
            }
            else if (mi.GetParameters().Length == 2)
            {
                if (mi.GetParameters()[0].ParameterType != typeof(IAgent) ||
                    mi.GetParameters()[1].ParameterType != typeof(Message))
                {
                    m_Log.FatalFormat("Method {0} parameter types do not match for {1}", mi.Name, info);
                }
                else
                {
#if EXPLICITDEBUG
                    m_Log.InfoFormat("Method {0} of {1} registered for {2}", mi.Name, t.Name, info);
#endif
                    return new MessageHandlerExtenderIAgent(Agent,
                        (Action<IAgent, Message>)Delegate.CreateDelegate(typeof(Action<IAgent, Message>), o, mi)).Handler;
                }
            }
            else if (mi.GetParameters().Length != 1)
            {
                m_Log.FatalFormat("Method {0} parameter count does not match for {1}", mi.Name, info);
            }
            else if (mi.GetParameters()[0].ParameterType != typeof(Message))
            {
                m_Log.FatalFormat("Method {0} parameter types do not match for {1}", mi.Name, info);
            }
            else
            {
#if EXPLICITDEBUG
                m_Log.InfoFormat("Method {0} of {1} registered for {2}", mi.Name, t.Name, info);
#endif
                return (Action<Message>)Delegate.CreateDelegate(typeof(Action<Message>), o, mi);
            }

            throw new ArgumentException("Handler resolver error");
        }

        private void AddMessageRouting(object o)
        {
            var types = new List<Type>();
            var tt = o.GetType();
            while(tt != typeof(object))
            {
                types.Add(tt);
                tt = tt.BaseType;
            }

            var messageRegisteredHere = new List<MessageType>();
            var genericMsgRegisteredHere = new List<string>();
            var godlikeMsgRegisteredHere = new List<string>();
            var imTypeRegisteredHere = new List<GridInstantMessageDialog>();

            foreach (var t in types)
            {
                foreach(var fi in t.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
                {
                    foreach (var pa in (PacketHandlerAttribute[])Attribute.GetCustomAttributes(fi, typeof(PacketHandlerAttribute)))
                    {
                        if (m_MessageRouting.ContainsKey(pa.Number))
                        {
                            if (!messageRegisteredHere.Contains(pa.Number))
                            {
                                m_Log.FatalFormat("Field {0} of {1} registered duplicate message {2}", fi.Name, t.GetType(), pa.Number.ToString());
                            }
                        }
                        else if(pa.Number == MessageType.ImprovedInstantMessage || pa.Number == MessageType.GenericMessage || pa.Number == MessageType.GodlikeMessage)
                        {
                            m_Log.FatalFormat("Field {0} of {1} tries to register unallowed message {2}", fi.Name, t.GetType(), pa.Number.ToString());
                        }
                        else
                        {
                            try
                            {
                                m_MessageRouting.Add(pa.Number, DeriveActionDelegateFromFieldInfo(fi, t, o, "message " + pa.Number.ToString()));
                                messageRegisteredHere.Add(pa.Number);
                            }
                            catch
                            {
                                m_Log.WarnFormat("Tried duplicate registration of message number {0}", pa.Number.ToString());
                            }
                        }
                    }

                    foreach (var gm in (GenericMessageHandlerAttribute[])Attribute.GetCustomAttributes(fi, typeof(GenericMessageHandlerAttribute)))
                    {
                        if (m_GenericMessageRouting.ContainsKey(gm.Method))
                        {
                            if (!genericMsgRegisteredHere.Contains(gm.Method))
                            {
                                m_Log.FatalFormat("Field {0} of {1} registered duplicate generic {2}", fi.Name, t.GetType(), gm.Method);
                            }
                        }
                        else
                        {
                            try
                            {
                                m_GenericMessageRouting.Add(gm.Method, DeriveActionDelegateFromFieldInfo(fi, t, o, "generic " + gm.Method));
                                genericMsgRegisteredHere.Add(gm.Method);
                            }
                            catch
                            {
                                m_Log.WarnFormat("Tried duplicate registration of generic message {0}", gm.Method);
                            }
                        }
                    }

                    foreach (var gm in (GodlikeMessageHandlerAttribute[])Attribute.GetCustomAttributes(fi, typeof(GodlikeMessageHandlerAttribute)))
                    {
                        if (m_GodlikeMessageRouting.ContainsKey(gm.Method))
                        {
                            if (!godlikeMsgRegisteredHere.Contains(gm.Method))
                            {
                                m_Log.FatalFormat("Field {0} of {1} registered duplicate godlike {2}", fi.Name, t.FullName, gm.Method);
                            }
                        }
                        else
                        {
                            try
                            {
                                m_GodlikeMessageRouting.Add(gm.Method, DeriveActionDelegateFromFieldInfo(fi, t, o, "godlike " + gm.Method));
                                godlikeMsgRegisteredHere.Add(gm.Method);
                            }
                            catch
                            {
                                m_Log.WarnFormat("Tried duplicate registration of godlike message {0}", gm.Method);
                            }
                        }
                    }

                    foreach (var im in (IMMessageHandlerAttribute[])Attribute.GetCustomAttributes(fi, typeof(IMMessageHandlerAttribute)))
                    {
                        if (m_IMMessageRouting.ContainsKey(im.Dialog))
                        {
                            if (!imTypeRegisteredHere.Contains(im.Dialog))
                            {
                                m_Log.FatalFormat("Field {0} of {1} registered duplicate im {2}", fi.Name, t.GetType(), im.Dialog.ToString());
                            }
                        }
                        else
                        {
                            try
                            {
                                m_IMMessageRouting.Add(im.Dialog, DeriveActionDelegateFromFieldInfo(fi, t, o, "im " + im.Dialog.ToString()));
                                imTypeRegisteredHere.Add(im.Dialog);
                            }
                            catch
                            {
                                m_Log.WarnFormat("Tried duplicate registration of InstantMessage dialog {0}", im.Dialog.ToString());
                            }
                        }
                    }
                }

                foreach (var mi in t.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
                {
                    if (Attribute.GetCustomAttribute(mi, typeof(IgnoreMethodAttribute)) != null)
                    {
                        continue;
                    }

                    foreach (var pa in (PacketHandlerAttribute[])Attribute.GetCustomAttributes(mi, typeof(PacketHandlerAttribute)))
                    {
                        if (m_MessageRouting.ContainsKey(pa.Number))
                        {
                            if (!messageRegisteredHere.Contains(pa.Number))
                            {
                                m_Log.FatalFormat("Method {0} of {2} registered duplicate message {1}", mi.Name, pa.Number.ToString(), t.FullName);
                            }
                        }
                        else if (pa.Number == MessageType.ImprovedInstantMessage || pa.Number == MessageType.GenericMessage || pa.Number == MessageType.GodlikeMessage)
                        {
                            m_Log.FatalFormat("Method {0} of {2} tries to register unallowed message {1}", mi.Name, pa.Number.ToString(), t.FullName);
                        }
                        else
                        {
                            try
                            {
                                m_MessageRouting.Add(pa.Number, DeriveActionDelegateFromMethodInfo(mi, o, "message " + pa.Number.ToString()));
                                messageRegisteredHere.Add(pa.Number);
                            }
                            catch
                            {
                                m_Log.WarnFormat("Tried duplicate registration of message {0}", pa.Number.ToString());
                            }
                        }
                    }

                    foreach (var gm in (GenericMessageHandlerAttribute[])Attribute.GetCustomAttributes(mi, typeof(GenericMessageHandlerAttribute)))
                    {
                        if (m_GenericMessageRouting.ContainsKey(gm.Method))
                        {
                            if (!genericMsgRegisteredHere.Contains(gm.Method))
                            {
                                m_Log.FatalFormat("Method {0} of {2} registered duplicate generic {1}", mi.Name, gm.Method, t.FullName);
                            }
                        }
                        else
                        {
                            try
                            {
                                m_GenericMessageRouting.Add(gm.Method, DeriveActionDelegateFromMethodInfo(mi, o, "generic " + gm.Method));
                                genericMsgRegisteredHere.Add(gm.Method);
                            }
                            catch
                            {
                                m_Log.WarnFormat("Tried duplicate registration of generic message {0}", gm.Method);
                            }
                        }
                    }

                    foreach (var gm in (GodlikeMessageHandlerAttribute[])Attribute.GetCustomAttributes(mi, typeof(GodlikeMessageHandlerAttribute)))
                    {
                        if (m_GodlikeMessageRouting.ContainsKey(gm.Method))
                        {
                            if (!godlikeMsgRegisteredHere.Contains(gm.Method))
                            {
                                m_Log.FatalFormat("Method {0} of {2} registered duplicate godlike {1}", mi.Name, gm.Method, t.FullName);
                            }
                        }
                        else
                        {
                            try
                            {
                                m_GodlikeMessageRouting.Add(gm.Method, DeriveActionDelegateFromMethodInfo(mi, o, "godlike " + gm.Method));
                                godlikeMsgRegisteredHere.Add(gm.Method);
                            }
                            catch
                            {
                                m_Log.WarnFormat("Tried duplicate registration of godlike message {0}", gm.Method);
                            }
                        }
                    }

                    foreach (var im in (IMMessageHandlerAttribute[])Attribute.GetCustomAttributes(mi, typeof(IMMessageHandlerAttribute)))
                    {
                        if (m_IMMessageRouting.ContainsKey(im.Dialog))
                        {
                            if (!imTypeRegisteredHere.Contains(im.Dialog))
                            {
                                m_Log.FatalFormat("Method {0} of {2} registered duplicate im {1}", mi.Name, im.Dialog.ToString(), t.FullName);
                            }
                        }
                        else
                        {
                            try
                            {
                                m_IMMessageRouting.Add(im.Dialog, DeriveActionDelegateFromMethodInfo(mi, o, "im " + im.Dialog.ToString()));
                                imTypeRegisteredHere.Add(im.Dialog);
                            }
                            catch
                            {
                                m_Log.WarnFormat("Tried duplicate registration of InstantMessage dialog {0}", im.Dialog.ToString());
                            }
                        }
                    }

#if EXPLICITDEBUG
                    if (pas.Length == 0 && gms.Length == 0 && ims.Length == 0)
                    {
                        if (mi.ReturnType != typeof(void))
                        {
                        }
                        else if(mi.GetParameters().Length == 3)
                        {
                            if( mi.GetParameters()[0].ParameterType == typeof(ViewerAgent) &&
                                mi.GetParameters()[1].ParameterType == typeof(AgentCircuit) &&
                                mi.GetParameters()[2].ParameterType == typeof(Message))
                            {
                                m_Log.InfoFormat("Candidate method {0} of {1} is not registered", mi.Name, t.Name);
                            }
                        }
                        else if (mi.GetParameters().Length == 2)
                        {
                            if (mi.GetParameters()[0].ParameterType == typeof(IAgent) &&
                                mi.GetParameters()[1].ParameterType == typeof(Message))
                            {
                                m_Log.InfoFormat("Candidate method {0} of {1} is not registered", mi.Name, t.Name);
                            }
                        }
                        else if (mi.GetParameters().Length != 1)
                        {
                        }
                        else if (mi.GetParameters()[0].ParameterType != typeof(Message))
                        {
                        }
                        else
                        {
                            m_Log.InfoFormat("Candidate method {0} of {1} is not registered", mi.Name, t.Name);
                        }
                    }
#endif
                }
            }
        }

        private void AddCapabilityExtensions(object o, UUID regionSeedID)
        {
            var types = new List<Type>();
            var tt = o.GetType();
            while (tt != typeof(object))
            {
                types.Add(tt);
                tt = tt.BaseType;
            }

            foreach (var t in types)
            {
                foreach (var mi in t.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
                {
                    var ca = (CapabilityHandlerAttribute)Attribute.GetCustomAttribute(mi, typeof(CapabilityHandlerAttribute));
                    if (ca == null)
                    {
                        /* not a capability handler */
                    }
                    else if (m_RegisteredCapabilities.ContainsKey(ca.Name))
                    {
                        m_Log.FatalFormat("Method {0} of {1} tried to add another instantiation for capability {2}", mi.Name, t.Name, ca.Name);
                    }
                    else if (mi.ReturnType != typeof(void))
                    {
                        m_Log.FatalFormat("Method {0} return type is not void", mi.Name);
                    }
                    else if (mi.GetParameters().Length != 3)
                    {
                        m_Log.FatalFormat("Method {0} parameter count does not match", mi.Name);
                    }
                    else if (mi.GetParameters()[0].ParameterType != typeof(ViewerAgent) ||
                        mi.GetParameters()[1].ParameterType != typeof(AgentCircuit) ||
                        mi.GetParameters()[2].ParameterType != typeof(HttpRequest))
                    {
                        m_Log.FatalFormat("Method {0} parameter types do not match", mi.Name);
                    }
                    else
                    {
#if DEBUG
                        m_Log.InfoFormat("Method {0} of {1} used for instantiation of capability {2}", mi.Name, t.Name, ca.Name);
#endif
                        try
                        {
                            AddExtenderCapability(ca.Name, regionSeedID, (Action<ViewerAgent, AgentCircuit, HttpRequest>)Delegate.CreateDelegate(typeof(Action<ViewerAgent, AgentCircuit, HttpRequest>), o, mi), Server.Scene.CapabilitiesConfig);
                        }
                        catch (Exception e)
                        {
                            m_Log.Warn(string.Format("Method {0} of {1} failed to instantiate capability {2}", mi.Name, t.Name, ca.Name), e);
                        }
                    }
                }
            }
        }
        #endregion

        public AgentCircuit(
            CommandRegistry commands,
            ViewerAgent agent,
            UDPCircuitsManager server,
            UInt32 circuitcode,
            CapsHttpRedirector capsredirector,
            UUID regionSeedID,
            Dictionary<string, string> serviceURLs,
            string gatekeeperURI,
            List<IProtocolExtender> extenders,
            EndPoint remoteEndPoint)
            : base(server, circuitcode)
        {
            RemoteEndPoint = remoteEndPoint;
            m_Commands = commands;
            InitSimStats();

            Agent = agent;
            m_CapsRedirector = capsredirector;
            GatekeeperURI = gatekeeperURI;

            Scene = server.Scene;

            m_MessageRouting.Add(MessageType.CopyInventoryItem, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.ChangeInventoryItemFlags, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.CreateInventoryFolder, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.CreateInventoryItem, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.FetchInventory, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.FetchInventoryDescendents, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.MoveInventoryFolder, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.MoveInventoryItem, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.PurgeInventoryDescendents, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.RemoveInventoryFolder, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.RemoveInventoryItem, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.UpdateInventoryFolder, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.UpdateInventoryItem, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.LinkInventoryItem, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.RemoveInventoryObjects, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.ActivateGestures, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.DeactivateGestures, m_InventoryRequestQueue.Enqueue);

            m_MessageRouting.Add(MessageType.RequestImage, m_TextureDownloadQueue.Enqueue);

            AddMessageRouting(this);
            AddMessageRouting(Agent);
            AddMessageRouting(Scene);
            AddCapabilityExtensions(Scene, regionSeedID);

            if(extenders != null)
            {
                foreach (var o in extenders)
                {
                    var extenderType = o.GetType();
                    var interfaces = extenderType.GetInterfaces();
                    if (interfaces.Contains(typeof(IPacketHandlerExtender)))
                    {
                        AddMessageRouting(o);
                    }

                    if(interfaces.Contains(typeof(ICapabilityExtender)))
                    {
                        AddCapabilityExtensions(o, regionSeedID);
                    }

                    if(interfaces.Contains(typeof(ITriggerOnRootAgentActions)))
                    {
                        m_TriggerOnRootAgentActions.Add((ITriggerOnRootAgentActions)o);
                    }
                }
            }

            /* the following two capabilities are mandatory */
            AddCapability("EventQueueGet", regionSeedID, Cap_EventQueueGet);
            SetupDefaultCapabilities(regionSeedID, server.Scene.CapabilitiesConfig, serviceURLs);
            AddCapability("SEED", regionSeedID, RegionSeedHandler);

#if DEBUG
            m_Log.DebugFormat("Registered {0} message handlers", m_MessageRouting.Count);
            m_Log.DebugFormat("Registered {0} IM handlers", m_IMMessageRouting.Count);
            m_Log.DebugFormat("Registered {0} GenericMessage handlers", m_GenericMessageRouting.Count);
            m_Log.DebugFormat("Registered {0} GodlikeMessage handlers", m_GodlikeMessageRouting.Count);
            m_Log.DebugFormat("Registered {0} capabilities", m_RegisteredCapabilities.Count);
#endif
        }

        protected void CloseCircuit()
        {
            foreach (var kvp in m_RegisteredCapabilities)
            {
                m_CapsRedirector.Caps[kvp.Key].Remove(kvp.Value);
            }
            if (Agent != null && Scene != null)
            {
#if DEBUG
                m_Log.DebugFormat("Removing agent {0} from scene {1}", Agent.Owner.FullName, Scene.Name);
#endif
                Scene.Remove(Agent);
                Agent.Circuits.Remove(Scene.ID);
            }
            ChatSessionRequestCapability = null;
            m_UploadCapabilities.Clear();
            Agent = null;
            Scene = null;
        }

        protected override void LogMsgOnLogoutCompletion()
        {
            m_Log.InfoFormat("Logout of agent {0} completed", Agent.ID);
        }

        protected override void LogMsgOnTimeout()
        {
            m_Log.InfoFormat("Packet Timeout for agent {0} {1} ({2}) timed out", Agent.FirstName, Agent.LastName, Agent.ID);
        }

        protected override void LogMsgLogoutReply()
        {
            m_Log.InfoFormat("LogoutReply for agent {0} {1} ({2}) timed out", Agent.FirstName, Agent.LastName, Agent.ID);
        }

        #region Receive Logic
        protected override void CheckForNewDataToSend()
        {
            m_TxObjectQueue.Enqueue(null);
        }

        protected override void OnCircuitSpecificPacketReceived(MessageType mType, UDPPacket p)
        {
            /* we know the message type now, so we have to decode it when possible */
            switch(mType)
            {
                case MessageType.ScriptDialogReply:
                    /* script dialog uses a different internal format, so we decode it specifically */
                    if(!p.ReadUUID().Equals(AgentID) ||
                        !p.ReadUUID().Equals(SessionID))
                    {
                        /* ScriptDialogReply is not for us */
                    }
                    else
                    {
                        /* specific decoder for ListenEvent */
                        var ev = new ListenEvent()
                        {
                            TargetID = p.ReadUUID(),
                            Channel = p.ReadInt32(),
                            ButtonIndex = p.ReadInt32(),
                            Message = p.ReadStringLen8(),
                            ID = AgentID,
                            Type = ListenEvent.ChatType.Say
                        };
                        Server.RouteChat(ev);
                    }
                    break;

                case MessageType.ChatFromViewer:
                    /* chat uses a different internal format, so we decode it specifically */
                    if(!p.ReadUUID().Equals(AgentID) ||
                        !p.ReadUUID().Equals(SessionID))
                    {
                        /* ChatFromViewer is not for us */
                    }
                    else
                    {
                        var ev = new ListenEvent()
                        {
                            ID = AgentID,
                            Message = p.ReadStringLen16(),
                            Type = (ListenEvent.ChatType)p.ReadUInt8(),
                            Channel = p.ReadInt32(),
                            GlobalPosition = Agent.GlobalPosition,
                            Name = Agent.Name,
                            TargetID = UUID.Zero,
                            SourceType = ListenEvent.ChatSourceType.Agent,
                            OwnerID = AgentID
                        };
                        Server.RouteChat(ev);
                    }
                    break;

                case MessageType.TransferRequest:
                    {
                        /* we need differentiation here of SimInventoryItem */
                        Action<Message> mdel;
                        var m = Messages.Transfer.TransferRequest.Decode(p);
                        if(m.SourceType == Messages.Transfer.SourceType.SimInventoryItem)
                        {
                            if(m.Params.Length >= 96)
                            {
                                m_InventoryRequestQueue.Enqueue(m);
                            }
                        }
                        else if(m.SourceType == Messages.Transfer.SourceType.Asset)
                        {
                            if(m.Params.Length >= 20)
                            {
                                m_InventoryRequestQueue.Enqueue(m);
                            }
                        }
                        else if (m_MessageRouting.TryGetValue(MessageType.TransferRequest, out mdel))
                        {
                            mdel(m);
                        }
                    }
                    break;

                default:
                    Func<UDPPacket, Message> del;
                    if(mType == MessageType.AgentUpdate)
                    {
                        Interlocked.Increment(ref m_AgentUpdatesReceived);
                    }
                    if(m_PacketDecoder.PacketTypes.TryGetValue(mType, out del))
                    {
                        var m = del(p);
                        /* we got a decoder, so we can make use of it */
                        m.CircuitAgentID = new UUID(AgentID);
                        try
                        {
                            m.CircuitAgentOwner = Agent.Owner;
                            m.CircuitSessionID = new UUID(SessionID);
                            m.CircuitSceneID = new UUID(Scene.ID);
                        }
                        catch
                        {
                            /* this is a specific error that happens only during logout */
                            return;
                        }

                        /* we keep the circuit relatively dumb so that we have no other logic than how to send and receive messages to the viewer.
                            * It merely collects delegates to other objects as well to call specific functions.
                            */
                        Action<Message> mdel;
                        if (m_MessageRouting.TryGetValue(m.Number, out mdel))
                        {
                            mdel(m);
                        }
                        else if(m.Number == MessageType.ImprovedInstantMessage)
                        {
                            var im = (ImprovedInstantMessage)m;
                            if(im.CircuitAgentID != im.AgentID ||
                                im.CircuitSessionID != im.SessionID)
                            {
                                break;
                            }
                            if(m_IMMessageRouting.TryGetValue(im.Dialog, out mdel))
                            {
                                mdel(m);
                            }
                            else
                            {
                                m_Log.DebugFormat("Unhandled im message {0} received", im.Dialog.ToString());
                            }
                        }
                        else if (m.Number == MessageType.GenericMessage)
                        {
                            var genMsg = (GenericMessage)m;
                            if (m_GenericMessageRouting.TryGetValue(genMsg.Method, out mdel))
                            {
                                mdel(m);
                            }
                            else
                            {
                                m_Log.DebugFormat("Unhandled generic message {0} received", genMsg.Method);
                            }
                        }
                        else if (m.Number == MessageType.GodlikeMessage)
                        {
                            var genMsg = (GodlikeMessage)m;
                            if (m_GodlikeMessageRouting.TryGetValue(genMsg.Method, out mdel))
                            {
                                mdel(m);
                            }
                            else
                            {
                                m_Log.DebugFormat("Unhandled godlike message {0} received", genMsg.Method);
                            }
                        }
                        else
                        {
                            m_Log.DebugFormat("Unhandled message type {0} received", m.Number.ToString());
                        }
                    }
                    else
                    {
                        /* Ignore we have no decoder for that */
                    }
                    break;
            }
        }

        internal void OnIMResult(GridInstantMessage im, bool success)
        {
            if (!success)
            {
                switch(im.Dialog)
                {
                    case GridInstantMessageDialog.MessageFromAgent:
                        var m = new ImprovedInstantMessage()
                        {
                            AgentID = im.FromAgent.ID,
                            SessionID = UUID.Zero,
                            FromAgentName = "System",
                            FromGroup = false,
                            ToAgentID = AgentID,
                            ParentEstateID = 0,
                            RegionID = UUID.Zero,
                            Position = Vector3.Zero,
                            IsOffline = false,
                            Timestamp = new Date(),
                            Dialog = GridInstantMessageDialog.BusyAutoResponse,
                            ID = im.IMSessionID,
                            Message = "User not logged in. Message not saved."
                        };
                        SendMessage(m);
                        break;

                    default:
                        break;
                }
            }
        }
        #endregion

        #region Log Incoming Agent
        public void LogIncomingAgent(ILog log, bool isChild)
        {
            log.InfoFormat("Incoming agent {0} {1} (Grid {2}, UUID {3}) TeleportFlags ({4}) Client IP {5} Type {6} Region {7} ({8})",
                Agent.Owner.FirstName,
                Agent.Owner.LastName,
                Agent.Owner.HomeURI,
                Agent.Owner.ID.ToString(),
                LastTeleportFlags,
                ((IPEndPoint)RemoteEndPoint).Address.ToString(),
                isChild ? "Child" : "Root",
                Scene.Name,
                Scene.ID.ToString());
        }
        #endregion

        #region Thread control logic
        private bool m_EventQueueEnabled = true;

        protected override void StartSpecificThreads()
        {
            if(!m_TextureDownloadThreadRunning)
            {
                m_TextureDownloadThread = ThreadManager.CreateThread(TextureDownloadThread);
                m_TextureDownloadThreadRunning = true;
                m_TextureDownloadThread.Start(this);
            }
            if(!m_InventoryThreadRunning)
            {
                m_InventoryThread = ThreadManager.CreateThread(FetchInventoryThread);
                m_InventoryThreadRunning = true;
                m_InventoryThread.Start(this);
            }
            if (!m_ObjectUpdateThreadRunning)
            {
                m_ObjectUpdateThread = ThreadManager.CreateThread(HandleObjectUpdates);
                m_ObjectUpdateThreadRunning = true;
                m_ObjectUpdateThread.Start();
            }
            m_EventQueueEnabled = true;
        }

        protected override void StopSpecificThreads()
        {
            if(m_TextureDownloadThread != null)
            {
                m_TextureDownloadThreadRunning = false;
                m_TextureDownloadThread = null;
            }
            if(m_InventoryThread != null)
            {
                m_InventoryThreadRunning = false;
                m_InventoryThread = null;
            }
            if (m_ObjectUpdateThread != null)
            {
                m_ObjectUpdateThreadRunning = false;
                m_ObjectUpdateThread = null;
                m_TxObjectQueue.Enqueue(null);
            }
            m_EventQueueEnabled = false;
            CloseCircuit();
        }
        #endregion
    }
}
