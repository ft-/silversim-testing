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
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using ThreadedClasses;

namespace SilverSim.Scripting.LSL.Variants.LSL
{
    public partial class LSLScript : IScriptInstance
    {
        public class ResetScriptException : Exception
        {
            public ResetScriptException()
            {

            }
        }

        public class ChangeStateException : Exception
        {
            public string NewState { get; private set; }
            public ChangeStateException(string newstate)
            {
                NewState = newstate;
            }
        }

        public static int MaxListenerHandles = 64;

        private ObjectPart m_Part;
        private NonblockingQueue<IScriptEvent> m_Events = new NonblockingQueue<IScriptEvent>();
        protected List<DetectInfo> m_Detected = new List<DetectInfo>();
        private Dictionary<string, LSLState> m_States = new Dictionary<string, LSLState>();
        private LSLState m_CurrentState = null;
        public Integer StartParameter = new Integer();
        protected RwLockedDictionary<int, ChatServiceInterface.Listener> m_Listeners = new RwLockedDictionary<int, ChatServiceInterface.Listener>();
        private double m_ExecutionTime = 0;

        public double ExecutionTime
        {
            get
            {
                return m_ExecutionTime;
            }
        }

        public void AddState(string name, LSLState state)
        {
            m_States.Add(name, state);
        }

        public LSLScript(ObjectPart part)
        {
            m_Part = part;
            m_Part.OnUpdate += OnPrimUpdate;
            m_Part.OnPositionChange += OnPrimPositionUpdate;
            m_Part.Group.OnUpdate += OnGroupUpdate;
            m_Part.Group.OnPositionChange += OnGroupPositionUpdate;
        }

        private void OnPrimPositionUpdate(IObject part)
        {

        }

        private void OnGroupPositionUpdate(IObject group)
        {

        }

        private void OnPrimUpdate(ObjectPart part, int flags)
        {
            if(flags != 0)
            {
                ChangedEvent e = new ChangedEvent();
                e.Flags = flags;
                PostEvent(e);
            }
        }

        private void OnGroupUpdate(ObjectGroup group, int flags)
        {
            if (flags != 0)
            {
                ChangedEvent e = new ChangedEvent();
                e.Flags = flags;
                PostEvent(e);
            }
        }

        public void Dispose()
        {
            m_Part.OnUpdate -= OnPrimUpdate;
            m_Part.OnPositionChange -= OnPrimPositionUpdate;
            m_Part.Group.OnUpdate -= OnGroupUpdate;
            m_Part.Group.OnPositionChange -= OnGroupPositionUpdate;

            m_Timer.Enabled = false;
            IsRunning = false;
            m_Events.Clear();
            ResetListeners();

            m_States.Clear();
            m_Part = null;
        }

        public ObjectPart Part
        {
            get
            {
                return m_Part;
            }
        }

        public void PostEvent(IScriptEvent e)
        {
            if (IsRunning)
            {
                m_Events.Enqueue(e);
            }
        }

        public void Reset()
        {
            if (IsRunning)
            {
                m_Events.Enqueue(new ResetScriptEvent());
                /* TODO: add to script thread pool */
            }
        }

        public bool IsRunning { get; set; }

        public void Remove()
        {
            m_Part.OnUpdate -= OnPrimUpdate;
            m_Part.OnPositionChange -= OnPrimPositionUpdate;
            m_Part.Group.OnUpdate -= OnGroupUpdate;
            m_Part.Group.OnPositionChange -= OnGroupPositionUpdate;
            m_Timer.Enabled = false;
            IsRunning = false;
            m_Events.Clear();
            ResetListeners();
            m_States.Clear();
            m_Part = null;
        }

        public void ProcessEvent()
        {
            IScriptEvent ev;
            try
            {
                ev = m_Events.Dequeue();
                if(m_CurrentState == null)
                {
                    m_CurrentState = m_States["default"];
                    m_CurrentState.state_entry();
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
                    m_CurrentState.at_rot_target(new Integer(e.Handle), e.TargetRotation, e.OurRotation);
                }
                else if (ev is AttachEvent)
                {
                    AttachEvent e = (AttachEvent)ev;
                    m_CurrentState.attach(e.ObjectID);
                }
                else if (ev is AtTargetEvent)
                {
                    AtTargetEvent e = (AtTargetEvent)ev;
                    m_CurrentState.at_target(new Integer(e.Handle), e.TargetPosition, e.OurPosition);
                }
                else if (ev is ChangedEvent)
                {
                    ChangedEvent e = (ChangedEvent)ev;
                    m_CurrentState.changed(new Integer(e.Flags));
                }
                else if (ev is CollisionEvent)
                {
                    m_Detected = ((CollisionEvent)ev).Detected;
                    switch(((CollisionEvent)ev).Type)
                    {
                        case CollisionEvent.CollisionType.Start:
                            m_CurrentState.collision_start(new Integer(m_Detected.Count));
                            break;

                        case CollisionEvent.CollisionType.End:
                            m_CurrentState.collision_end(new Integer(m_Detected.Count));
                            break;

                        case CollisionEvent.CollisionType.Continuous:
                            m_CurrentState.collision(new Integer(m_Detected.Count));
                            break;

                        default:
                            break;
                    }
                }
                else if (ev is DataserverEvent)
                {
                    DataserverEvent e = (DataserverEvent)ev;
                    m_CurrentState.dataserver(e.QueryID, e.Data);
                }
                else if (ev is EmailEvent)
                {
                    EmailEvent e = (EmailEvent)ev;
                    m_CurrentState.email(e.Time, e.Address, e.Subject, e.Message, e.NumberLeft);
                }
                else if (ev is HttpRequestEvent)
                {
                    HttpRequestEvent e = (HttpRequestEvent)ev;
                    m_CurrentState.http_request(e.RequestID, e.Method, e.Body);
                }
                else if (ev is HttpResponseEvent)
                {
                    HttpResponseEvent e = (HttpResponseEvent)ev;
                    m_CurrentState.http_response(e.RequestID, e.Status, e.Metadata, e.Body);
                }
                else if (ev is LandCollisionEvent)
                {
                    LandCollisionEvent e = (LandCollisionEvent)ev;
                    switch(e.Type)
                    {
                        case LandCollisionEvent.CollisionType.Start:
                            m_CurrentState.land_collision_start(e.Position);
                            break;

                        case LandCollisionEvent.CollisionType.End:
                            m_CurrentState.land_collision_end(e.Position);
                            break;

                        case LandCollisionEvent.CollisionType.Continuous:
                            m_CurrentState.land_collision(e.Position);
                            break;

                        default:
                            break;
                    }
                }
                else if (ev is LinkMessageEvent)
                {
                    LinkMessageEvent e = (LinkMessageEvent)ev;
                    m_CurrentState.link_message(e.SenderNumber, e.Number, e.Data, e.Id);
                }
                else if (ev is ListenEvent)
                {
                    ListenEvent e = (ListenEvent)ev;
                    m_CurrentState.listen(e.Channel, e.Name, e.ID, e.Message);
                }
                else if (ev is MoneyEvent)
                {
                    MoneyEvent e = (MoneyEvent)ev;
                    m_CurrentState.money(e.ID, e.Amount);
                }
                else if (ev is MovingEndEvent)
                {
                    m_CurrentState.moving_end();
                }
                else if (ev is MovingStartEvent)
                {
                    m_CurrentState.moving_start();
                }
                else if (ev is NoSensorEvent)
                {
                    m_CurrentState.no_sensor();
                }
                else if (ev is NotAtRotTargetEvent)
                {
                    m_CurrentState.not_at_rot_target();
                }
                else if (ev is NotAtTargetEvent)
                {
                    m_CurrentState.not_at_target();
                }
                else if (ev is ObjectRezEvent)
                {
                    ObjectRezEvent e = (ObjectRezEvent)ev;
                    m_CurrentState.object_rez(e.ObjectID);
                }
                else if (ev is OnRezEvent)
                {
                    OnRezEvent e = (OnRezEvent)ev;
                    StartParameter = new Integer(e.StartParam);
                    m_CurrentState.on_rez(new Integer(e.StartParam));
                }
                else if (ev is PathUpdateEvent)
                {
                    PathUpdateEvent e = (PathUpdateEvent)ev;
                    m_CurrentState.path_update(new Integer(e.Type), e.Reserved);
                }
                else if (ev is RemoteDataEvent)
                {
                    RemoteDataEvent e =(RemoteDataEvent)ev;
                    m_CurrentState.remote_data(e.Type, e.Channel, e.MessageID, e.Sender, e.IData, e.SData);
                }
                else if (ev is ResetScriptEvent)
                {
                    throw new ResetScriptException();
                }
                else if (ev is RuntimePermissionsEvent)
                {
                    RuntimePermissionsEvent e = (RuntimePermissionsEvent)ev;
                    m_CurrentState.run_time_permissions(new Integer(e.Permissions));
                }
                else if (ev is SensorEvent)
                {
                    SensorEvent e = (SensorEvent)ev;
                    m_Detected = e.Data;
                    m_CurrentState.sensor(new Integer(m_Detected.Count));
                }
                else if (ev is TouchEvent)
                {
                    TouchEvent e = (TouchEvent)ev;
                    m_Detected = e.Detected;
                    switch(e.Type)
                    {
                        case TouchEvent.TouchType.Start:
                            m_CurrentState.touch_start(m_Detected.Count);
                            break;

                        case TouchEvent.TouchType.End:
                            m_CurrentState.touch_end(m_Detected.Count);
                            break;

                        case TouchEvent.TouchType.Continuous:
                            m_CurrentState.touch(m_Detected.Count);
                            break;

                        default:
                            break;
                    }
                }
            }
            catch(ResetScriptException)
            {
                ResetListeners();
                m_Timer.Enabled = false;
                m_Events.Clear();
                lock(this)
                {
                    m_ExecutionTime = 0f;
                }
                m_CurrentState = m_States["default"];
                startticks = Environment.TickCount;
                m_CurrentState.state_entry();
            }
            catch(ChangeStateException e)
            {
                ResetListeners();
                m_Timer.Enabled = false;
                m_Events.Clear();
                m_CurrentState.state_exit();
                m_CurrentState = m_States[e.NewState];
                m_CurrentState.state_entry();
            }
            catch(Exception e)
            {
                llShout(DEBUG_CHANNEL, e.Message);
            }
            int exectime = Environment.TickCount - startticks;
            float execfloat = exectime / 1000f;
            lock(this)
            {
                m_ExecutionTime += execfloat;
            }
        }

        public bool HasEventsPending
        { 
            get
            {
                return m_Events.Count != 0;
            }
        }
    }
}
