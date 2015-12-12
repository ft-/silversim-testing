// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Economy;
using SilverSim.Viewer.Messages.IM;
using SilverSim.Main.Common.Caps;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using SilverSim.Types.Economy;
using SilverSim.Types.IM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using ThreadedClasses;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit : Circuit
    {
        internal readonly RwLockedList<UUID> SelectedObjects = new RwLockedList<UUID>();
        private static readonly ILog m_Log = LogManager.GetLogger("LL AGENT CIRCUIT");
        private static Encoding UTF8NoBOM = new System.Text.UTF8Encoding(false);
        private static readonly UDPPacketDecoder m_PacketDecoder = new UDPPacketDecoder();
        public UUID SessionID = UUID.Zero;
        public UUID AgentID = UUID.Zero;
        public ViewerAgent Agent;
        private SceneInterface m_Scene;
        readonly RwLockedDictionary<string, UUID> m_RegisteredCapabilities = new RwLockedDictionary<string, UUID>();
        readonly CapsHttpRedirector m_CapsRedirector;
        readonly object m_SceneSetLock = new object();
        private ChatServiceInterface m_ChatService;
        private ChatServiceInterface.Listener m_ChatListener;
        private ChatServiceInterface.Listener m_DebugChannelListener;

        private Thread m_TextureDownloadThread;
        private bool m_TextureDownloadThreadRunning;
        readonly BlockingQueue<Message> m_TextureDownloadQueue = new BlockingQueue<Message>();
        internal List<ITriggerOnRootAgentActions> m_TriggerOnRootAgentActions = new List<ITriggerOnRootAgentActions>();

        private Thread m_InventoryThread;
        private bool m_InventoryThreadRunning;
        readonly BlockingQueue<Message> m_InventoryRequestQueue = new BlockingQueue<Message>();
        public string GatekeeperURI { get; protected set; }

        private Thread m_ObjectUpdateThread;
        private bool m_ObjectUpdateThreadRunning;

        int m_AgentUpdatesReceived;

        public SceneInterface Scene
        {
            get
            {
                return m_Scene;
            }

            set
            {
                lock (m_SceneSetLock) /* scene change serialization */
                {
                    if (null != m_Scene)
                    {
                        if(m_ChatListener != null)
                        {
                            m_ChatListener.Remove();
                            m_ChatListener = null;
                        }
                        if(m_DebugChannelListener != null)
                        {
                            m_DebugChannelListener.Remove();
                            m_DebugChannelListener = null;
                        }
                        m_ChatService = null;
                        
                        foreach (KeyValuePair<string, object> kvp in m_Scene.SceneCapabilities)
                        {
                            if (kvp.Value is ICapabilityInterface)
                            {
                                ICapabilityInterface iface = (ICapabilityInterface)(kvp.Value);
                                RemoveCapability(iface.CapabilityName);
                            }
                        }
                    }
                    m_Scene = value;
                    if (null != m_Scene)
                    {
                        UUID sceneCapID = UUID.Random;
                        foreach (KeyValuePair<string, object> kvp in m_Scene.SceneCapabilities)
                        {
                            if (kvp.Value is ICapabilityInterface)
                            {
                                ICapabilityInterface iface = (ICapabilityInterface)(kvp.Value);
                                AddCapability(iface.CapabilityName, sceneCapID, iface.HttpRequestHandler);
                            }
                        }

                        m_ChatService = m_Scene.GetService<ChatServiceInterface>();
                        if(null != m_ChatService)
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
                    }
                }
            }
        }

        #region Chat Listener
        private Vector3 ChatGetAgentPosition()
        {
            GridVector thisRegionPos = Scene.GridPosition;
            GridVector rootAgentRegionPos = Agent.GetRootAgentGridPosition(thisRegionPos);
            Vector3 diff = rootAgentRegionPos - thisRegionPos;
            Vector3 agentPos = Agent.GlobalPosition;
            agentPos.X += diff.X;
            agentPos.Y += diff.Y;

            return agentPos;
        }

        private UUID ChatGetAgentUUID()
        {
            return AgentID;
        }

        const int PUBLIC_CHANNEL = 0;
        const int DEBUG_CHANNEL = 0x7FFFFFFF;

        private void ChatListenerAction(ListenEvent le)
        {
            Messages.Chat.ChatFromSimulator cfs = new Messages.Chat.ChatFromSimulator();
            cfs.Audible = Messages.Chat.ChatAudibleLevel.Fully;
            cfs.ChatType = (Messages.Chat.ChatType)(byte)le.Type;
            cfs.FromName = le.Name;
            cfs.Message = le.Message;
            cfs.Position = le.GlobalPosition;
            cfs.SourceID = le.ID;
            cfs.SourceType = (Messages.Chat.ChatSourceType)(byte)le.SourceType;
            cfs.OwnerID = le.OwnerID;
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

        [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
        sealed class MessageHandlerExtenderKeyValuePairCircuitQueue
        {
            readonly WeakReference m_Circuit;
            readonly Queue<KeyValuePair<AgentCircuit, Message>> m_Queue;

            public MessageHandlerExtenderKeyValuePairCircuitQueue(AgentCircuit circuit, Queue<KeyValuePair<AgentCircuit, Message>> q)
            {
                m_Circuit = new WeakReference(circuit, false);
                m_Queue = q;
            }

            public void Handler(Message m)
            {
                AgentCircuit circuit = m_Circuit.Target as AgentCircuit;
                if (circuit != null)
                {
                    m_Queue.Enqueue(new KeyValuePair<AgentCircuit, Message>(circuit, m));
                }
            }
        }

        sealed class MessageHandlerExtenderViewerAgent
        {
            readonly WeakReference m_Agent;
            readonly WeakReference m_Circuit;
            readonly Action<ViewerAgent, AgentCircuit, Message> m_Delegate;

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
                ViewerAgent agent = m_Agent.Target as ViewerAgent;
                AgentCircuit circuit = m_Circuit.Target as AgentCircuit;
                if (agent != null && circuit != null)
                {
                    m_Delegate(agent, circuit, m);
                }
            }
        }

        sealed class MessageHandlerExtenderIAgent
        {
            readonly WeakReference m_Agent;
            readonly Action<IAgent, Message> m_Delegate;

            //for documentation
            //public delegate void HandlerDelegate(IAgent agent, Message m);

            public MessageHandlerExtenderIAgent(IAgent agent, Action<IAgent, Message> del)
            {
                m_Agent = new WeakReference(agent, false);
                m_Delegate = del;
            }

            public void Handler(Message m)
            {
                IAgent agent = m_Agent.Target as IAgent;
                if (agent != null)
                {
                    m_Delegate(agent, m);
                }
            }
        }

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        Action<Message> DeriveActionDelegateFromFieldInfo(FieldInfo fi, Type t, object o, string info)
        {
            if (typeof(Queue<Message>).IsAssignableFrom(fi.FieldType))
            {
                MethodInfo mi = fi.FieldType.GetMethod("Enqueue", new Type[] { typeof(Message) });
                if (null == mi)
                {
                    m_Log.FatalFormat("Field {0} of {1} has no Enqueue method we can use", fi.Name, t.GetType());
                }
                else
                {
#if DEBUG
                    m_Log.InfoFormat("Field {0} of {1} registered for {2}", fi.Name, t.Name, info);
#endif
                    return (Action<Message>)Delegate.CreateDelegate(typeof(Action<Message>), fi.GetValue(o), mi);

                }
            }
            else if (typeof(Queue<KeyValuePair<AgentCircuit, Message>>).IsAssignableFrom(fi.FieldType))
            {
                MethodInfo mi = fi.FieldType.GetMethod("Enqueue", new Type[] { typeof(KeyValuePair<AgentCircuit, Message>) });
                if (null == mi)
                {
                    m_Log.FatalFormat("Field {0} of {1} has no Enqueue method we can use", fi.Name, t.GetType());
                }
                else
                {
#if DEBUG
                    m_Log.InfoFormat("Field {0} of {1} registered for {2}", fi.Name, t.Name, info);
#endif
                    return new MessageHandlerExtenderKeyValuePairCircuitQueue(this, (Queue<KeyValuePair<AgentCircuit, Message>>)fi.GetValue(o)).Handler;

                }
            }
            else
            {
                m_Log.FatalFormat("Field {0} of {1} is not derived from Queue<Message> or Queue<KeyValuePair<Circuit, Message>>", fi.Name, t.GetType());
            }

            throw new ArgumentException("Handler resolver error");
        }

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        Action<Message> DeriveActionDelegateFromMethodInfo(MethodInfo mi, Type t, object o, string info)
        {
            if (mi.ReturnType != typeof(void))
            {
                m_Log.FatalFormat("Method {0} return type is not void", mi.Name);
            }
            else if (mi.GetParameters().Length == 3)
            {
                if (mi.GetParameters()[0].ParameterType != typeof(ViewerAgent) ||
                    mi.GetParameters()[1].ParameterType != typeof(AgentCircuit) ||
                    mi.GetParameters()[2].ParameterType != typeof(Message))
                {
                    m_Log.FatalFormat("Method {0} parameter types do not match", mi.Name);
                }
                else
                {
#if DEBUG
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
                    m_Log.FatalFormat("Method {0} parameter types do not match", mi.Name);
                }
                else
                {
#if DEBUG
                    m_Log.InfoFormat("Method {0} of {1} registered for {2}", mi.Name, t.Name, info);
#endif
                    return new MessageHandlerExtenderIAgent(Agent,
                        (Action<IAgent, Message>)Delegate.CreateDelegate(typeof(Action<IAgent, Message>), o, mi)).Handler;
                }
            }
            else if (mi.GetParameters().Length != 1)
            {
                m_Log.FatalFormat("Method {0} parameter count does not match", mi.Name);
            }
            else if (mi.GetParameters()[0].ParameterType != typeof(Message))
            {
                m_Log.FatalFormat("Method {0} parameter types do not match", mi.Name);
            }
            else
            {
#if DEBUG
                m_Log.InfoFormat("Method {0} of {1} registered for {2}", mi.Name, t.Name, info);
#endif
                return (Action<Message>)Delegate.CreateDelegate(typeof(Action<Message>), o, mi);
            }

            throw new ArgumentException("Handler resolver error");
        }

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        void AddMessageRouting(object o)
        {
            List<Type> types = new List<Type>();
            Type tt;
            tt = o.GetType();
            while(tt != typeof(object))
            {
                types.Add(tt);
                tt = tt.BaseType;
            }

            foreach(Type t in types)
            {
                foreach(FieldInfo fi in t.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
                {
                    PacketHandlerAttribute[] pas = (PacketHandlerAttribute[])Attribute.GetCustomAttributes(fi, typeof(PacketHandlerAttribute));
                    foreach (PacketHandlerAttribute pa in pas)
                    {
                        if (m_MessageRouting.ContainsKey(pa.Number))
                        {
                            m_Log.FatalFormat("Field {0} of {1} registered duplicate message {1}", fi.Name, t.GetType(), pa.Number.ToString());
                        }
                        else if(pa.Number == MessageType.ImprovedInstantMessage || pa.Number == MessageType.GenericMessage)
                        {
                            m_Log.FatalFormat("Field {0} of {1} tries to register unallowed message {1}", fi.Name, t.GetType(), pa.Number.ToString());
                        }
                        else
                        {
                            try
                            {
                                m_MessageRouting.Add(pa.Number, DeriveActionDelegateFromFieldInfo(fi, t, o, "message " + pa.Number.ToString()));
                            }
                            catch
                            {

                            }
                        }
                    }

                    GenericMessageHandlerAttribute[] gms = (GenericMessageHandlerAttribute[])Attribute.GetCustomAttributes(fi, typeof(GenericMessageHandlerAttribute));
                    foreach (GenericMessageHandlerAttribute gm in gms)
                    {
                        if (m_GenericMessageRouting.ContainsKey(gm.Method))
                        {
                            m_Log.FatalFormat("Field {0} of {1} registered duplicate generic {1}", fi.Name, t.GetType(), gm.Method);
                        }
                        else
                        {
                            try
                            {
                                m_GenericMessageRouting.Add(gm.Method, DeriveActionDelegateFromFieldInfo(fi, t, o, "generic " + gm.Method));
                            }
                            catch
                            {

                            }
                        }
                    }

                    IMMessageHandlerAttribute[] ims = (IMMessageHandlerAttribute[])Attribute.GetCustomAttributes(fi, typeof(IMMessageHandlerAttribute));
                    foreach (IMMessageHandlerAttribute im in ims)
                    {
                        if (m_IMMessageRouting.ContainsKey(im.Dialog))
                        {
                            m_Log.FatalFormat("Field {0} of {1} registered duplicate im {1}", fi.Name, t.GetType(), im.Dialog.ToString());
                        }
                        else
                        {
                            try
                            {
                                m_IMMessageRouting.Add(im.Dialog, DeriveActionDelegateFromFieldInfo(fi, t, o, "im " + im.Dialog.ToString()));
                            }
                            catch
                            {

                            }
                        }
                    }
                }

                foreach (MethodInfo mi in t.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
                {
                    if (null != Attribute.GetCustomAttribute(mi, typeof(IgnoreMethodAttribute)))
                    {
                        continue;
                    }
                    PacketHandlerAttribute[] pas = (PacketHandlerAttribute[])Attribute.GetCustomAttributes(mi, typeof(PacketHandlerAttribute));
                    GenericMessageHandlerAttribute[] gms = (GenericMessageHandlerAttribute[])Attribute.GetCustomAttributes(mi, typeof(GenericMessageHandlerAttribute));
                    IMMessageHandlerAttribute[] ims = (IMMessageHandlerAttribute[])Attribute.GetCustomAttributes(mi, typeof(IMMessageHandlerAttribute));

                    foreach (PacketHandlerAttribute pa in pas)
                    {
                        if (m_MessageRouting.ContainsKey(pa.Number))
                        {
                            m_Log.FatalFormat("Method {0} registered duplicate message {1}", mi.Name, pa.Number.ToString());
                        }
                        else if (pa.Number == MessageType.ImprovedInstantMessage || pa.Number == MessageType.GenericMessage)
                        {
                            m_Log.FatalFormat("Method {0} tries to register unallowed message {1}", mi.Name, pa.Number.ToString());
                        }
                        else
                        {
                            try
                            {
                                m_MessageRouting.Add(pa.Number, DeriveActionDelegateFromMethodInfo(mi, t, o, "message " + pa.Number.ToString()));
                            }
                            catch
                            {

                            }
                        }
                    }

                    foreach (GenericMessageHandlerAttribute gm in gms)
                    {
                        if (m_GenericMessageRouting.ContainsKey(gm.Method))
                        {
                            m_Log.FatalFormat("Method {0} registered duplicate generic {1}", mi.Name, gm.Method);
                        }
                        else
                        {
                            try
                            {
                                m_GenericMessageRouting.Add(gm.Method, DeriveActionDelegateFromMethodInfo(mi, t, o, "message " + gm.Method));
                            }
                            catch
                            {

                            }
                        }
                    }

                    foreach (IMMessageHandlerAttribute im in ims)
                    {
                        if (m_IMMessageRouting.ContainsKey(im.Dialog))
                        {
                            m_Log.FatalFormat("Method {0} registered duplicate im {1}", mi.Name, im.Dialog.ToString());
                        }
                        else
                        {
                            try
                            {
                                m_IMMessageRouting.Add(im.Dialog, DeriveActionDelegateFromMethodInfo(mi, t, o, "im " + im.Dialog.ToString()));
                            }
                            catch
                            {

                            }
                        }
                    }

#if DEBUG
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

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        void AddCapabilityExtensions(object o, UUID regionSeedID)
        {
            List<Type> types = new List<Type>();
            Type tt;
            tt = o.GetType();
            while (tt != typeof(object))
            {
                types.Add(tt);
                tt = tt.BaseType;
            }

            foreach (Type t in types)
            {
                foreach (MethodInfo mi in t.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
                {
                    CapabilityHandlerAttribute ca = (CapabilityHandlerAttribute)Attribute.GetCustomAttribute(mi, typeof(CapabilityHandlerAttribute));
                    if (null == ca)
                    {

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
                    else if (mi.GetParameters()[0].ParameterType != typeof(ViewerAgent))
                    {
                        m_Log.FatalFormat("Method {0} parameter types do not match", mi.Name);
                    }
                    else if (mi.GetParameters()[1].ParameterType != typeof(AgentCircuit))
                    {
                        m_Log.FatalFormat("Method {0} parameter types do not match", mi.Name);
                    }
                    else if (mi.GetParameters()[2].ParameterType != typeof(HttpRequest))
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

        public AgentCircuit(
            ViewerAgent agent, 
            UDPCircuitsManager server,
            UInt32 circuitcode,
            CapsHttpRedirector capsredirector, 
            UUID regionSeedID, 
            Dictionary<string, string> serviceURLs,
            string gatekeeperURI,
            List<IProtocolExtender> extenders)
            : base(server, circuitcode)
        {
            InitSimStats();

            Agent = agent;
            m_CapsRedirector = capsredirector;
            GatekeeperURI = gatekeeperURI;
            
            /* the following two capabilities are mandatory */
            AddCapability("SEED", regionSeedID, RegionSeedHandler);
            AddCapability("EventQueueGet", regionSeedID, Cap_EventQueueGet);

            SetupDefaultCapabilities(regionSeedID, server.Scene.CapabilitiesConfig, serviceURLs);
            Scene = server.Scene;

            m_MessageRouting.Add(MessageType.CopyInventoryItem, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.ChangeInventoryItemFlags, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.CreateInventoryFolder, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.CreateInventoryItem, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.FetchInventory, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.FetchInventoryDescendents, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.MoveInventoryFolder, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.PurgeInventoryDescendents, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.RemoveInventoryFolder, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.RemoveInventoryItem, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.UpdateInventoryFolder, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.UpdateInventoryItem, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.LinkInventoryItem, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.RemoveInventoryObjects, m_InventoryRequestQueue.Enqueue);

            m_MessageRouting.Add(MessageType.RequestImage, m_TextureDownloadQueue.Enqueue);

            AddMessageRouting(this);
            AddMessageRouting(Agent);
            AddMessageRouting(Scene);
            AddCapabilityExtensions(Scene, regionSeedID);

            if(extenders != null)
            {
                foreach (IProtocolExtender o in extenders)
                {
                    Type extenderType = o.GetType();
                    Type[] interfaces = extenderType.GetInterfaces();
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
        }

        ~AgentCircuit()
        {
            foreach (KeyValuePair<string, UUID> kvp in m_RegisteredCapabilities)
            {
                m_CapsRedirector.Caps[kvp.Key].Remove(kvp.Value);
            }
            if (null != Agent && null != Scene)
            {
                Agent.Circuits.Remove(CircuitCode, Scene.ID);
            }
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

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        protected override void OnCircuitSpecificPacketReceived(MessageType mType, UDPPacket p)
        {
            /* we know the message type now, so we have to decode it when possible */
            switch(mType)
            { 
                case MessageType.ScriptDialogReply:
                    /* script dialog uses a different internal format, so we decode it specifically */
                    if(!p.ReadUUID().Equals(AgentID))
                    {

                    }
                    else if (!p.ReadUUID().Equals(SessionID))
                    {

                    }
                    else
                    {
                        /* specific decoder for ListenEvent */
                        ListenEvent ev = new ListenEvent();
                        ev.TargetID = p.ReadUUID();
                        ev.Channel = p.ReadInt32();
                        ev.ButtonIndex = p.ReadInt32();
                        ev.Message = p.ReadStringLen8();
                        ev.ID = AgentID;
                        ev.Type = ListenEvent.ChatType.Say;
                        Server.RouteChat(ev);
                    }
                    break;

                case MessageType.ChatFromViewer:
                    /* chat uses a different internal format, so we decode it specifically */
                    if(!p.ReadUUID().Equals(AgentID))
                    {

                    }
                    else if (!p.ReadUUID().Equals(SessionID))
                    {

                    }
                    else
                    {
                        ListenEvent ev = new ListenEvent();
                        ev.ID = AgentID;
                        ev.Message = p.ReadStringLen16();
                        byte type = p.ReadUInt8();

                        ev.Type = (ListenEvent.ChatType)type;
                        ev.Channel = p.ReadInt32();
                        ev.GlobalPosition = Agent.GlobalPosition;
                        ev.Name = Agent.Name;
                        ev.TargetID = UUID.Zero;
                        ev.SourceType = ListenEvent.ChatSourceType.Agent;
                        ev.OwnerID = AgentID;
                        Server.RouteChat(ev);
                    }
                    break;

                case MessageType.TransferRequest:
                    {
                        /* we need differentiation here of SimInventoryItem */
                        Action<Message> mdel;
                        Messages.Transfer.TransferRequest m = Messages.Transfer.TransferRequest.Decode(p);
                        if(m.SourceType == Messages.Transfer.SourceType.SimInventoryItem)
                        {
                            if(m.Params.Length >= 96)
                            {
                                UUID taskID = new UUID(m.Params, 48);
                                if(taskID.Equals(UUID.Zero))
                                {
                                    m_InventoryRequestQueue.Enqueue(m);
                                }
                                else if (m_MessageRouting.TryGetValue(MessageType.TransferRequest, out mdel))
                                {
                                    mdel(m);
                                }
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
                        Message m = del(p);
                        /* we got a decoder, so we can make use of it */
                        m.ReceivedOnCircuitCode = CircuitCode;
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
                            ImprovedInstantMessage im = (ImprovedInstantMessage)m;
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
                            SilverSim.Viewer.Messages.Generic.GenericMessage genMsg = (SilverSim.Viewer.Messages.Generic.GenericMessage)m;
                            if (m_GenericMessageRouting.TryGetValue(genMsg.Method, out mdel))
                            {
                                mdel(m);
                            }
                            else
                            {
                                m_Log.DebugFormat("Unhandled generic message {0} received", m.Number.ToString());
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
                        ImprovedInstantMessage m = new ImprovedInstantMessage();
                        m.AgentID = im.FromAgent.ID;
                        m.SessionID = UUID.Zero;
                        m.FromAgentName = "System";
                        m.FromGroup = false;
                        m.ToAgentID = AgentID;
                        m.ParentEstateID = 0;
                        m.RegionID = UUID.Zero;
                        m.Position = Vector3.Zero;
                        m.IsOffline = false;
                        m.Timestamp = new Date();
                        m.Dialog = GridInstantMessageDialog.BusyAutoResponse;
                        m.ID = im.IMSessionID;
                        m.Message = "User not logged in. Message not saved.";
                        SendMessage(m);
                        break;

                    default:
                        break;
                }
            }
        }
        #endregion

        #region Thread control logic
        protected override void StartSpecificThreads()
        {
            if(!m_TextureDownloadThreadRunning)
            {
                m_TextureDownloadThread = new Thread(TextureDownloadThread);
                m_TextureDownloadThreadRunning = true;
                m_TextureDownloadThread.Start(this);
            }
            if(!m_InventoryThreadRunning)
            {
                m_InventoryThread = new Thread(FetchInventoryThread);
                m_InventoryThreadRunning = true;
                m_InventoryThread.Start(this);
            }
            if (!m_ObjectUpdateThreadRunning)
            {
                m_ObjectUpdateThread = new Thread(HandleObjectUpdates);
                m_ObjectUpdateThreadRunning = true;
                m_ObjectUpdateThread.Start();
            }
            m_EventQueueEnabled = true;
        }

        protected override void StopSpecificThreads()
        {
            if(null != m_TextureDownloadThread)
            {
                m_TextureDownloadThreadRunning = false;
                m_TextureDownloadThread = null;
            }
            if(null != m_InventoryThread)
            {
                m_InventoryThreadRunning = false;
                m_InventoryThread = null;
            }
            if (null != m_ObjectUpdateThread)
            {
                m_ObjectUpdateThreadRunning = false;
                m_ObjectUpdateThread = null;
                m_TxObjectQueue.Enqueue(null);
            }
            m_EventQueueEnabled = false;
        }
        #endregion
    }
}
