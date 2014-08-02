/*

ArribaSim is distributed under the terms of the
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

using ArribaSim.Scene.Types.Agent;
using ArribaSim.Scene.Types.Object;
using ArribaSim.Scene.Types.Parcel;
using ArribaSim.Scene.Types.Terrain;
using ArribaSim.Types.Grid;
using ArribaSim.ServiceInterfaces.Asset;
using ArribaSim.ServiceInterfaces.Avatar;
using ArribaSim.ServiceInterfaces.Grid;
using ArribaSim.ServiceInterfaces.GridUser;
using ArribaSim.ServiceInterfaces.Groups;
using ArribaSim.ServiceInterfaces.Presence;
using ArribaSim.Types;
using log4net;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace ArribaSim.Scene.Types.Scene
{
    public interface ISceneObjects : IEnumerable<IObject>
    {
        IObject this[UUID id] { get; }
        void ForEach(Vector3 pos, double maxdistance, Action<IObject> d);
    }

    public interface ISceneObjectGroups : IEnumerable<ObjectGroup>
    {
        ObjectGroup this[UUID id] { get; }
        int Count { get; }
    }

    public interface ISceneObjectParts : IEnumerable<ObjectPart>
    {
        ObjectPart this[UUID id] { get; }
        int Count { get; }
    }

    public interface ISceneAgents : IEnumerable<IAgent>
    {
        IAgent this[UUID id] { get; }
        int Count { get; }
    }

    public interface ISceneParcels : IEnumerable<ParcelInfo>
    {
        ParcelInfo this[UUID id] { get; }
        ParcelInfo this[Vector3 position] { get; }
    }

    public abstract class SceneInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public UUID ID { get; protected set; }
        public UUID RegionSecret { get; private set; }
        public uint RegionPort { get; protected set; }
        public uint SizeX { get; protected set; }
        public uint SizeY { get; protected set; }
        public string Name { get; protected set; }
        public IPAddress LastIPAddress { get; protected set; }
        public string ExternalHostName { get; protected set; }
        public TerrainMap Terrain { get; protected set; }
        public GridVector GridPosition { get; protected set; }
        public abstract ISceneObjects Objects { get; }
        public abstract ISceneObjectGroups ObjectGroups { get; }
        public abstract ISceneObjectParts Primitives { get; }
        public abstract ISceneAgents Agents { get; }
        public abstract ISceneParcels Parcels { get; }
        public event Action<SceneInterface> OnRemove;
        public delegate void IPChangedDelegate(SceneInterface scene, IPAddress address);
        public event IPChangedDelegate OnIPChanged;
        public AssetServiceInterface AssetService { get; protected set; }
        public GroupsServiceInterface GroupsService { get; protected set; }
        public AvatarServiceInterface AvatarService { get; protected set; }
        public PresenceServiceInterface PresenceService { get; protected set; }
        public GridUserServiceInterface GridUserService { get; protected set; }
        public GridServiceInterface GridService { get; protected set; }
        private NotecardCache m_NotecardCache;

        public RegionInfo RegionData
        {
            get
            {
                RegionInfo reg = new RegionInfo();
                reg.Access = 0;
                reg.Flags = RegionFlags.RegionOnline;
                reg.ID = ID;
                reg.Location = GridPosition;
                reg.Name = Name;
                reg.Owner = Owner;
                reg.ParcelMapTexture = UUID.Zero;
                reg.RegionMapTexture = UUID.Zero;
                reg.RegionSecret = RegionSecret;
                reg.ScopeID = UUID.Zero;
                reg.ServerIP = ExternalHostName;
                reg.ServerPort = RegionPort;
                reg.Size.X = SizeX;
                reg.Size.Y = SizeY;
                return reg;
            }
        }

        public UUI Owner { get; protected set; }
        public virtual T GetService<T>()
        {
            if(typeof(T).IsAssignableFrom(typeof(AssetServiceInterface)))
            {
                return (T)(object)AssetService;
            }
            else if(typeof(T).IsAssignableFrom(typeof(GroupsServiceInterface)))
            {
                return (T)(object)GroupsService;
            }
            else if(typeof(T).IsAssignableFrom(typeof(AvatarServiceInterface)))
            {
                return (T)(object)AvatarService;
            }
            else if (typeof(T).IsAssignableFrom(typeof(PresenceServiceInterface)))
            {
                return (T)(object)PresenceService;
            }
            else if (typeof(T).IsAssignableFrom(typeof(GridUserServiceInterface)))
            {
                return (T)(object)GridUserService;
            }
            else if (typeof(T).IsAssignableFrom(typeof(GridServiceInterface)))
            {
                return (T)(object)GridService;
            }
            else if(typeof(T).IsAssignableFrom(typeof(NotecardCache)))
            {
                return (T)(object)m_NotecardCache;
            }
            else
            {
                throw new ArgumentException();
            }
        }

        public SceneInterface()
        {
            RegionSecret = UUID.Random;
            LastIPAddress = new IPAddress(0);
            m_NotecardCache = new NotecardCache(this);
        }

        public void InvokeOnRemove()
        {
            if (null != OnRemove)
            {
                foreach (Action<SceneInterface> del in OnRemove.GetInvocationList())
                {
                    try
                    {
                        del(this);
                    }
                    catch (Exception e)
                    {
                        m_Log.DebugFormat("[SCENE]: Exception {0}:{1} at {2}", e.GetType().Name, e.Message, e.StackTrace.ToString());
                    }
                }
            }
        }

        public abstract void Add(IObject obj);
        public abstract bool Remove(IObject obj);

        public void TriggerIPChanged(IPAddress ip)
        {
            LastIPAddress = ip;
            if (OnIPChanged != null)
            {
                foreach (IPChangedDelegate del in OnIPChanged.GetInvocationList())
                {
                    try
                    {
                        del(this, ip);
                    }
                    catch (Exception e)
                    {
                        m_Log.DebugFormat("[SCENE]: Exception {0}:{1} at {2}", e.GetType().Name, e.Message, e.StackTrace.ToString());
                    }
                }
            }
        }

        #region Dynamic IP Support
        public void CheckExternalNameLookup()
        {
            IPAddress[] addresses = Dns.GetHostAddresses(ExternalHostName);
            for (int i = 0; i < addresses.Length; ++i)
            {
                if (addresses[i].AddressFamily == AddressFamily.InterNetwork)
                {
                    /* we take the first IPv4 address */
                    if (!LastIPAddress.Equals(addresses[i]))
                    {
                        TriggerIPChanged(LastIPAddress);
                    }
                    return;
                }
            }
        }
        #endregion
    }
}
