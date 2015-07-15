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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SilverSim.Types.Parcel
{
    [Flags]
    public enum ObjectReturnType : uint
    {
        None = 0,
        Owner = 1 << 1,
        Group = 1 << 2,
        Other = 1 << 3,
        List = 1 << 4,
        Sell = 1 << 5
    }

    public enum ParcelAccessFlags : uint
    {
        NoAccess = 0,
        Access = 1
    }

    public enum TeleportLandingType : byte
    {
        None = 0,
        LandingPoint = 1,
        Direct = 2
    }

    [Flags]
    public enum ParcelFlags : uint
    {
        None = 0,
        AllowFly = 1 << 0,
        AllowOtherScripts = 1 << 1,
        ForSale = 1 << 2,
        AllowLandmark = 1 << 3,
        AllowTerraform = 1 << 4,
        AllowDamage = 1 << 5,
        CreateObjects = 1 << 6,
        ForSaleObjects = 1 << 7,
        UseAccessGroup = 1 << 8,
        UseAccessList = 1 << 9,
        UseBanList = 1 << 10,
        UsePassList = 1 << 11,
        ShowDirectory = 1 << 12,
        AllowDeedToGroup = 1 << 13,
        ContributeWithDeed = 1 << 14,
        SoundLocal = 1 << 15,
        SellParcelObjects = 1 << 16,
        AllowPublish = 1 << 17,
        MaturePublish = 1 << 18,
        UrlWebPage = 1 << 19,
        UrlRawHtml = 1 << 20,
        RestrictPushObject = 1 << 21,
        DenyAnonymous = 1 << 22,
        LindenHome = 1 << 23,
        AllowGroupScripts = 1 << 25,
        CreateGroupObjects = 1 << 26,
        AllowAPrimitiveEntry = 1 << 27,
        AllowGroupObjectEntry = 1 << 28,
        AllowVoiceChat = 1 << 29,
        UseEstateVoiceChan = 1 << 30,
        DenyAgeUnverified = (uint)1 << 31
    }

    public enum ParcelStatus : byte
    {
        None = 0xFF,
        Leased = 0,
        LeasePending = 1,
        Abandoned = 2
    }

    public enum ParcelCategory : byte
    {
        None = 0,
        LL,
        Adult,
        Arts,
        Business,
        Educational,
        Gaming,
        Hangout,
        Newcomer,
        Park,
        Residential,
        Shopping,
        Stage,
        Other,
        Any = 0xFF
    }

    public class ParcelInfo
    {
        public int Area = 0;
        public uint AuctionID = 0;
        public UUI AuthBuyer = new UUI();
        public ParcelCategory Category = ParcelCategory.None;
        public Date ClaimDate = new Date();
        public int ClaimPrice = 0;
        public UUID ID = UUID.Random;
        public UGI Group = UGI.Unknown;
        public bool GroupOwned = false;
        public string Description = string.Empty;
        public ParcelFlags Flags = ParcelFlags.AllowFly |
                            ParcelFlags.AllowLandmark |
                            ParcelFlags.AllowAPrimitiveEntry |
                            ParcelFlags.AllowDeedToGroup |
                            ParcelFlags.AllowTerraform |
                            ParcelFlags.CreateObjects |
                            ParcelFlags.AllowOtherScripts |
                            ParcelFlags.SoundLocal |
                            ParcelFlags.AllowVoiceChat;
        public TeleportLandingType LandingType = TeleportLandingType.None;
        public Vector3 LandingPosition = Vector3.Zero;
        public Vector3 LandingLookAt = Vector3.Zero;
        public string Name = string.Empty;
        public ParcelStatus Status = ParcelStatus.Leased;
        public int LocalID = 0;
        public URI MusicURI = null;
        public URI MediaURI = null;
        public UUID MediaID;
        public UUI Owner = new UUI();
        public UUID SnapshotID = UUID.Zero;
        public Int32 SalePrice;
        public Int32 OtherCleanTime;
        public bool MediaAutoScale;
        public Int32 RentPrice = 0;
        public Vector3 AABBMin;
        public Vector3 AABBMax;
        public double ParcelPrimBonus;
        public Int32 PassPrice;
        public double PassHours;
        public Int32 ActualArea;
        public Int32 BillableArea;
        public double Dwell;

        internal byte[,] m_LandBitmap;
        internal ReaderWriterLock m_LandBitmapRwLock = new ReaderWriterLock();
        /* Bitmap is per 4m * 4m */
        internal int m_BitmapWidth;
        internal int m_BitmapHeight;

        public class ParcelDataLandBitmap
        {
            byte[,] m_LandBitmap;
            int m_BitmapWidth;
            int m_BitmapHeight;
            ReaderWriterLock m_LandBitmapRwLock;
            ParcelInfo m_ParcelInfo;

            public int BitmapWidth
            {
                get
                {
                    return m_BitmapWidth;
                }
            }

            public int BitmapHeight
            {
                get
                {
                    return m_BitmapHeight;
                }
            }

            public ParcelDataLandBitmap(byte[,] landBitmap, int bitmapWidth, int bitmapHeight, ReaderWriterLock landBitmapRwLock, ParcelInfo parcelInfo)
            {
                m_LandBitmap = landBitmap;
                m_BitmapWidth = bitmapWidth;
                m_BitmapHeight = bitmapHeight;
                m_LandBitmapRwLock = landBitmapRwLock;
                m_ParcelInfo = parcelInfo;
            }

            public byte[] Data
            {
                get
                {
                    m_LandBitmapRwLock.AcquireReaderLock(-1);
                    try
                    {
                        byte[] b = new byte[m_LandBitmap.Length];
                        Buffer.BlockCopy(m_LandBitmap, 0, b, 0, m_LandBitmap.Length);
                        return b;
                    }
                    finally
                    {
                        m_LandBitmapRwLock.ReleaseReaderLock();
                    }
                }

                set
                {
                    try
                    {
                        m_LandBitmapRwLock.AcquireWriterLock(-1);
                        if (value.Length == m_LandBitmap.Length)
                        {
                            Buffer.BlockCopy(value, 0, m_LandBitmap, 0, m_LandBitmap.Length);
                            DetermineAABB();
                        }
                        else
                        {
                            throw new ArgumentException("Parcel Bitmap size does not match");
                        }
                    }
                    finally
                    {
                        m_LandBitmapRwLock.ReleaseWriterLock();
                    }
                }
            }

            public void SetAllBits()
            {
                for(int x = 0; x < m_BitmapWidth / 8; ++x)
                {
                    for(int y = 0; y < m_BitmapHeight; ++y)
                    {
                        m_LandBitmap[y, x] = 0xFF;
                    }
                }
            }

            public bool this[int x, int y]
            {
                get
                {
                    if (x < m_BitmapWidth && y < m_BitmapHeight)
                    {
                        return 0 != (m_LandBitmap[y, x / m_BitmapWidth] & (1 << (x % 8)));
                    }
                    else
                    {
                        throw new KeyNotFoundException();
                    }
                }
                set
                {
                    if (x < m_BitmapWidth && y < m_BitmapHeight)
                    {
                        m_LandBitmapRwLock.AcquireWriterLock(-1);
                        try
                        {
                            byte b = m_LandBitmap[y, x / m_BitmapWidth];
                            if (value)
                            {
                                b |= (byte)(1 << (x % 8));
                            }
                            else
                            {
                                b &= (byte)(~(1 << (x % 8)));
                            }
                            m_LandBitmap[y, x / m_BitmapWidth] = b;
                            DetermineAABB();
                        }
                        finally
                        {
                            m_LandBitmapRwLock.ReleaseWriterLock();
                        }
                    }
                    else
                    {
                        throw new KeyNotFoundException();
                    }
                }
            }

            public bool this[int x, int y, bool runaabb]
            {
                get
                {
                    if (x < m_BitmapWidth && y < m_BitmapHeight)
                    {
                        return 0 != (m_LandBitmap[y, x / m_BitmapWidth] & (1 << (x % 8)));
                    }
                    else
                    {
                        throw new KeyNotFoundException();
                    }
                }
                set
                {
                    if (x < m_BitmapWidth && y < m_BitmapHeight)
                    {
                        m_LandBitmapRwLock.AcquireWriterLock(-1);
                        try
                        {
                            byte b = m_LandBitmap[y, x / m_BitmapWidth];
                            if (value)
                            {
                                b |= (byte)(1 << (x % 8));
                            }
                            else
                            {
                                b &= (byte)(~(1 << (x % 8)));
                            }
                            m_LandBitmap[y, x / m_BitmapWidth] = b;
                            if (runaabb)
                            {
                                DetermineAABB();
                            }
                        }
                        finally
                        {
                            m_LandBitmapRwLock.ReleaseWriterLock();
                        }
                    }
                    else
                    {
                        throw new KeyNotFoundException();
                    }
                }
            }

            static const int[] ParcelAreaFromByte = new int[256] 
            {
                /*        x0   x1   x2   x3   x4   x5   x6   x7   x8   x9   xA   xB   xC   xD   xE   xF */
                /* 0x */   0,   1,   1,   2,   1,   2,   2,   3,   1,   2,   2,   3,   2,   3,   3,   4,
                /* 1x */   1,   2,   2,   3,   2,   3,   3,   4,   2,   3,   3,   4,   3,   4,   4,   5,
                /* 2x */   1,   2,   2,   3,   2,   3,   3,   4,   2,   3,   3,   4,   3,   4,   4,   5,
                /* 3x */   2,   3,   3,   4,   3,   4,   4,   5,   3,   4,   4,   5,   4,   5,   5,   6,
                /* 4x */   1,   2,   2,   3,   2,   3,   3,   4,   2,   3,   3,   4,   3,   4,   4,   5,
                /* 5x */   2,   3,   3,   4,   3,   4,   4,   5,   3,   4,   4,   5,   4,   5,   5,   6,
                /* 6x */   2,   3,   3,   4,   3,   4,   4,   5,   3,   4,   4,   5,   4,   5,   5,   6,
                /* 7x */   3,   4,   4,   5,   4,   5,   5,   6,   4,   5,   5,   6,   5,   6,   6,   7,
                /* 8x */   1,   2,   2,   3,   2,   3,   3,   4,   2,   3,   3,   4,   3,   4,   4,   5,
                /* 9x */   2,   3,   3,   4,   3,   4,   4,   5,   3,   4,   4,   5,   4,   5,   5,   6,
                /* Ax */   2,   3,   3,   4,   3,   4,   4,   5,   3,   4,   4,   5,   4,   5,   5,   6,
                /* Bx */   3,   4,   4,   5,   4,   5,   5,   6,   4,   5,   5,   6,   5,   6,   6,   7,
                /* Cx */   2,   3,   3,   4,   3,   4,   4,   5,   3,   4,   4,   5,   4,   5,   5,   6,
                /* Dx */   3,   4,   4,   5,   4,   5,   5,   6,   4,   5,   5,   6,   5,   6,   6,   7,
                /* Ex */   3,   4,   4,   5,   4,   5,   5,   6,   4,   5,   5,   6,   5,   6,   6,   7,
                /* Fx */   4,   5,   5,   6,   5,   6,   6,   7,   5,   6,   6,   7,   6,   7,   7,   8
            };

            void DetermineAABB()
            {
                int aabbminy = m_BitmapHeight - 1;
                int aabbminx = m_BitmapWidth * 8 - 1;
                int aabbmaxy = 0;
                int aabbmaxx = 0;
                int parcelarea = 0;

                for (int y = 0; y < m_BitmapHeight; ++y)
                {
                    for (int x = 0; x < m_BitmapWidth / 8; ++x)
                    {
                        if (m_LandBitmap[y, x] != 0)
                        {
                            /* we calculate 8 bits at a time */
                            parcelarea += ParcelAreaFromByte[m_LandBitmap[y, x]];

                            /* AABB only checks a block of 8 X-pixels which has at least one bit set */
                            if (aabbmaxy < y)
                            {
                                aabbmaxy = y;
                            }
                            if (aabbminy > y)
                            {
                                aabbminy = y;
                            }

                            int xx = x * 8;
                            for (int ofx = 0; ofx < 8; ++ofx, ++xx)
                            {
                                if ((m_LandBitmap[y, x] & (1 << ofx)) != 0)
                                {
                                    if (aabbminx > xx)
                                    {
                                        aabbminx = xx;
                                    }
                                    if (aabbmaxx < xx)
                                    {
                                        aabbmaxx = xx;
                                    }
                                }
                            }
                        }
                    }
                }

                m_ParcelInfo.AABBMin = new Vector3(aabbminx * 4, aabbminy * 4, 0);
                m_ParcelInfo.AABBMax = new Vector3(aabbmaxx * 4 + 3, aabbmaxy * 4 + 3, 0);
                m_ParcelInfo.Area = parcelarea;
            }
        }

        public ParcelDataLandBitmap LandBitmap { get; private set; }

        public ParcelInfo(int bitmapWidth, int bitmapHeight)
        {
            m_LandBitmap = new byte[bitmapHeight, bitmapWidth / 8];
            m_BitmapWidth = bitmapWidth;
            m_BitmapHeight = bitmapHeight;
            LandBitmap = new ParcelDataLandBitmap(m_LandBitmap, m_BitmapWidth, m_BitmapHeight, m_LandBitmapRwLock, this);
        }
    }
}
