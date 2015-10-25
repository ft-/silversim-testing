﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        public Quaternion CameraRotation
        {
            get
            {
                return Quaternion.Axes2Rot(CameraAtAxis, CameraLeftAxis, CameraUpAxis);
            }

            set
            {
                CameraAtAxis = value.FwdAxis;
                CameraLeftAxis = value.LeftAxis;
                CameraUpAxis = value.UpAxis;
            }
        }

        Vector3 m_CameraAtAxis;
        Vector3 m_CameraLeftAxis;
        Vector3 m_CameraUpAxis;
        Vector3 m_CameraPosition;

        public Vector3 CameraPosition
        {
            get
            {
                lock(this)
                {
                    return m_CameraPosition;
                }
            }
            set
            {
                lock(this)
                {
                    m_CameraPosition = value;
                }
            }
        }

        public Vector3 CameraLookAt
        {
            get
            {
                Vector3 atAxis = CameraAtAxis;
                if(atAxis == Vector3.Zero)
                {
                    return atAxis;
                }
                return new Vector3(atAxis.X, atAxis.Y, 0).Normalize();
            }
        }

        public Vector3 CameraAtAxis
        {
            get
            {
                lock(this)
                {
                    return m_CameraAtAxis;
                }
            }
            set
            {
                lock(this)
                {
                    m_CameraAtAxis = value;
                }
            }
        }

        public Vector3 CameraLeftAxis
        {
            get
            {
                lock(this)
                {
                    return m_CameraLeftAxis;
                }
            }
            set
            {
                lock(this)
                {
                    m_CameraLeftAxis = value;
                }
            }
        }

        public Vector3 CameraUpAxis
        {
            get
            {
                lock(this)
                {
                    return m_CameraUpAxis;
                }
            }
            set
            {
                lock(this)
                {
                    m_CameraUpAxis = value;
                }
            }
        }
    }
}
