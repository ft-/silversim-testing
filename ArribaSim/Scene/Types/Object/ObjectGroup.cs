/*
 * ArribaSim is distributed under the terms of the
 * GNU General Public License v2 
 * with the following clarification and special exception.
 * 
 * Linking this code statically or dynamically with other modules is
 * making a combined work based on this code. Thus, the terms and
 * conditions of the GNU General Public License cover the whole
 * combination.
 * 
 * As a special exception, the copyright holders of this code give you
 * permission to link this code with independent modules to produce an
 * executable, regardless of the license terms of these independent
 * modules, and to copy and distribute the resulting executable under
 * terms of your choice, provided that you also meet, for each linked
 * independent module, the terms and conditions of the license of that
 * module. An independent module is a module which is not derived from
 * or based on this code. If you modify this code, you may extend
 * this exception to your version of the code, but you are not
 * obligated to do so. If you do not wish to do so, delete this
 * exception statement from your version.
 * 
 * License text is derived from GNU classpath text
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThreadedClasses;
using ArribaSim.Types;

namespace ArribaSim.Scene.Types.Object
{
    public class ObjectGroup : RwLockedSortedDoubleDictionary<int, UUID, ObjectPart>, IObject
    {
        #region Events
        public event Action<ObjectGroup> OnUpdate;
        #endregion

        public const int LINK_SET = -1;
        public const int LINK_ALL_OTHERS = -2;
        public const int LINK_ALL_CHILDREN = -3;
        public const int LINK_THIS = -4;
        public const int LINK_ROOT = 1;

        private bool m_IsTempOnRez = false;
        private bool m_IsPhysics = false;
        private bool m_IsPhantom = false;
        private Vector3 m_Velocity = Vector3.Zero;
        private UUID m_GroupID = UUID.Zero;
        private UUI m_Owner = UUI.Unknown;
        private UUI m_Creator = UUI.Unknown;

        #region Constructor
        public ObjectGroup()
        {

        }
        #endregion

        #region Properties
        public bool IsTempOnRez
        {
            get
            {
                return m_IsTempOnRez;
            }
            set
            {
                m_IsTempOnRez = value;
                OnUpdate(this);
            }
        }

        public bool IsPhantom
        {
            get
            {
                return m_IsPhantom;
            }
            set
            {
                m_IsPhantom = value;
                OnUpdate(this);
            }
        }

        public bool IsPhysics
        {
            get
            {
                return m_IsPhysics;
            }
            set
            {
                m_IsPhysics = value;
                OnUpdate(this);
            }
        }

        public UUID GroupID
        {
            get
            {
                lock (this)
                {
                    return new UUID(m_GroupID);
                }
            }
            set
            {
                lock (this)
                {
                    m_GroupID = new UUID(value);
                }
                OnUpdate(this);
            }
        }

        public UUI Owner
        {
            get
            {
                lock (this)
                {
                    return new UUI(m_Owner);
                }
            }
            set
            {
                lock (this)
                {
                    m_Owner = value;
                }
                OnUpdate(this);
            }
        }

        public UUI Creator
        {
            get
            {
                lock (this)
                {
                    return new UUI(m_Creator);
                }
            }
            set
            {
                lock (this)
                {
                    m_Creator = value;
                }
                OnUpdate(this);
            }
        }

        public ObjectPart RootPart
        {
            get
            {
                return this[LINK_ROOT]; /* we always count from one here */
            }
        }

        public Vector3 Velocity
        {
            get
            {
                lock (this)
                {
                    return m_Velocity;
                }
            }
            set
            {
                lock (this)
                {
                    m_Velocity = value;
                }
                OnUpdate.Invoke(this);
            }
        }

        public UUID ID
        {
            get { return RootPart.ID; }
        }

        public string Name
        {
            get { return RootPart.Name; }
            set { RootPart.Name = value; }
        }

        public string Description
        {
            get { return RootPart.Description; }
            set { RootPart.Description = value; }
        }

        public Vector3 Position
        {
            get { return RootPart.Position; }
            set { RootPart.Position = value; }
        }

        public Vector3 GlobalPosition
        {
            get { return RootPart.GlobalPosition; }
            set { RootPart.GlobalPosition = value; }
        }

        public Vector3 LocalPosition
        {
            get { return RootPart.LocalPosition; }
            set { RootPart.LocalPosition = value; }
        }
        #endregion

        #region Primitive Params Methods
        public void GetPrimitiveParams(int linkTarget, AnArray.Enumerator enumerator, ref AnArray paramList)
        {
            if(0 == linkTarget)
            {
                linkTarget = LINK_ROOT;
            }
            else if (linkTarget < LINK_ROOT)
            {
                throw new ArgumentException(String.Format("Invalid link target parameter for SetPrimitiveParams: {0}", linkTarget));
            }

            int linkThis = linkTarget;

            while (enumerator.MoveNext())
            {
                switch(ParamsHelper.GetPrimParamType(enumerator))
                { 
                    case PrimitiveParamsType.LinkTarget:
                        linkTarget = ParamsHelper.GetInteger(enumerator, "PRIM_LINK_TARGET");
                        if(0 == linkTarget)
                        {
                            linkTarget = LINK_ROOT;
                        }
                        else if(LINK_THIS == linkTarget)
                        {
                            linkTarget = linkThis;
                        }
                        break;

                    case PrimitiveParamsType.Physics:
                        paramList.Add(IsPhysics);
                        break;

                    case PrimitiveParamsType.TempOnRez:
                        paramList.Add(IsTempOnRez);
                        break;

                    case PrimitiveParamsType.Phantom:
                        paramList.Add(IsPhantom);
                        break;

                    default:
                        switch (linkTarget)
                        {
                            case LINK_SET:
                                throw new ArgumentException("Link Target LINK_SET not allowed for GetPrimitiveParams");

                            case LINK_ALL_OTHERS:
                                throw new ArgumentException("Link Target LINK_ALL_OTHERS not allowed for GetPrimitiveParams");

                            case LINK_ALL_CHILDREN:
                                throw new ArgumentException("Link Target LINK_ALL_CHILDREN not allowed for GetPrimitiveParams");

                            default:
                                if(linkTarget < 1)
                                {
                                    throw new ArgumentException(String.Format("Invalid link target {0}", linkTarget));
                                }
                                try
                                {
                                    ObjectPart obj = this[linkTarget];
                                    obj.GetPrimitiveParams(enumerator, ref paramList);
                                }
                                catch(KeyNotFoundException)
                                {
                                    throw new ArgumentException(String.Format("Link target {0} does not exist", linkTarget));
                                }
                                break;
                        }
                        break;
                }
            }
        }

        public void GetPrimitiveParams(AnArray.Enumerator enumerator, ref AnArray paramList)
        {
            GetPrimitiveParams(LINK_ROOT, enumerator, ref paramList);
        }

        public void SetPrimitiveParams(int linkTarget, AnArray.MarkEnumerator enumerator)
        {
            if (0 == linkTarget)
            {
                linkTarget = LINK_ROOT;
            }
            else if(linkTarget < LINK_ROOT)
            {
                throw new ArgumentException(String.Format("Invalid link target parameter for SetPrimitiveParams: {0}", linkTarget));
            }

            int linkThis = linkTarget;

            while (enumerator.MoveNext())
            {
                switch (ParamsHelper.GetPrimParamType(enumerator))
                {
                    case PrimitiveParamsType.LinkTarget:
                        if (!enumerator.MoveNext())
                        {
                            return;
                        }
                        linkTarget = ParamsHelper.GetInteger(enumerator, "PRIM_LINK_TARGET");
                        if (0 == linkTarget)
                        {
                            linkTarget = LINK_ROOT;
                        }
                        else if(LINK_THIS == linkTarget)
                        {
                            linkTarget = linkThis;
                        }
                        break;

                    case PrimitiveParamsType.Physics:
                        IsPhysics = ParamsHelper.GetBoolean(enumerator, "PRIM_PHYSICS");
                        break;

                    case PrimitiveParamsType.TempOnRez:
                        IsTempOnRez = ParamsHelper.GetBoolean(enumerator, "PRIM_TEMP_ON_REZ");
                        break;

                    case PrimitiveParamsType.Phantom:
                        IsPhantom = ParamsHelper.GetBoolean(enumerator, "PRIM_PHANTOM");
                        break;

                    default:
                        switch (linkTarget)
                        {
                            case LINK_SET:
                                enumerator.MarkPosition();
                                foreach(ObjectPart obj in this.ValuesByKey1)
                                {
                                    enumerator.GoToMarkPosition();
                                    obj.SetPrimitiveParams(enumerator);
                                }
                                break;

                            case LINK_ALL_CHILDREN:
                                enumerator.MarkPosition();
                                ForEach(delegate(KeyValuePair<int, ObjectPart> kvp)
                                {
                                    if (kvp.Key != LINK_ROOT)
                                    {
                                        enumerator.GoToMarkPosition();
                                        kvp.Value.SetPrimitiveParams(enumerator);
                                    }
                                });
                                break;

                            case LINK_ALL_OTHERS:
                                enumerator.MarkPosition();
                                ForEach(delegate(KeyValuePair<int, ObjectPart> kvp)
                                {
                                    if (kvp.Key != linkThis)
                                    {
                                        enumerator.GoToMarkPosition();
                                        kvp.Value.SetPrimitiveParams(enumerator);
                                    }
                                });
                                break;

                            default:
                                if (linkTarget < 1)
                                {
                                    throw new ArgumentException(String.Format("Invalid link target {0}", linkTarget));
                                }
                                try
                                {
                                    ObjectPart obj = this[linkTarget];
                                    obj.SetPrimitiveParams(enumerator);
                                }
                                catch(KeyNotFoundException)
                                {
                                    throw new ArgumentException(String.Format("Link target {0} does not exist", linkTarget));
                                }
                                break;
                        }
                        break;
                }
            }
        }

        public void SetPrimitiveParams(AnArray.MarkEnumerator enumerator)
        {
            SetPrimitiveParams(LINK_ROOT, enumerator);
        }
        #endregion

        #region Object Details Methods
        public void GetObjectDetails(AnArray.Enumerator enumerator, ref AnArray paramList)
        {
            while(enumerator.MoveNext())
            {
                switch (ParamsHelper.GetObjectDetailsType(enumerator))
                {
                    case ObjectDetailsType.Name:
                        paramList.Add(Name);
                        break;

                    case ObjectDetailsType.Desc:
                        paramList.Add(Description);
                        break;

                    case ObjectDetailsType.Pos:
                        paramList.Add(Position);
                        break;
                        
                    case ObjectDetailsType.Rot:
                        paramList.Add(RootPart.Rotation);
                        break;

                    case ObjectDetailsType.Velocity:
                        paramList.Add(Velocity);
                        break;

                    case ObjectDetailsType.Owner:
                        paramList.Add(Owner.ID);
                        break;

                    case ObjectDetailsType.Group:
                        paramList.Add(GroupID);
                        break;

                    case ObjectDetailsType.Creator:
                        paramList.Add(Creator.ID);
                        break;
                    
                    case ObjectDetailsType.RunningScriptCount:
                        paramList.Add(0);
                        break;

                    case ObjectDetailsType.TotalScriptCount:
                        {
                            int n = 0;
                            foreach(ObjectPart obj in this.Values)
                            {
                                n += obj.Inventory.CountScripts();
                            }
                            paramList.Add(n);
                        }
                        break;

                    case ObjectDetailsType.ScriptMemory:
                        paramList.Add(0);
                        break;

                    case ObjectDetailsType.ScriptTime:
                        paramList.Add(0f);
                        break;

                    case ObjectDetailsType.PrimEquivalence:
                        paramList.Add(Count);
                        break;

                    case ObjectDetailsType.ServerCost:
                        paramList.Add(0f);
                        break;

                    case ObjectDetailsType.StreamingCost:
                        paramList.Add(0f);
                        break;

                    case ObjectDetailsType.PhysicsCost:
                        paramList.Add(0f);
                        break;

                    case ObjectDetailsType.CharacterTime:
                        paramList.Add(0);
                        break;

                    case ObjectDetailsType.Root:
                        paramList.Add(ID);
                        break;

                    case ObjectDetailsType.AttachedPoint:
                        paramList.Add(0);
                        break;

                    case ObjectDetailsType.PathfindingType:
                        paramList.Add(0);
                        break;

                    case ObjectDetailsType.Physics:
                        paramList.Add(IsPhysics);
                        break;

                    case ObjectDetailsType.Phantom:
                        paramList.Add(IsPhantom);
                        break;

                    case ObjectDetailsType.TempOnRez:
                        paramList.Add(IsTempOnRez);
                        break;

                    case ObjectDetailsType.RenderWeight:
                        paramList.Add(0);
                        break;

                    default:
                        throw new ArgumentException("Unknown Object Details Type");
                }
            }
        }
        #endregion
    }
}
