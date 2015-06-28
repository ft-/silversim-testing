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
        public ObjectUpdateInfo(ObjectPart part)
        {
            Part = part;
            LocalID = part.LocalID;
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

        public byte[] FullUpdate
        {
            get
            {
                lock(this)
                {
                    if(Part != null && !m_Killed)
                    {
                        return Part.FullUpdateData;
                    }
                    return null;
                }
            }
        }

        public byte[] TerseUpdate
        {
            get
            {
                lock (this)
                {
                    if (Part != null && !m_Killed)
                    {
                        return Part.TerseUpdateData;
                    }
                    return null;
                }
            }
        }

        public byte[] PropertiesUpdate
        {
            get
            {
                lock (this)
                {
                    if (Part != null && !m_Killed)
                    {
                        return Part.PropertiesUpdateData;
                    }
                    return null;
                }
            }
        }

        public int SerialNumber
        {
            get
            {
                return Part.SerialNumber;
            }
        }
    }
}
