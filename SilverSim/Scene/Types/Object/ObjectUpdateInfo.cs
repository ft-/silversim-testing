using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using SilverSim.LL.Messages;
using SilverSim.Types;

namespace SilverSim.Scene.Types.Object
{
    public class ObjectUpdateInfo
    {

        private bool m_Killed = false;
        public uint LocalID;
        private ObjectPart m_Part;
        private int m_SerialNumber;

        public ObjectUpdateInfo(ObjectPart part)
        {
            m_Part = part;
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
                    m.ClickAction = m_Part.ClickAction;
                    m.CRC = 1;
                    m.ExtraParams = m_Part.ExtraParamsBytes;
                    m.Flags = 0;
                    m.FullID = m_Part.ID;
                    m.Gain = 0;
                    m.JointAxisOrAnchor = Vector3.Zero;
                    m.JointPivot = Vector3.Zero;
                    m.JointType = 0;
                    m.LocalID = m_Part.LocalID;
                    m.LoopedSound = UUID.Zero;
                    m.Material = m_Part.Material;
                    m.MediaURL = "";
                    m.NameValue = m_Part.Name;
                    m.ObjectData = new byte[0];
                    m.OwnerID = m_Part.Owner.ID;
                    m.ParentID = m_Part.Group.RootPart.LocalID;
                    m.PathBegin = 0;
                    m.PathEnd = 0;
                    m.PathRadiusOffset = 0;
                    m.PathRevolutions = 0;
                    m.PathScaleX = 0;
                    m.PathScaleY = 0;
                    m.PathShearX = 0;
                    m.PathShearY = 0;
                    m.PathSkew = 0;
                    m.PathTaperX = 0;
                    m.PathTaperY = 0;
                    m.PathTwist = 0;
                    m.PathTwistBegin = 0;
                    m.PCode = 0;
                    m.ProfileBegin = 0;
                    m.ProfileCurve = 0;
                    m.ProfileEnd = 0;
                    m.ProfileHollow = 0;
                    m.PSBlock = m_Part.ParticleSystemBytes;
                    m.Radius = 0;
                    m.Scale = m_Part.Size;
                    m.State = 0;
                    ObjectPart.TextParam textparam = m_Part.Text;
                    m.Text = textparam.Text;
                    m.TextColor = textparam.TextColor;
                    m.TextureAnim = new byte[0];
                    m.TextureEntry = m_Part.TextureEntryBytes;
                    m.UpdateFlags = 0;

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
                    objdata.Data = m_Part.TerseData;
                    objdata.TextureEntry = m_Part.TextureEntryBytes;
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
