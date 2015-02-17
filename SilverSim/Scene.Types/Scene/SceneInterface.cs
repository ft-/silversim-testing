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

using log4net;
using SilverSim.LL.Messages;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Physics;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Economy;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Types;
using SilverSim.Types.Economy;
using SilverSim.Types.Grid;
using SilverSim.Types.Parcel;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using ThreadedClasses;

namespace SilverSim.Scene.Types.Scene
{
    public interface ISceneObjects : IEnumerable<IObject>
    {
        IObject this[UUID id] { get; }
        void ForEach(Vector3 pos, double maxdistance, Action<IObject> d);
        int Count { get; }
    }

    public interface ISceneObjectGroups : IEnumerable<ObjectGroup>
    {
        ObjectGroup this[UUID id] { get; }
        int Count { get; }
    }

    public interface ISceneObjectParts : IEnumerable<ObjectPart>
    {
        ObjectPart this[UUID id] { get; }
        ObjectPart this[UInt32 localId] { get; }
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

    public abstract partial class SceneInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("SCENE");

        public readonly RegionSettings RegionSettings = new RegionSettings();

        public UUID ID { get; protected set; }
        public UUID RegionSecret { get; private set; }
        public uint RegionPort { get; protected set; }
        public uint SizeX { get; private set; }
        public uint SizeY { get; private set; }
        public string Name { get; protected set; }
        public IPAddress LastIPAddress { get; protected set; }
        public string ExternalHostName { get; protected set; }
        public GridVector GridPosition { get; protected set; }
        public abstract ISceneObjects Objects { get; }
        public abstract ISceneObjectGroups ObjectGroups { get; }
        public abstract ISceneObjectParts Primitives { get; }
        public abstract ISceneAgents Agents { get; }
        public abstract ISceneAgents RootAgents { get; }
        public abstract ISceneParcels Parcels { get; }
        public event Action<SceneInterface> OnRemove;
        public delegate void IPChangedDelegate(SceneInterface scene, IPAddress address);
        public event IPChangedDelegate OnIPChanged;
        public AssetServiceInterface TemporaryAssetService { get; protected set; }
        public AssetServiceInterface PersistentAssetService { get; protected set; }
        public AssetServiceInterface AssetService { get; private set; }
        public GroupsNameServiceInterface GroupsNameService { get; protected set; }
        public AvatarNameServiceInterface AvatarNameService { get; private set; }
        public readonly RwLockedList<AvatarNameServiceInterface> AvatarNameServices = new RwLockedList<AvatarNameServiceInterface>();
        public GridServiceInterface GridService { get; protected set; }
        public EconomyServiceInterface EconomyService { get; protected set; }
        public ServerParamServiceInterface ServerParamService { get; protected set; }
        public EconomyInfo EconomyData { get; protected set; }
        private NotecardCache m_NotecardCache;
        public Dictionary<string, string> CapabilitiesConfig { get; protected set; }
        public string GatekeeperURI { get; protected set; }
        public bool IsSceneEnabled { get; protected set; }

        public abstract void LoadSceneAsync();

        #region Physics
        IPhysicsScene m_PhysicsScene = DummyPhysicsScene.SharedInstance;
        object m_PhysicsSceneChangeLock = new object();

        public IPhysicsScene PhysicsScene
        {
            get
            {
                lock (m_PhysicsSceneChangeLock)
                {
                    return m_PhysicsScene;
                }
            }
            set
            {
                if(value == null)
                {
                    throw new ArgumentNullException();
                }
                lock (m_PhysicsSceneChangeLock)
                {
                    m_PhysicsScene.RemoveAll();
                    m_PhysicsScene = value;
                    foreach(ObjectPart p in Primitives)
                    {
                        m_PhysicsScene.Add(p);
                    }
                }
            }
        }
        #endregion

        /* do not put any other than ICapabilityInterface into this list */
        public readonly RwLockedDictionary<string, object> SceneCapabilities = new RwLockedDictionary<string, object>();

        protected readonly RwLockedDictionary<MessageType, Action<Message>> m_PacketHandlers = new RwLockedDictionary<MessageType, Action<Message>>();

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
            else if(typeof(T).IsAssignableFrom(typeof(AvatarNameServiceInterface)))
            {
                return (T)(object)AvatarNameService;
            }
            else if(typeof(T).IsAssignableFrom(typeof(GroupsNameServiceInterface)))
            {
                return (T)(object)GroupsNameService;
            }
            else if (typeof(T).IsAssignableFrom(typeof(GridServiceInterface)))
            {
                return (T)(object)GridService;
            }
            else if(typeof(T).IsAssignableFrom(typeof(NotecardCache)))
            {
                return (T)(object)m_NotecardCache;
            }
            else if(typeof(T).IsAssignableFrom(typeof(EconomyServiceInterface)))
            {
                return (T)(object)EconomyService;
            }
            else if(typeof(T).IsAssignableFrom(typeof(EnvironmentController)))
            {
                return (T)(object)Environment;
            }
            else if (typeof(T).IsAssignableFrom(typeof(TerrainController)))
            {
                return (T)(object)Terrain;
            }
            else
            {
                throw new ArgumentException();
            }
        }

        private const uint PARCEL_BLOCK_SIZE = 4;

        public SceneInterface(UInt32 sizeX, UInt32 sizeY)
        {
            IsSceneEnabled = false;
            SizeX = sizeX;
            SizeY = sizeY;
            AssetService = new DefaultAssetService(this);
            AvatarNameService = new DefaultAvatarNameService(AvatarNameServices);
            Owner = new UUI();
            CapabilitiesConfig = new Dictionary<string, string>();
            RegionSecret = UUID.Random;
            LastIPAddress = new IPAddress(0);
            m_NotecardCache = new NotecardCache(this);

            /* region */
            m_PacketHandlers[MessageType.RegionHandleRequest] = HandleRegionHandleRequest;

            /* scripts */
            m_PacketHandlers[MessageType.ScriptAnswerYes] = HandleScriptAnswerYes;

            /* objects */
            m_PacketHandlers[MessageType.DeRezObject] = HandleDeRezObject;
            m_PacketHandlers[MessageType.RequestPayPrice] = HandleRequestPayPrice;
            m_PacketHandlers[MessageType.ObjectSpinStart] = HandleObjectSpinStart;
            m_PacketHandlers[MessageType.ObjectSpinUpdate] = HandleObjectSpinUpdate;
            m_PacketHandlers[MessageType.ObjectSpinStop] = HandleObjectSpinStop;
            m_PacketHandlers[MessageType.ObjectShape] = HandleObjectShape;
            m_PacketHandlers[MessageType.ObjectSaleInfo] = HandleObjectSaleInfo;
            m_PacketHandlers[MessageType.ObjectRotation] = HandleObjectRotation;
            m_PacketHandlers[MessageType.ObjectPermissions] = HandleObjectPermissions;
            m_PacketHandlers[MessageType.ObjectOwner] = HandleObjectOwner;
            m_PacketHandlers[MessageType.ObjectName] = HandleObjectName;
            m_PacketHandlers[MessageType.ObjectMaterial] = HandleObjectMaterial;
            m_PacketHandlers[MessageType.ObjectLink] = HandleObjectLink;
            m_PacketHandlers[MessageType.ObjectDelink] = HandleObjectDelink;
            m_PacketHandlers[MessageType.ObjectGroup] = HandleObjectGroup;
            m_PacketHandlers[MessageType.ObjectIncludeInSearch] = HandleObjectIncludeInSearch;
            m_PacketHandlers[MessageType.ObjectFlagUpdate] = HandleObjectFlagUpdate;
            m_PacketHandlers[MessageType.ObjectExtraParams] = HandleObjectExtraParams;
            m_PacketHandlers[MessageType.ObjectExportSelected] = HandleObjectExportSelected;
            m_PacketHandlers[MessageType.ObjectSelect] = HandleObjectSelect;
            m_PacketHandlers[MessageType.ObjectDrop] = HandleObjectDrop;
            m_PacketHandlers[MessageType.ObjectAttach] = HandleObjectAttach;
            m_PacketHandlers[MessageType.ObjectDetach] = HandleObjectDetach;
            m_PacketHandlers[MessageType.ObjectDescription] = HandleObjectDescription;
            m_PacketHandlers[MessageType.ObjectDeselect] = HandleObjectDeselect;
            m_PacketHandlers[MessageType.ObjectClickAction] = HandleObjectClickAction;
            m_PacketHandlers[MessageType.ObjectCategory] = HandleObjectCategory;
            m_PacketHandlers[MessageType.ObjectBuy] = HandleObjectBuy;
            m_PacketHandlers[MessageType.BuyObjectInventory] = HandleBuyObjectInventory;
            m_PacketHandlers[MessageType.ObjectGrab] = HandleObjectGrab;
            m_PacketHandlers[MessageType.ObjectGrabUpdate] = HandleObjectGrabUpdate;
            m_PacketHandlers[MessageType.ObjectDeGrab] = HandleObjectDeGrab;
            m_PacketHandlers[MessageType.RequestObjectPropertiesFamily] = HandleRequestObjectPropertiesFamily;

            /* sound */
            m_PacketHandlers[MessageType.SoundTrigger] = HandleSoundTrigger;

            InitializeParcelLayer();
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
                        m_Log.DebugFormat("Exception {0}:{1} at {2}", e.GetType().Name, e.Message, e.StackTrace.ToString());
                    }
                }
            }
        }

        public abstract void Add(IObject obj);
        public abstract bool Remove(IObject obj, Script.ScriptInstance instance = null);

        public abstract ILLUDPServer UDPServer { get; }

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
                        m_Log.DebugFormat("Exception {0}:{1} at {2}", e.GetType().Name, e.Message, e.StackTrace.ToString());
                    }
                }
            }
        }

        private readonly RwLockedDictionary<UInt32, IObject> m_LocalIDs = new RwLockedDictionary<uint, IObject>();
        private UInt32 m_LastLocalID = 0;
        private object m_LastLocalIDLock = new object();

        private UInt32 NextLocalID
        {
            get
            {
                UInt32 newLocalID;
                lock(m_LastLocalIDLock)
                {
                    ++m_LastLocalID;
                    if(0 == m_LastLocalID)
                    {
                        ++m_LastLocalID;
                    }
                    newLocalID = m_LastLocalID;
                }
                return newLocalID;
            }
        }

        protected void AddNewLocalID(IObject v)
        {
            do
            {
                try
                {
                    UInt32 localID = NextLocalID;
                    m_LocalIDs.Add(localID, v);
                    v.LocalID = localID;
                    break;
                }
                catch
                {

                }
            } while (true);
        }

        protected void RemoveLocalID(IObject v)
        {
            m_LocalIDs.Remove(v.LocalID);
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

        #region Scene LL Message interface
        public void HandleSimulatorMessage(Message m)
        {
            Action<Message> del;
            if(m_PacketHandlers.TryGetValue(m.Number, out del))
            {
                del(m);
            }
        }
        #endregion
    }
}
