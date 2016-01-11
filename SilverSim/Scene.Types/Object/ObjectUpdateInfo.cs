// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using SilverSim.Viewer.Messages;
using SilverSim.Types;
using SilverSim.Types.Primitive;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Types.Object
{
    [SuppressMessage("Gendarme.Rules.Concurrency", "DoNotLockOnThisOrTypesRule")]
    public class ObjectUpdateInfo
    {

        private bool m_Killed;
        public uint LocalID;
        public ObjectPart Part { get; private set; }

        public ObjectUpdateInfo(ObjectPart part)
        {
            Part = part;
            LocalID = part.LocalID;
        }

        public void KillObject()
        {
            m_Killed = true;
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
                if(Part != null && !m_Killed)
                {
                    return Part.ObjectGroup.IsPhysics;
                }
                return false;
            }
        }

        public byte[] FullUpdate
        {
            get
            {
                if(Part != null && !m_Killed)
                {
                    return Part.FullUpdateData;
                }
                return null;
            }
        }

        public byte[] TerseUpdate
        {
            get
            {
                if (Part != null && !m_Killed)
                {
                    return Part.TerseUpdateData;
                }
                return null;
            }
        }

        public byte[] PropertiesUpdate
        {
            get
            {
                if (Part != null && !m_Killed)
                {
                    return Part.PropertiesUpdateData;
                }
                return null;
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
