using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using SilverSim.LL.Messages;
using SilverSim.Types;
using SilverSim.Types.Primitive;

namespace SilverSim.Scene.Types.Object
{
    public class ObjectUpdateInfo
    {

        private bool m_Killed = false;
        public uint LocalID;
        public ObjectPart Part { get; private set; }
        private int m_SerialNumber;

        public ObjectUpdateInfo(ObjectPart part)
        {
            Part = part;
            LocalID = part.LocalID;
            m_SerialNumber = 0;
        }

        public void KillObject()
        {
            lock(this)
            {
                m_Killed = true;
            }
        }

        private int clampSignedTo(double val, double imin, double imax, int omin, int omax)
        {
            double range = val * (omax - omin) / (imax - imin);
            range += omin;
            return (int)range;
        }

        private uint clampUnsignedTo(double val, double imin, double imax, uint omin, uint omax)
        {
            double range = val * (omax - omin) / (imax - imin);
            range += omin;
            return (uint)range;
        }

        public SilverSim.LL.Messages.Object.ObjectUpdate.ObjData SerializeFull()
        {
            lock(this)
            {
                if(m_Killed)
                {
                    return null;
                }
                else
                {
                    SilverSim.LL.Messages.Object.ObjectUpdate.ObjData m = new LL.Messages.Object.ObjectUpdate.ObjData();
                    m.ClickAction = Part.ClickAction;
                    m.CRC = (uint)m_SerialNumber;
                    m.ExtraParams = Part.ExtraParamsBytes;
                    m.FullID = Part.ID;
                    m.JointAxisOrAnchor = Vector3.Zero;
                    m.JointPivot = Vector3.Zero;
                    m.JointType = 0;
                    m.LocalID = Part.LocalID;
                    m.Material = Part.Material;
                    m.MediaURL = Part.MediaURL;
                    if (Part.ObjectGroup.IsAttached)
                    {
                        m.NameValue = string.Format("AttachItemID STRING RW SV {0}", Part.ObjectGroup.FromItemID);
                        m.State = (byte)(((byte)Part.ObjectGroup.AttachPoint % 16) * 16 + (((byte)Part.ObjectGroup.AttachPoint / 16)));
                    }
                    else
                    {
                        m.NameValue = string.Empty;
                        m.State = Part.ObjectGroup.RootPart.Shape.State;
                    }
                    m.ObjectData = new byte[60];
                    Part.Position.ToBytes(m.ObjectData, 0);
                    Part.Velocity.ToBytes(m.ObjectData, 12);
                    Part.Acceleration.ToBytes(m.ObjectData, 24);
                    Part.Rotation.ToBytes(m.ObjectData, 36);
                    Part.AngularVelocity.ToBytes(m.ObjectData, 48);
                    if(Part.ObjectGroup.RootPart != Part)
                    {
                        m.ParentID = Part.ObjectGroup.RootPart.LocalID;
                    }
                    else if (Part.ObjectGroup.IsAttached)
                    {
                        /* we need the owner localid here */
                        try
                        {
                            m.ParentID = Part.ObjectGroup.Scene.Agents[Part.Owner.ID].LocalID;
                        }
                        catch
                        {
                            m.ParentID = 0;
                        }
                    }
                    else
                    {
                        m.ParentID = 0;
                    }
                    ObjectPart.PrimitiveShape shape = Part.Shape;
                    m.PathBegin = shape.PathBegin;
                    m.PathEnd = shape.PathEnd;
                    m.PathCurve = shape.PathCurve;
                    m.PathRadiusOffset = shape.PathRadiusOffset;
                    m.PathRevolutions = shape.PathRevolutions;
                    m.PathScaleX = shape.PathScaleX;
                    m.PathScaleY = shape.PathScaleY;
                    m.PathShearX = shape.PathShearX;
                    m.PathShearY = shape.PathShearY;
                    m.PathSkew = shape.PathSkew;
                    m.PathTaperX = shape.PathTaperX;
                    m.PathTaperY = shape.PathTaperY;
                    m.PathTwist = shape.PathTwist;
                    m.PathTwistBegin = shape.PathTwistBegin;
                    m.PCode = shape.PCode;
                    m.ProfileBegin = shape.ProfileBegin;
                    m.ProfileCurve = shape.ProfileCurve;
                    m.ProfileEnd = shape.ProfileEnd;
                    m.ProfileHollow = shape.ProfileHollow;
                    m.PSBlock = Part.ParticleSystemBytes;
                    m.Scale = Part.Size;
                    ObjectPart.TextParam textparam = Part.Text;
                    m.Text = textparam.Text;
                    m.TextColor = textparam.TextColor;
                    m.TextureAnim = Part.TextureAnimationBytes;
                    m.TextureEntry = Part.TextureEntryBytes;
                    m.UpdateFlags = 0;
                    switch(m.PCode)
                    {
                        case PrimitiveCode.Grass:
                        case PrimitiveCode.Tree:
                        case PrimitiveCode.NewTree:
                            m.Data = new byte[] { Part.Shape.State };
                            break;

                        default:
                            m.Data = new byte[0];
                            break;
                    }

                    if(Part.IsAllowedDrop)
                    {
                        m.UpdateFlags |= PrimitiveFlags.AllowInventoryDrop;
                    }
                    if(Part.Inventory.Count == 0)
                    {
                        m.UpdateFlags |= PrimitiveFlags.InventoryEmpty;
                    }
                    if(Part.ObjectGroup.IsPhysics)
                    {
                        m.UpdateFlags |= PrimitiveFlags.Physics;
                    }
                    if(Part.Inventory.CountScripts != 0)
                    {
                        m.UpdateFlags |= PrimitiveFlags.Scripted;
                    }
                    if (Part.ObjectGroup.IsGroupOwned)
                    {
                        m.UpdateFlags |= PrimitiveFlags.ObjectGroupOwned;
                    }
                    if(Part.ObjectGroup.IsTemporary)
                    {
                        m.UpdateFlags |= PrimitiveFlags.Temporary;
                    }
                    if (Part.ObjectGroup.IsTempOnRez)
                    {
                        m.UpdateFlags |= PrimitiveFlags.TemporaryOnRez;
                    }
                    m.UpdateFlags |= Part.Flags;

                    ObjectPart.SoundParam soundparam = Part.Sound;
                    if (soundparam.SoundID != UUID.Zero)
                    {
                        m.LoopedSound = UUID.Zero;
                        m.OwnerID = Part.Owner.ID;
                        m.Gain = soundparam.Gain;
                        m.Radius = soundparam.Radius;
                        m.Flags = soundparam.Flags;
                    }
                    else
                    {
                        m.LoopedSound = UUID.Zero;
                        m.OwnerID = Part.Owner.ID;
                        m.Gain = 0;
                        m.Radius = 0;
                        m.Flags = 0;
                    }

                    switch (shape.PCode)
                    {
                        case PrimitiveCode.Grass:
                        case PrimitiveCode.Tree:
                        case PrimitiveCode.NewTree:
                            m.Data = new byte[] { shape.State };
                            break;
                        default:
                            m.Data = new byte[0];
                            break;
                    }

                    if(Part.Shape.SculptType == SilverSim.Types.Primitive.PrimitiveSculptType.Mesh)
                    {
                        m.ProfileBegin = 12500;
                        m.ProfileEnd = 0;
                        m.ProfileHollow = 27500;
                    }

                    return m;
                }
            }
        }

        public SilverSim.LL.Messages.Object.ImprovedTerseObjectUpdate.ObjData SerializeTerse()
        {
            lock(this)
            {
                if(m_Killed)
                {
                    return null;
                }
                else
                {
                    SilverSim.LL.Messages.Object.ImprovedTerseObjectUpdate.ObjData objdata = new LL.Messages.Object.ImprovedTerseObjectUpdate.ObjData();
                    objdata.Data = Part.TerseData;
                    objdata.TextureEntry = Part.TextureEntryBytes;
                    return objdata;
                }
            }
        }

        public SilverSim.LL.Messages.Object.ObjectProperties.ObjData SerializeObjProperties()
        {
            lock(this)
            {
                if(m_Killed)
                {
                    return null;
                }
                else
                {
                    SilverSim.LL.Messages.Object.ObjectProperties.ObjData objdata = new LL.Messages.Object.ObjectProperties.ObjData();

                    objdata.ObjectID = Part.ID;
                    objdata.CreatorID = Part.Creator.ID;
                    if (Part.ObjectGroup.IsGroupOwned)
                    {
                        objdata.OwnerID = UUID.Zero;
                    }
                    else
                    {
                        objdata.OwnerID = Part.Owner.ID;
                    }
                    objdata.GroupID = Part.ObjectGroup.Group.ID;
                    objdata.CreationDate = Part.CreationDate.AsULong * 1000000;
                    objdata.BaseMask = Part.BaseMask;
                    objdata.OwnerMask = Part.OwnerMask;
                    objdata.GroupMask = Part.GroupMask;
                    objdata.EveryoneMask = Part.EveryoneMask;
                    objdata.NextOwnerMask = Part.NextOwnerMask;
                    objdata.OwnershipCost = Part.ObjectGroup.OwnershipCost;
                    objdata.TaxRate = 0;
                    objdata.SaleType = Part.ObjectGroup.SaleType;
                    objdata.SalePrice = Part.ObjectGroup.SalePrice;
                    objdata.AggregatePerms = 0;
                    objdata.AggregatePermTextures = 0;
                    objdata.AggregatePermTexturesOwner = 0;
                    objdata.Category = Part.ObjectGroup.Category;
                    objdata.InventorySerial = (Int16)Part.Inventory.InventorySerial;
                    objdata.ItemID = Part.ObjectGroup.FromItemID;
                    objdata.FolderID = UUID.Zero;
                    objdata.FromTaskID = UUID.Zero;
                    objdata.LastOwnerID = Part.ObjectGroup.LastOwner.ID;
                    objdata.Name = Part.Name;
                    objdata.Description = Part.Description;
                    objdata.TouchName = Part.TouchText;
                    objdata.SitName = Part.SitText;
                    return objdata;
                }
            }
        }

        public bool IsKilled
        {
            get
            {
                return m_Killed;
            }
        }

        public bool IsPhysics
        {
            get
            {
                lock(this)
                {
                    if(Part != null && !m_Killed)
                    {
                        return Part.ObjectGroup.IsPhysics;
                    }
                }
                return false;
            }
        }

        public int SerialNumber
        {
            get
            {
                return m_SerialNumber;
            }
        }

        public void IncSerialNumber()
        {
            Interlocked.Increment(ref m_SerialNumber);
        }
    }
}
