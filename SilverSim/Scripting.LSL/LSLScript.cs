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

using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using SilverSim.Types.Script;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Timers;
using System.Xml;
using ThreadedClasses;

namespace SilverSim.Scripting.LSL
{
    public partial class Script : ScriptInstance, IScriptState
    {
        private ObjectPart m_Part;
        private ObjectPartInventoryItem m_Item;
        private NonblockingQueue<IScriptEvent> m_Events = new NonblockingQueue<IScriptEvent>();
        internal List<DetectInfo> m_Detected = new List<DetectInfo>();
        private Dictionary<string, LSLState> m_States = new Dictionary<string, LSLState>();
        private LSLState m_CurrentState = null;
        public int StartParameter = 0;
        internal RwLockedDictionary<int, ChatServiceInterface.Listener> m_Listeners = new RwLockedDictionary<int, ChatServiceInterface.Listener>();
        private double m_ExecutionTime = 0;
        protected bool UseMessageObjectEvent = false;
        internal RwLockedList<UUID> m_RequestedURLs = new RwLockedList<UUID>();

        public readonly Timer Timer = new Timer();
        public bool UseForcedSleep = true;
        public double ForcedSleepFactor = 1;

        private void OnTimerEvent(object sender, ElapsedEventArgs e)
        {
            lock (this)
            {
                PostEvent(new TimerEvent());
            }
        }


        public override double ExecutionTime
        {
            get
            {
                lock(this) return m_ExecutionTime;
            }
            set
            {
                lock(this) m_ExecutionTime = value;
            }
        }

        public void ForcedSleep(double secs)
        {
            if (UseForcedSleep)
            {
                Sleep(secs * ForcedSleepFactor);
            }
        }

        public void AddState(string name, LSLState state)
        {
            m_States.Add(name, state);
        }

        void WriteListItem(XmlTextWriter writer, object o)
        {
            writer.WriteStartElement("ListItem");
            writer.WriteStartAttribute("type");
            writer.WriteValue(o.GetType().FullName);
            writer.WriteEndAttribute();
            writer.WriteValue(o.ToString());
            writer.WriteEndElement();
        }

        public void ToXml(XmlTextWriter writer)
        {
            writer.WriteStartElement("State");
            writer.WriteStartAttribute("UUID");
            writer.WriteValue(Item.ID);
            writer.WriteEndAttribute();
            writer.WriteStartAttribute("Asset");
            writer.WriteValue(Item.AssetID);
            writer.WriteEndAttribute();
            writer.WriteStartAttribute("Engine");
            writer.WriteValue("XEngine");
            writer.WriteEndAttribute();
            {
                writer.WriteStartElement("ScriptState");
                {
                    string current_state = "default";
                    foreach (KeyValuePair<string, LSLState> kvp in m_States)
                    {
                        if (kvp.Value == m_CurrentState)
                        {
                            current_state = kvp.Key;
                        }
                    }
                    writer.WriteStartElement("State");
                    {
                        writer.WriteValue(current_state);
                    }
                    writer.WriteEndElement();
                    writer.WriteStartElement("Running");
                    {
                        writer.WriteValue(IsRunning);
                    }
                    writer.WriteEndElement();
                    writer.WriteStartElement("Variables");
                    {
                        
                    }
                    writer.WriteEndElement();
                    writer.WriteStartElement("Queue");
                    {

                    }
                    writer.WriteEndElement();
                    writer.WriteStartElement("Plugins");
                    {
                        /*
                        List<object> listeners = new List<object>();
                        m_Listeners.ForEach(delegate(KeyValuePair<int, ChatServiceInterface.Listener> kvp)
                        {
                            listeners.Add(kvp.Value.IsActive);
                            listeners.Add(kvp.Key);
                            listeners.Add(kvp.Value.Channel);
                            listeners.Add(kvp.Value.Name);
                            listeners.Add(kvp.Value.ID);
                            listeners.Add(kvp.Value.Message);
                            listeners.Add(kvp.Value.RegexBitfield);
                        });
                         * if(listeners.Count != 0)
                         * {
                         * WriteListitem(writer, "listener");
                         * WriteListItem(writer, listeners.Count);
                         * foreach(object o in listeners)
                         * {
                         * WriteListItem(writer, o);
                         * }
                         * }
                        */

                        if(Timer.Enabled)
                        {
                            WriteListItem(writer, "timer");
                            WriteListItem(writer, 2);
                            WriteListItem(writer, Timer.Interval);
                            WriteListItem(writer, 0);
                        }

                        /* sensor, <element count> 
                         * foreach(sensor)
                         * {
                        data.Add(ts.interval);
                        data.Add(ts.name);
                        data.Add(ts.keyID);
                        data.Add(ts.type);
                        data.Add(ts.range);
                        data.Add(ts.arc);
                         * }
                         
                         */

                    }
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        public Script(ObjectPart part, ObjectPartInventoryItem item, bool forcedSleepDefault)
        {
            UseForcedSleep = forcedSleepDefault;
            m_Part = part;
            m_Item = item;
            Timer.Elapsed += OnTimerEvent;
            /* we replace the loaded script state with ours */
            m_Item.ScriptState = this;
            m_Part.OnUpdate += OnPrimUpdate;
            m_Part.OnPositionChange += OnPrimPositionUpdate;
            m_Part.ObjectGroup.OnUpdate += OnGroupUpdate;
            m_Part.ObjectGroup.OnPositionChange += OnGroupPositionUpdate;
        }

        private void OnPrimPositionUpdate(IObject part)
        {

        }

        private void OnGroupPositionUpdate(IObject group)
        {

        }

        private void OnPrimUpdate(ObjectPart part, ChangedEvent.ChangedFlags flags)
        {
            if(flags != 0)
            {
                ChangedEvent e = new ChangedEvent();
                e.Flags = flags;
                PostEvent(e);
            }
        }

        private void OnGroupUpdate(ObjectGroup group, ChangedEvent.ChangedFlags flags)
        {
            if (flags != 0)
            {
                ChangedEvent e = new ChangedEvent();
                e.Flags = flags;
                PostEvent(e);
            }
        }

        public override ObjectPart Part
        {
            get
            {
                return m_Part;
            }
        }

        public override ObjectPartInventoryItem Item 
        {
            get
            {
                return m_Item;
            }
        }

        public override void PostEvent(IScriptEvent e)
        {
            if (IsRunning && !IsAborting)
            {
                m_Events.Enqueue(e);
            }
        }

        public override void Reset()
        {
            if (IsRunning && !IsAborting)
            {
                m_Events.Enqueue(new ResetScriptEvent());
                /* TODO: add to script thread pool */
            }
        }

        public override bool IsRunning { get; set; }

        public override void Remove()
        {
            Timer.Elapsed -= OnTimerEvent;
            m_Part.OnUpdate -= OnPrimUpdate;
            m_Part.OnPositionChange -= OnPrimPositionUpdate;
            m_Part.ObjectGroup.OnUpdate -= OnGroupUpdate;
            m_Part.ObjectGroup.OnPositionChange -= OnGroupPositionUpdate;
            IsRunning = false;
            m_Events.Clear();
            m_States.Clear();
            m_Part = null;
        }

        private Dictionary<string, MethodInfo> m_CurrentStateMethods = new Dictionary<string, MethodInfo>();

        public override void RevokePermissions(UUID permissionsKey, ScriptPermissions permissions)
        {
            ObjectPartInventoryItem.PermsGranterInfo grantinfo = Item.PermsGranter;
            if (permissionsKey == grantinfo.PermsGranter.ID && grantinfo.PermsGranter != UUI.Unknown)
            {
                IAgent agent;
                try
                {
                    agent = Part.ObjectGroup.Scene.Agents[grantinfo.PermsGranter.ID];
                }
                catch
                {
                   return;
                }
                agent.RevokePermissions(Part.ID, Item.ID, (~permissions) & (grantinfo.PermsMask));
                grantinfo.PermsMask &= (~permissions);
                if (ScriptPermissions.None == grantinfo.PermsMask)
                {
                    Item.PermsGranter = null;
                }
                else
                {
                    Item.PermsGranter = grantinfo;
                }
            }
        }

        private void InvokeStateEvent(string name, params object[] param)
        {
            MethodInfo mi;
            if (!m_CurrentStateMethods.TryGetValue(name, out mi))
            {
                mi = m_CurrentState.GetType().GetMethod(name);
                m_CurrentStateMethods.Add(name, mi);
            }

            if (null != mi)
            {
                mi.Invoke(m_CurrentState, param);
            }
        }

        public override void ProcessEvent()
        {
            IScriptEvent ev;
            try
            {
                ev = m_Events.Dequeue();
                if(m_CurrentState == null)
                {
                    m_CurrentState = m_States["default"];
                    InvokeStateEvent("state_entry");
                }
            }
            catch
            {
                return;
            }

            int startticks = Environment.TickCount;
            try
            {
                if (ev is AtRotTargetEvent)
                {
                    AtRotTargetEvent e = (AtRotTargetEvent)ev;
                    InvokeStateEvent("at_rot_target", e.TargetRotation, e.OurRotation);
                }
                else if (ev is AttachEvent)
                {
                    AttachEvent e = (AttachEvent)ev;
                    InvokeStateEvent("attach", new LSLKey(e.ObjectID));
                }
                else if (ev is AtTargetEvent)
                {
                    AtTargetEvent e = (AtTargetEvent)ev;
                    InvokeStateEvent("at_target", e.Handle, e.TargetPosition, e.OurPosition);
                }
                else if (ev is ChangedEvent)
                {
                    ChangedEvent e = (ChangedEvent)ev;
                    m_CurrentState.GetType().GetMethod("changed").Invoke(m_CurrentState, new object[] {(int)e.Flags});
                }
                else if (ev is CollisionEvent)
                {
                    m_Detected = ((CollisionEvent)ev).Detected;
                    switch(((CollisionEvent)ev).Type)
                    {
                        case CollisionEvent.CollisionType.Start:
                            InvokeStateEvent("collision_start", m_Detected.Count);
                            break;

                        case CollisionEvent.CollisionType.End:
                            InvokeStateEvent("collision_end", m_Detected.Count);
                            break;

                        case CollisionEvent.CollisionType.Continuous:
                            InvokeStateEvent("collision", m_Detected.Count);
                            break;

                        default:
                            break;
                    }
                }
                else if (ev is DataserverEvent)
                {
                    DataserverEvent e = (DataserverEvent)ev;
                    InvokeStateEvent("dataserver", new LSLKey(e.QueryID), e.Data);
                }
                else if(ev is MessageObjectEvent)
                {
                    MessageObjectEvent e = (MessageObjectEvent)ev;
                    if (UseMessageObjectEvent)
                    {
                        InvokeStateEvent("object_message", new LSLKey(e.ObjectID), e.Data);
                    }
                    else
                    {
                        InvokeStateEvent("dataserver", new LSLKey(e.ObjectID), e.Data);
                    }
                }
                else if (ev is EmailEvent)
                {
                    EmailEvent e = (EmailEvent)ev;
                    InvokeStateEvent("email", e.Time, e.Address, e.Subject, e.Message, e.NumberLeft);
                }
                else if (ev is HttpRequestEvent)
                {
                    HttpRequestEvent e = (HttpRequestEvent)ev;
                    InvokeStateEvent("http_request", new LSLKey(e.RequestID), e.Method, e.Body);
                }
                else if (ev is HttpResponseEvent)
                {
                    HttpResponseEvent e = (HttpResponseEvent)ev;
                    InvokeStateEvent("http_response", new LSLKey(e.RequestID), e.Status, e.Metadata, e.Body);
                }
                else if (ev is LandCollisionEvent)
                {
                    LandCollisionEvent e = (LandCollisionEvent)ev;
                    switch(e.Type)
                    {
                        case LandCollisionEvent.CollisionType.Start:
                            InvokeStateEvent("land_collision_start", e.Position);
                            break;

                        case LandCollisionEvent.CollisionType.End:
                            InvokeStateEvent("land_collision_end", e.Position);
                            break;

                        case LandCollisionEvent.CollisionType.Continuous:
                            InvokeStateEvent("land_collision", e.Position);
                            break;

                        default:
                            break;
                    }
                }
                else if (ev is LinkMessageEvent)
                {
                    LinkMessageEvent e = (LinkMessageEvent)ev;
                    InvokeStateEvent("link_message", e.SenderNumber, e.Number, e.Data, new LSLKey(e.Id));
                }
                else if (ev is ListenEvent)
                {
                    ListenEvent e = (ListenEvent)ev;
                    InvokeStateEvent("listen", e.Channel, e.Name, new LSLKey(e.ID), e.Message);
                }
                else if (ev is MoneyEvent)
                {
                    MoneyEvent e = (MoneyEvent)ev;
                    InvokeStateEvent("money", e.ID, e.Amount);
                }
                else if (ev is MovingEndEvent)
                {
                    InvokeStateEvent("moving_end");
                }
                else if (ev is MovingStartEvent)
                {
                    InvokeStateEvent("moving_start");
                }
                else if (ev is NoSensorEvent)
                {
                    InvokeStateEvent("no_sensor");
                }
                else if (ev is NotAtRotTargetEvent)
                {
                    InvokeStateEvent("not_at_rot_target");
                }
                else if (ev is NotAtTargetEvent)
                {
                    InvokeStateEvent("not_at_target");
                }
                else if (ev is ObjectRezEvent)
                {
                    ObjectRezEvent e = (ObjectRezEvent)ev;
                    InvokeStateEvent("object_rez", new LSLKey(e.ObjectID));
                }
                else if (ev is OnRezEvent)
                {
                    OnRezEvent e = (OnRezEvent)ev;
                    StartParameter = new Integer(e.StartParam);
                    InvokeStateEvent("on_rez", e.StartParam);
                }
                else if (ev is PathUpdateEvent)
                {
                    PathUpdateEvent e = (PathUpdateEvent)ev;
                    InvokeStateEvent("path_update", e.Type, e.Reserved);
                }
                else if (ev is RemoteDataEvent)
                {
                    RemoteDataEvent e =(RemoteDataEvent)ev;
                    InvokeStateEvent("remote_data", e.Type, new LSLKey(e.Channel), new LSLKey(e.MessageID), e.Sender, e.IData, e.SData);
                }
                else if (ev is ResetScriptEvent)
                {
                    throw new ResetScriptException();
                }
                else if (ev is RuntimePermissionsEvent)
                {
                    RuntimePermissionsEvent e = (RuntimePermissionsEvent)ev;
                    if(e.PermissionsKey != Item.Owner)
                    {
                        e.Permissions &= ~(ScriptPermissions.Debit | ScriptPermissions.SilentEstateManagement | ScriptPermissions.ChangeLinks);
                    }
                    if(e.PermissionsKey != Item.Owner)
                    {
#warning Add group support here (also allowed are group owners)
                        e.Permissions &= ~ScriptPermissions.ReturnObjects;
                    }
                    if(Item.IsGroupOwned)
                    {
                        e.Permissions &= ~ScriptPermissions.Debit;
                    }

                    ObjectPartInventoryItem.PermsGranterInfo grantinfo = new ObjectPartInventoryItem.PermsGranterInfo();
                    grantinfo.PermsGranter = e.PermissionsKey;
                    grantinfo.PermsMask = (ScriptPermissions)e.Permissions;
                    Item.PermsGranter = grantinfo;
                    InvokeStateEvent("run_time_permissions", (ScriptPermissions)e.Permissions);
                }
                else if (ev is SensorEvent)
                {
                    SensorEvent e = (SensorEvent)ev;
                    m_Detected = e.Data;
                    InvokeStateEvent("sensor", m_Detected.Count);
                }
                else if (ev is TouchEvent)
                {
                    TouchEvent e = (TouchEvent)ev;
                    m_Detected = e.Detected;
                    switch(e.Type)
                    {
                        case TouchEvent.TouchType.Start:
                            InvokeStateEvent("touch_start", m_Detected.Count);
                            break;

                        case TouchEvent.TouchType.End:
                            InvokeStateEvent("touch_end", m_Detected.Count);
                            break;

                        case TouchEvent.TouchType.Continuous:
                            InvokeStateEvent("touch", m_Detected.Count);
                            break;

                        default:
                            break;
                    }
                }
            }
            catch(ResetScriptException)
            {
                TriggerOnStateChange();
                TriggerOnScriptReset();
                m_Events.Clear();
                lock(this)
                {
                    m_ExecutionTime = 0f;
                }
                m_CurrentState = m_States["default"];
                m_CurrentStateMethods.Clear();
                StartParameter = 0;
                startticks = Environment.TickCount;
                InvokeStateEvent("state_entry");
            }
            catch(System.Threading.ThreadAbortException)
            {
                throw;
            }
            catch(ChangeStateException e)
            {
                if (m_CurrentState != m_States[e.NewState])
                {
                    /* if state is equal, it simply aborts the event execution */
                    TriggerOnStateChange();
                    m_Events.Clear();
                    InvokeStateEvent("state_exit");
                    m_CurrentState = m_States[e.NewState];
                    m_CurrentStateMethods.Clear();
                    InvokeStateEvent("state_entry");
                }
            }
            catch (NotImplementedException e)
            {
                ShoutError("Script uses not implemented function " + e.StackTrace[0].ToString());
            }
            catch (Exception e)
            {
                ShoutError(e.Message);
            }
            int exectime = Environment.TickCount - startticks;
            float execfloat = exectime / 1000f;
            lock(this)
            {
                m_ExecutionTime += execfloat;
            }
        }

        public override bool HasEventsPending
        { 
            get
            {
                return m_Events.Count != 0;
            }
        }

        internal void onListen(ListenEvent ev)
        {
            PostEvent(ev);
        }


        public override void ShoutError(string message)
        {
            ListenEvent ev = new ListenEvent();
            ev.Channel = 0x7FFFFFFF; /* DEBUG_CHANNEL */
            ev.Type = ListenEvent.ChatType.Shout;
            ev.Message = message;
            ev.SourceType = ListenEvent.ChatSourceType.Object;
            ev.OwnerID = Part.ObjectGroup.Owner.ID;
            lock (this)
            {
                ev.ID = Part.ObjectGroup.ID;
                ev.Name = Part.ObjectGroup.Name;
                Part.ObjectGroup.Scene.GetService<ChatServiceInterface>().Send(ev);
            }
        }

    }
}
