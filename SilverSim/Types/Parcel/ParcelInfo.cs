// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace SilverSim.Types.Parcel
{
    [Flags]
    [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
    [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    public enum ObjectReturnType : uint
    {
        None = 0,
        Owner = 1 << 1,
        Group = 1 << 2,
        Other = 1 << 3,
        List = 1 << 4,
        Sell = 1 << 5
    }

    [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
    [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    public enum ParcelAccessFlags : uint
    {
        NoAccess = 0,
        Access = 1
    }

    [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
    [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    public enum TeleportLandingType : byte
    {
        Blocked = 0,
        LandingPoint = 1,
        Anywhere = 2
    }

    [Flags]
    [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
    [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
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
        AllowGroupScripts = 1 << 25,
        CreateGroupObjects = 1 << 26,
        AllowAllPrimitiveEntry = 1 << 27,
        AllowGroupObjectEntry = 1 << 28,
        AllowVoiceChat = 1 << 29,
        UseEstateVoiceChan = 1 << 30,
        DenyAgeUnverified = (uint)1 << 31
    }

    [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
    public enum ParcelStatus : byte
    {
        None = 0xFF,
        Leased = 0,
        LeasePending = 1,
        Abandoned = 2
    }

    [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
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
        public int Area;
        public uint AuctionID;
        public UUI AuthBuyer = UUI.Unknown;
        public ParcelCategory Category;
        public Date ClaimDate = new Date();
        public int ClaimPrice;
        public UUID ID = UUID.Random;
        public UGI Group = UGI.Unknown;
        public bool GroupOwned;
        public string Description = string.Empty;
        public ParcelFlags Flags = ParcelFlags.AllowFly |
                            ParcelFlags.AllowLandmark |
                            ParcelFlags.AllowDeedToGroup |
                            ParcelFlags.AllowTerraform |
                            ParcelFlags.CreateGroupObjects |
                            ParcelFlags.AllowGroupScripts |
                            ParcelFlags.SoundLocal |
                            ParcelFlags.AllowVoiceChat;
        public TeleportLandingType LandingType;
        public Vector3 LandingPosition = Vector3.Zero;
        public Vector3 LandingLookAt = Vector3.Zero;
        public string Name = string.Empty;
        public ParcelStatus Status;
        public int LocalID;
        public URI MusicURI;
        public URI MediaURI;
        public UUID MediaID;
        public string MediaType = "none/none";
        public UUI Owner = new UUI();
        public UUID SnapshotID = UUID.Zero;
        public Int32 SalePrice;
        public Int32 OtherCleanTime;
        public bool MediaAutoScale;
        public int MediaWidth;
        public int MediaHeight;
        public bool MediaLoop;
        public string MediaDescription;
        public Int32 RentPrice;
        public Vector3 AABBMin;
        public Vector3 AABBMax;
        public double ParcelPrimBonus = 1;
        public Int32 PassPrice;
        public double PassHours;
        public Int32 ActualArea;
        public Int32 BillableArea;
        public double Dwell;
        public bool ObscureMedia;
        public bool ObscureMusic;
        public bool SeeAvatars = true;
        public bool AnyAvatarSounds = true;
        public bool GroupAvatarSounds = true;
        public bool IsPrivate;

        internal byte[,] m_LandBitmap;
        internal ReaderWriterLock m_LandBitmapRwLock = new ReaderWriterLock();
        /* Bitmap is per 4m * 4m */
        internal int m_BitmapWidth;
        internal int m_BitmapHeight;

        public class ParcelDataLandBitmap
        {
            readonly byte[,] m_LandBitmap;
            readonly int m_BitmapWidth;
            readonly int m_BitmapHeight;
            readonly ReaderWriterLock m_LandBitmapRwLock;
            readonly ParcelInfo m_ParcelInfo;

            public int BitmapWidth => m_BitmapWidth;

            public int BitmapHeight => m_BitmapHeight;

            public ParcelDataLandBitmap(byte[,] landBitmap, int bitmapWidth, int bitmapHeight, ReaderWriterLock landBitmapRwLock, ParcelInfo parcelInfo)
            {
                m_LandBitmap = landBitmap;
                m_BitmapWidth = bitmapWidth;
                m_BitmapHeight = bitmapHeight;
                m_LandBitmapRwLock = landBitmapRwLock;
                m_ParcelInfo = parcelInfo;
            }

            [SuppressMessage("Gendarme.Rules.Performance", "AvoidReturningArraysOnPropertiesRule")]
            /** <summary>Do not use this to merge with an active parcel data</summary> */
            public void Merge(ParcelDataLandBitmap bitmap)
            {
                try
                {
                    m_LandBitmapRwLock.AcquireWriterLock(-1);
                    if (bitmap.m_LandBitmap.Length == m_LandBitmap.Length)
                    {
                        for (int y = 0; y < m_BitmapHeight; ++y)
                        {
                            for (int x = 0; x < m_BitmapWidth; ++x)
                            {
                                m_LandBitmap[y, x] |= bitmap.m_LandBitmap[y, x];
                            }
                        }
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

            [SuppressMessage("Gendarme.Rules.Performance", "AvoidReturningArraysOnPropertiesRule")]
            public byte[] Data
            {
                get
                {
                    m_LandBitmapRwLock.AcquireReaderLock(-1);
                    try
                    {
                        var b = new byte[m_LandBitmap.Length];
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

            public byte[] DataNoAABBUpdate
            {
                get
                {
                    return Data;
                }
                set
                {
                    try
                    {
                        m_LandBitmapRwLock.AcquireWriterLock(-1);
                        if (value.Length == m_LandBitmap.Length)
                        {
                            Buffer.BlockCopy(value, 0, m_LandBitmap, 0, m_LandBitmap.Length);
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

            public bool ContainsLocation(Vector3 v)
            {
                int x = ((int)v.X) / 4;
                int y = ((int)v.Y) / 4;
                if (v.X < 0 || v.Y < 0 || x < 0 || y < 0 || x >= m_BitmapWidth || y >= m_BitmapHeight)
                {
                    return false;
                }
                return this[x / 4, y / 4];
            }

            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
            public bool this[int x, int y]
            {
                get
                {
                    return this[x, y, true];
                }
                set
                {
                    this[x, y, true] = value;
                }
            }

            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
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

            static readonly int[] ParcelAreaFromByte = new int[256] 
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

        public ParcelDataLandBitmap LandBitmap { get; }

        public ParcelInfo(int bitmapWidth, int bitmapHeight)
        {
            m_LandBitmap = new byte[bitmapHeight, bitmapWidth / 8];
            m_BitmapWidth = bitmapWidth;
            m_BitmapHeight = bitmapHeight;
            LandBitmap = new ParcelDataLandBitmap(m_LandBitmap, m_BitmapWidth, m_BitmapHeight, m_LandBitmapRwLock, this);
        }

        public ParcelInfo(ParcelInfo src)
        {
            m_LandBitmap = new byte[src.m_BitmapHeight, src.m_BitmapHeight / 8];
            m_BitmapWidth = src.m_BitmapWidth;
            m_BitmapHeight = src.m_BitmapHeight;
            LandBitmap = new ParcelDataLandBitmap(m_LandBitmap, m_BitmapWidth, m_BitmapHeight, m_LandBitmapRwLock, this);
            Area = src.Area;
            AuctionID = src.AuctionID;
            AuthBuyer = new UUI(src.AuthBuyer);
            Category = src.Category;
            ClaimDate = src.ClaimDate;
            ClaimPrice = src.ClaimPrice;
            ID = src.ID;
            Group = new UGI(src.Group);
            GroupOwned = src.GroupOwned;
            Description = src.Description;
            Flags = src.Flags;
            LandingType = src.LandingType;
            LandingPosition = src.LandingPosition;
            LandingLookAt = src.LandingLookAt;
            Name = src.Name;
            Status = src.Status;
            LocalID = src.LocalID;
            MusicURI = src.MusicURI;
            MediaURI = src.MediaURI;
            MediaID = src.MediaID;
            MediaType = src.MediaType;
            Owner = new UUI(src.Owner);
            SnapshotID = src.SnapshotID;
            SalePrice = src.SalePrice;
            OtherCleanTime = src.OtherCleanTime;
            MediaAutoScale = src.MediaAutoScale;
            MediaWidth = src.MediaWidth;
            MediaHeight = src.MediaHeight;
            MediaLoop = src.MediaLoop;
            MediaDescription = src.MediaDescription;
            RentPrice = src.RentPrice;
            AABBMin = src.AABBMin;
            AABBMax = src.AABBMax;
            ParcelPrimBonus = src.ParcelPrimBonus;
            PassPrice = src.PassPrice;
            PassHours = src.PassHours;
            ActualArea = src.ActualArea;
            BillableArea = src.BillableArea;
            Dwell = src.Dwell;
            ObscureMedia = src.ObscureMedia;
            ObscureMusic = src.ObscureMusic;
            SeeAvatars = src.SeeAvatars;
            AnyAvatarSounds = src.AnyAvatarSounds;
            GroupAvatarSounds = src.GroupAvatarSounds;
            IsPrivate = src.IsPrivate;
            LandBitmap.DataNoAABBUpdate = src.LandBitmap.Data;
        }
    }
}
