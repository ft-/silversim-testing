// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace SilverSim.Scene.Types.Agent
{
    class J2kEncoder
    {
        public J2kEncoder()
        {

        }
    }

    static class GetJ2KEncoder
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibrary(string dllToLoad);

        public static J2kEncoder GetEncoder()
        {
            if(Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                if(Environment.Is64BitProcess)
                {
                    LoadLibrary(Path.GetFullPath("platform-libs/windows/64/openjp2.dll"));
                }
                else
                {
                    LoadLibrary(Path.GetFullPath("platform-libs/windows/32/openjp2.dll"));
                }
            }

            return new J2kEncoder();
        }
    }

    public static class AgentBakeAppearance
    {
        private static readonly ILog m_BakeLog = LogManager.GetLogger("AVATAR BAKING");

        enum BakeType
        {
            Head,
            UpperBody,
            LowerBody,
            Eyes,
            Skirt,
            Hair
        }

        class OutfitItem
        {
            public InventoryItem ActualItem;
            public Wearable WearableData;

            public OutfitItem(InventoryItem linkItem)
            {
                ActualItem = linkItem;
            }
        }

        class BakeStatus : IDisposable
        {
            public readonly Dictionary<UUID, OutfitItem> OutfitItems = new Dictionary<UUID, OutfitItem>();
            public readonly Dictionary<UUID, Image> Textures = new Dictionary<UUID, Image>();
            public readonly Dictionary<UUID, Image> TexturesResized128 = new Dictionary<UUID, Image>();
            public readonly Dictionary<UUID, Image> TexturesResized512 = new Dictionary<UUID, Image>();
            public UUID Layer0TextureID = UUID.Zero;

            public BakeStatus()
            {

            }

            public bool TryGetTexture(BakeType bakeType, UUID textureID, out Image img)
            {
                int targetDimension;
                Dictionary<UUID, Image> resizeCache;
                if (bakeType == BakeType.Eyes)
                {
                    resizeCache = TexturesResized128;
                    targetDimension = 128;
                }
                else
                {
                    resizeCache = TexturesResized512;
                    targetDimension = 512;
                }

                /* do not redo the hard work of rescaling images unnecessarily */
                if (resizeCache.TryGetValue(textureID, out img))
                {
                    return true;
                }

                if (Textures.TryGetValue(textureID, out img))
                {
                    if (img.Width != targetDimension || img.Height != targetDimension)
                    {
                        img = new Bitmap(img, targetDimension, targetDimension);
                        resizeCache.Add(textureID, img);
                    }
                    return true;
                }

                img = null;
                return false;
            }

            public void Dispose()
            {
                foreach (Image img in Textures.Values)
                {
                    img.Dispose();
                }
                foreach (Image img in TexturesResized128.Values)
                {
                    img.Dispose();
                }
                foreach (Image img in TexturesResized512.Values)
                {
                    img.Dispose();
                }
            }
        }

        public class BakingErrorException : Exception
        {
            public BakingErrorException()
            {

            }

            public BakingErrorException(string message)
            : base(message)
            {

            }

            protected BakingErrorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
            {
            }

            public BakingErrorException(string message, Exception innerException)
            : base(message, innerException)
            {

            }
        }

        #region Visual Param Mapping
        public class VisualParamMap
        {
            public int ValueId { get; private set; }
            public double DefValue { get; private set; }
            public double MinValue { get; private set; }
            public double MaxValue { get; private set; }

            public VisualParamMap(int valueId, double defVal, double minVal, double maxVal, int[] othervals = null)
            {
                ValueId = valueId;
                DefValue = defVal;
                MinValue = minVal;
                MaxValue = maxVal;
            }
        }

        static readonly VisualParamMap[] m_VisualParamMapping = new VisualParamMap[]
        {
            new VisualParamMap(1, -0.3f, -0.3f, 2f),
            new VisualParamMap(2, -0.8f, -0.8f, 2.5f),
            new VisualParamMap(4, -0.5f, -0.5f, 1f),
            new VisualParamMap(5, -0.1f, -0.1f, 1f),
            new VisualParamMap(6, -0.3f, -0.3f, 1f),
            new VisualParamMap(7, -0.5f, -0.5f, 0.5f),
            new VisualParamMap(8, -0.5f, -0.5f, 1.5f),
            new VisualParamMap(10, -1.5f, -1.5f, 3f),
            new VisualParamMap(11, -0.5f, -0.5f, 1.5f),
            new VisualParamMap(12, -0.5f, -0.5f, 2.5f),
            new VisualParamMap(13, 0f, 0f, 1.5f),
            new VisualParamMap(14, -0.5f, -0.5f, 1f),
            new VisualParamMap(15, -0.5f, -0.5f, 1.5f),
            new VisualParamMap(16, -0.5f, -0.5f, 3f, new int[] { 870 }),
            new VisualParamMap(17, -0.5f, -0.5f, 1f),
            new VisualParamMap(18, -1.5f, -1.5f, 2.5f),
            new VisualParamMap(19, -1.5f, -1.5f, 1f),
            new VisualParamMap(20, -0.5f, -0.5f, 1.5f),
            new VisualParamMap(21, -0.2f, -0.2f, 1.3f),
            new VisualParamMap(22, 0f, 0f, 1f),
            new VisualParamMap(23, -0.5f, -0.5f, 1.5f),
            new VisualParamMap(24, -1.5f, -1.5f, 2f),
            new VisualParamMap(25, -0.8f, -0.8f, 1.5f),
            new VisualParamMap(27, -1.3f, -1.3f, 1.2f),
            new VisualParamMap(31, 0.5f, 0f, 2f, new int[] { 872 }),
            new VisualParamMap(33, -2.3f, -2.3f, 2f),
            new VisualParamMap(34, -0.7f, -0.7f, 1.5f),
            new VisualParamMap(35, -1f, -1f, 2f),
            new VisualParamMap(36, -0.5f, -1.8f, 1.4f),
            new VisualParamMap(37, -3.2f, -3.2f, 2.8f),
            new VisualParamMap(38, -1f, -1f, 1f),
            new VisualParamMap(80, 0f, 0f, 1f, new int[] { 32, 153, 40, 100, 857 }),
            new VisualParamMap(93, 0.8f, 0.01f, 1f, new int[] { 1058, 1059 }),
            new VisualParamMap(98, 0f, 0f, 1f),
            new VisualParamMap(99, 0f, 0f, 1f),
            new VisualParamMap(105, 0.5f, 0f, 1f, new int[] { 843, 627, 626 }),
            new VisualParamMap(108, 0f, 0f, 1f),
            new VisualParamMap(110, 0f, 0f, 0.1f),
            new VisualParamMap(111, 0.5f, 0f, 1f),
            new VisualParamMap(112, 0f, 0f, 1f),
            new VisualParamMap(113, 0f, 0f, 1f),
            new VisualParamMap(114, 0.5f, 0f, 1f),
            new VisualParamMap(115, 0f, 0f, 1f),
            new VisualParamMap(116, 0f, 0f, 1f),
            new VisualParamMap(117, 0f, 0f, 1f),
            new VisualParamMap(119, 0.5f, 0f, 1f, new int[] { 1000, 1001 }),
            new VisualParamMap(130, 0.45f, 0f, 1f, new int[] { 144, 145 }),
            new VisualParamMap(131, 0.5f, 0f, 1f, new int[] { 146, 147 }),
            new VisualParamMap(132, 0.39f, 0f, 1f, new int[] { 148, 149 }),
            new VisualParamMap(133, 0.25f, 0f, 1f, new int[] { 172, 171 }),
            new VisualParamMap(134, 0.5f, 0f, 1f, new int[] { 174, 173 }),
            new VisualParamMap(135, 0.55f, 0f, 1f, new int[] { 176, 175 }),
            new VisualParamMap(136, 0.5f, 0f, 1f, new int[] { 179, 178 }),
            new VisualParamMap(137, 0.5f, 0f, 1f, new int[] { 190, 191 }),
            new VisualParamMap(140, 0f, 0f, 2f),
            new VisualParamMap(141, 0f, 0f, 2f),
            new VisualParamMap(142, 0f, 0f, 2f),
            new VisualParamMap(143, 0.125f, -4f, 1.5f),
            new VisualParamMap(150, 0f, 0f, 1f, new int[] { 125, 126, 160, 161, 874, 878 }),
            new VisualParamMap(155, 0f, -0.9f, 1.3f, new int[] { 29, 30 }),
            new VisualParamMap(157, 0f, 0f, 1f, new int[] { 104, 156, 849 }),
            new VisualParamMap(162, 0f, 0f, 1f, new int[] { 158, 159, 873 }),
            new VisualParamMap(163, 0f, 0f, 1f, new int[] { 118 }),
            new VisualParamMap(165, 0f, 0f, 1f),
            new VisualParamMap(166, 0f, 0f, 1f, new int[] { 1004, 1005 }),
            new VisualParamMap(167, 0f, 0f, 1f, new int[] { 1006, 1007 }),
            new VisualParamMap(168, 0f, 0f, 1f, new int[] { 1008, 1009 }),
            new VisualParamMap(169, 0f, 0f, 1f, new int[] { 1010, 1011 }),
            new VisualParamMap(171, 0f, 0f, 1f),
            new VisualParamMap(177, 0f, 0f, 1f),
            new VisualParamMap(181, 0.14f, -1f, 1f),
            new VisualParamMap(182, 0.7f, -1f, 1f),
            new VisualParamMap(183, 0.05f, -1f, 1f),
            new VisualParamMap(184, 0f, 0f, 1f),
            new VisualParamMap(185, -1f, -1f, 1f),
            new VisualParamMap(192, 0f, 0f, 1f),
            new VisualParamMap(193, 0.5f, 0f, 1f, new int[] { 188, 642, 189, 643 }),
            new VisualParamMap(196, 0f, -2f, 1f, new int[] { 194, 195 }),
            new VisualParamMap(198, 0f, 0f, 1f, new int[] { 197, 500 }),
            new VisualParamMap(503, 0f, 0f, 1f, new int[] { 501, 502 }),
            new VisualParamMap(505, 0.5f, 0f, 1f, new int[] { 26, 28 }),
            new VisualParamMap(506, -2f, -2f, 2f),
            new VisualParamMap(507, 0f, -1.5f, 2f),
            new VisualParamMap(508, -1f, -1f, 2f),
            new VisualParamMap(513, 0.5f, 0f, 1f, new int[] { 509, 510 }),
            new VisualParamMap(514, 0.5f, 0f, 1f, new int[] { 511, 512 }),
            new VisualParamMap(515, -1f, -1f, 3f),
            new VisualParamMap(517, -0.5f, -0.5f, 1f),
            new VisualParamMap(518, -0.3f, -0.3f, 1.5f),
            new VisualParamMap(603, 0.4f, 0.01f, 1f, new int[] { 1042, 1043 }),
            new VisualParamMap(604, 0.85f, 0f, 1f, new int[] { 1044, 1045 }),
            new VisualParamMap(605, 0.84f, 0f, 1f, new int[] { 1046, 1047 }),
            new VisualParamMap(606, 0.8f, 0f, 1f, new int[] { 1019, 1039, 1020 }),
            new VisualParamMap(607, 0.8f, 0f, 1f, new int[] { 1021, 1040, 1022 }),
            new VisualParamMap(608, 0.8f, 0f, 1f, new int[] { 620, 1025, 1037, 621, 1027, 1033 }),
            new VisualParamMap(609, 0.2f, 0f, 1f, new int[] { 622, 1026, 1038, 623, 1028, 1034 }),
            new VisualParamMap(616, 0.1f, 0f, 1f, new int[] { 1052, 1053 }),
            new VisualParamMap(617, 0.35f, 0f, 1f, new int[] { 1050, 1051 }),
            new VisualParamMap(619, 0.3f, 0f, 1f, new int[] { 1054, 1055 }),
            new VisualParamMap(624, 0.8f, 0f, 1f, new int[] { 1056, 1057 }),
            new VisualParamMap(625, 0f, 0f, 1.5f),
            new VisualParamMap(629, 0.5f, 0f, 1f, new int[] { 630, 644, 631, 645 }),
            new VisualParamMap(637, 0f, 0f, 1f, new int[] { 633, 634, 635, 851 }),
            new VisualParamMap(638, 0f, 0f, 1.3f),
            new VisualParamMap(646, 0f, -1.3f, 1f, new int[] { 640, 186 }),
            new VisualParamMap(647, 0f, -0.5f, 1f, new int[] { 641, 187 }),
            new VisualParamMap(649, 0.5f, 0f, 1f, new int[] { 648, 106 }),
            new VisualParamMap(650, -1.3f, -1.3f, 1.2f),
            new VisualParamMap(652, 0.5f, 0f, 1f, new int[] { 651, 152 }),
            new VisualParamMap(653, -1f, -1f, 2f),
            new VisualParamMap(654, 0f, 0f, 2f),
            new VisualParamMap(656, -2f, -2f, 2f),
            new VisualParamMap(659, 0.5f, 0f, 1f, new int[] { 658, 657 }),
            new VisualParamMap(662, 0.5f, 0f, 1f, new int[] { 660, 661, 774 }),
            new VisualParamMap(663, 0f, -2f, 2f),
            new VisualParamMap(664, 0f, -1.3f, 1.3f),
            new VisualParamMap(665, 0f, -2f, 2f),
            new VisualParamMap(674, -0.3f, -1f, 2f),
            new VisualParamMap(675, -0.3f, -0.3f, 0.3f),
            new VisualParamMap(676, 0f, -1f, 2f, new int[] { 855, 856 }),
            new VisualParamMap(678, 0.5f, 0f, 1f, new int[] { 677, 106 }),
            new VisualParamMap(682, 0.5f, 0f, 1f, new int[] { 679, 694, 680, 681, 655 }),
            new VisualParamMap(683, -0.15f, -0.4f, 0.2f),
            new VisualParamMap(684, 0f, -0.3f, 1.3f),
            new VisualParamMap(685, 0f, -0.5f, 1.1f),
            new VisualParamMap(690, 0.5f, 0f, 1f, new int[] { 686, 687, 695, 688, 691, 689 }),
            new VisualParamMap(692, -1f, -1f, 1f),
            new VisualParamMap(693, 0.6f, -1f, 1f),
            new VisualParamMap(700, 0.25f, 0f, 1f),
            new VisualParamMap(701, 0f, 0f, 0.9f),
            new VisualParamMap(702, 0f, 0f, 1f),
            new VisualParamMap(703, 0f, 0f, 1f),
            new VisualParamMap(704, 0f, 0f, 0.9f),
            new VisualParamMap(705, 0.5f, 0f, 1f),
            new VisualParamMap(706, 0.6f, 0.2f, 1f),
            new VisualParamMap(707, 0f, 0f, 0.7f),
            new VisualParamMap(708, 0f, 0f, 1f),
            new VisualParamMap(709, 0f, 0f, 1f),
            new VisualParamMap(710, 0f, 0f, 1f),
            new VisualParamMap(711, 0.5f, 0f, 1f),
            new VisualParamMap(712, 0f, 0f, 1f),
            new VisualParamMap(713, 0.7f, 0.2f, 1f),
            new VisualParamMap(714, 0f, 0f, 1f),
            new VisualParamMap(715, 0f, 0f, 1f),
            new VisualParamMap(750, 0.7f, 0f, 1f),
            new VisualParamMap(752, 0.5f, 0f, 1f),
            new VisualParamMap(753, 0f, -0.5f, 3f),
            new VisualParamMap(754, 0f, -1f, 2f),
            new VisualParamMap(755, 0.05f, -1.5f, 1.5f),
            new VisualParamMap(756, 0f, -1f, 1f),
            new VisualParamMap(757, -1f, -4f, 2f, new int[] { 871 }),
            new VisualParamMap(758, -1.5f, -1.5f, 1.5f),
            new VisualParamMap(759, 0.5f, -1f, 1.5f),
            new VisualParamMap(760, 0f, -1.2f, 2f),
            new VisualParamMap(762, 0f, 0f, 3f),
            new VisualParamMap(763, 0.55f, 0f, 1f, new int[] { 761, 180 }),
            new VisualParamMap(764, -0.5f, -0.5f, 1.2f),
            new VisualParamMap(765, -0.3f, -0.3f, 2.5f),
            new VisualParamMap(769, 0.5f, 0f, 1f, new int[] { 767, 768 }),
            new VisualParamMap(773, 0.5f, 0f, 1f, new int[] { 770, 771, 772 }),
            new VisualParamMap(775, 0f, 0f, 1f, new int[] { 776, 777 }),
            new VisualParamMap(779, 0.84f, 0f, 1f, new int[] { 1048, 1049 }),
            new VisualParamMap(780, 0.8f, 0f, 1f, new int[] { 1023, 1041, 1024 }),
            new VisualParamMap(781, 0.78f, 0f, 1f, new int[] { 778, 1016, 1032, 903 }),
            new VisualParamMap(785, 0f, 0f, 1f, new int[] { 782, 783, 790, 784 }),
            new VisualParamMap(789, 0f, 0f, 1f, new int[] { 786, 787, 788 }),
            new VisualParamMap(795, 0.25f, 0f, 1f, new int[] { 867, 794, 151, 852 }),
            new VisualParamMap(796, -0.4f, -0.4f, 3f),
            new VisualParamMap(799, 0.5f, 0f, 1f, new int[] { 797, 798 }),
            new VisualParamMap(800, 0.89f, 0f, 1f, new int[] { 600, 1013, 1029, 900 }),
            new VisualParamMap(801, 1f, 0f, 1f, new int[] { 601, 1014, 1030, 901 }),
            new VisualParamMap(802, 0.78f, 0f, 1f, new int[] { 602, 1015, 1031, 902 }),
            new VisualParamMap(803, 1f, 0f, 1f),
            new VisualParamMap(804, 1f, 0f, 1f),
            new VisualParamMap(805, 1f, 0f, 1f),
            new VisualParamMap(806, 1f, 0f, 1f),
            new VisualParamMap(807, 1f, 0f, 1f),
            new VisualParamMap(808, 1f, 0f, 1f),
            new VisualParamMap(812, 1f, 0f, 1f),
            new VisualParamMap(813, 1f, 0f, 1f),
            new VisualParamMap(814, 1f, 0f, 1f),
            new VisualParamMap(815, 0.8f, 0f, 1f, new int[] { 615, 1018, 1036, 793, 915 }),
            new VisualParamMap(816, 0f, 0f, 1f, new int[] { 516, 913 }),
            new VisualParamMap(817, 1f, 0f, 1f),
            new VisualParamMap(818, 1f, 0f, 1f),
            new VisualParamMap(819, 1f, 0f, 1f),
            new VisualParamMap(820, 1f, 0f, 1f),
            new VisualParamMap(821, 1f, 0f, 1f),
            new VisualParamMap(822, 1f, 0f, 1f),
            new VisualParamMap(823, 1f, 0f, 1f),
            new VisualParamMap(824, 1f, 0f, 1f),
            new VisualParamMap(825, 1f, 0f, 1f),
            new VisualParamMap(826, 1f, 0f, 1f),
            new VisualParamMap(827, 1f, 0f, 1f),
            new VisualParamMap(828, 0f, 0f, 1f, new int[] { 628, 899 }),
            new VisualParamMap(829, 1f, 0f, 1f),
            new VisualParamMap(830, 1f, 0f, 1f),
            new VisualParamMap(834, 1f, 0f, 1f, new int[] { 809, 831 }),
            new VisualParamMap(835, 1f, 0f, 1f, new int[] { 810, 832 }),
            new VisualParamMap(836, 1f, 0f, 1f, new int[] { 811, 833 }),
            new VisualParamMap(840, 0f, 0f, 1.5f),
            new VisualParamMap(841, 0f, -1f, 1f, new int[] { 853, 847 }),
            new VisualParamMap(842, -1f, -1f, 1f),
            new VisualParamMap(844, 1f, 0.01f, 1f, new int[] { 1060, 1061 }),
            new VisualParamMap(848, 0.2f, 0f, 2f),
            new VisualParamMap(858, 0.4f, 0.01f, 1f),
            new VisualParamMap(859, 1f, 0f, 1f),
            new VisualParamMap(860, 1f, 0f, 1f),
            new VisualParamMap(861, 1f, 0f, 1f),
            new VisualParamMap(862, 1f, 0f, 1f),
            new VisualParamMap(863, 0.333f, 0f, 1f, new int[] { 866, 846, 845 }),
            new VisualParamMap(868, 0f, 0f, 1f),
            new VisualParamMap(869, 0f, 0f, 1f),
            new VisualParamMap(877, 0f, 0f, 1f, new int[] { 875, 876 }),
            new VisualParamMap(879, 0f, -0.5f, 2f),
            new VisualParamMap(880, -1.3f, -1.3f, 1.2f),
            new VisualParamMap(921, 1f, 0f, 1f),
            new VisualParamMap(922, 1f, 0f, 1f),
            new VisualParamMap(923, 1f, 0f, 1f),
            new VisualParamMap(10000, 0.1f, 0.1f, 1f),
            new VisualParamMap(10001, 0f, 0f, 30f),
            new VisualParamMap(10002, 1f, 0f, 10f),
            new VisualParamMap(10003, 0f, 0f, 3f),
            new VisualParamMap(10004, 10f, 0f, 100f),
            new VisualParamMap(10005, 10f, 1f, 100f),
            new VisualParamMap(10006, 0.2f, 0f, 1f),
            new VisualParamMap(10007, 0f, 0f, 3f),
            new VisualParamMap(10008, 10f, 0f, 100f),
            new VisualParamMap(10009, 10f, 1f, 100f),
            new VisualParamMap(10010, 0.2f, 0f, 1f),
            new VisualParamMap(10011, 0.1f, 0.1f, 1f),
            new VisualParamMap(10012, 0f, 0f, 30f),
            new VisualParamMap(10013, 1f, 0f, 10f),
            new VisualParamMap(10014, 0f, 0f, 3f),
            new VisualParamMap(10015, 10f, 0f, 100f),
            new VisualParamMap(10016, 10f, 1f, 100f),
            new VisualParamMap(10017, 0.2f, 0f, 1f),
            new VisualParamMap(10018, 0.1f, 0.1f, 1f),
            new VisualParamMap(10019, 0f, 0f, 30f),
            new VisualParamMap(10020, 1f, 0f, 10f),
            new VisualParamMap(10021, 0f, 0f, 3f),
            new VisualParamMap(10022, 10f, 0f, 100f),
            new VisualParamMap(10023, 10f, 1f, 100f),
            new VisualParamMap(10024, 0.2f, 0f, 1f),
            new VisualParamMap(10025, 0f, 0f, 3f),
            new VisualParamMap(10026, 10f, 0f, 100f),
            new VisualParamMap(10027, 10f, 1f, 100f),
            new VisualParamMap(10028, 0.2f, 0f, 1f),
            new VisualParamMap(10029, 0f, 0f, 3f),
            new VisualParamMap(10030, 10f, 0f, 100f),
            new VisualParamMap(10031, 10f, 1f, 100f),
            new VisualParamMap(10032, 0.2f, 0f, 1f)
        };
        #endregion

        #region Get Current Outfit
        static int LinkDescriptionToInt(string desc)
        {
            int res = 0;
            if(desc.StartsWith("@") && int.TryParse(desc.Substring(1), out res))
            {
                return res;
            }
            return 0;
        }

        static byte DoubleToByte(double val, double min, double max)
        {
            if(val < min)
            {
                return 0;
            }
            if(val > max)
            {
                return 255;
            }
            return (byte)Math.Floor((val - min) * 255 / (max - min));
        }

        public static void LoadAppearanceFromCurrentOutfit(this IAgent agent, AssetServiceInterface sceneAssetService, bool rebake = false, Action<string> logOutput = null)
        {
            UUI agentOwner = agent.Owner;
            InventoryServiceInterface inventoryService = agent.InventoryService;
            AssetServiceInterface assetService = agent.AssetService;

            if (null != logOutput)
            {
                logOutput.Invoke(string.Format("Baking agent {0}", agent.Owner.FullName));
            }
            if (agent.CurrentOutfitFolder == UUID.Zero)
            {
                InventoryFolder currentOutfitFolder = inventoryService.Folder[agentOwner.ID, AssetType.CurrentOutfitFolder];
                agent.CurrentOutfitFolder = currentOutfitFolder.ID;
                if (null != logOutput)
                {
                    logOutput.Invoke(string.Format("Retrived current outfit folder for agent {0}", agent.Owner.FullName));
                }
            }

            InventoryFolderContent currentOutfit = inventoryService.Folder.Content[agentOwner.ID, agent.CurrentOutfitFolder];
            if (currentOutfit.Version == agent.Appearance.Serial && !rebake)
            {
                if (null != logOutput)
                {
                    logOutput.Invoke(string.Format("No baking required for agent {0}", agent.Owner.FullName));
                }
                return;
            }

            /* the ordering of clothing layering is placed into the description of the link */

            List<InventoryItem> items = new List<InventoryItem>();
            List<UUID> itemlinks = new List<UUID>();
            foreach (InventoryItem item in currentOutfit.Items)
            {
                if (item.AssetType == AssetType.Link)
                {
                    items.Add(item);
                    itemlinks.Add(item.AssetID);
                }
            }
            items.Sort((item1, item2) => LinkDescriptionToInt(item1.Description).CompareTo(LinkDescriptionToInt(item2.Description)));

            Dictionary<WearableType, List<AgentWearables.WearableInfo>> wearables = new Dictionary<WearableType, List<AgentWearables.WearableInfo>>();

            List<InventoryItem> actualItems = inventoryService.Item[agentOwner.ID, itemlinks];
            Dictionary<UUID, InventoryItem> actualItemsInDict = new Dictionary<UUID, InventoryItem>();
            foreach (InventoryItem item in actualItems)
            {
                actualItemsInDict.Add(item.ID, item);
            }

            if (null != logOutput)
            {
                logOutput.Invoke(string.Format("Processing assets for baking agent {0}", agent.Owner.FullName));
            }

            foreach (InventoryItem linkItem in items)
            {
                InventoryItem actualItem;
                if (actualItemsInDict.TryGetValue(linkItem.AssetID, out actualItem))
                {
                    if (actualItem.AssetType == AssetType.Clothing || actualItem.AssetType == AssetType.Bodypart)
                    {
                        AssetData outfitData;
                        Wearable wearableData;
                        if (assetService.TryGetValue(actualItem.AssetID, out outfitData))
                        {
                            try
                            {
                                wearableData = new Wearable(outfitData);
                            }
                            catch (Exception e)
                            {
                                string info = string.Format("Asset {0} for agent {1} ({2}) failed to decode as wearable", actualItem.AssetID, agent.Owner.FullName, agent.Owner.ID);
                                m_BakeLog.ErrorFormat(info, e);
                                throw new BakingErrorException(info, e);
                            }

                            if (!wearables.ContainsKey(wearableData.Type))
                            {
                                wearables.Add(wearableData.Type, new List<AgentWearables.WearableInfo>());
                            }

                            AgentWearables.WearableInfo wearableInfo = new AgentWearables.WearableInfo();
                            wearableInfo.ItemID = actualItem.ID;
                            wearableInfo.AssetID = actualItem.AssetID;
                            wearables[wearableData.Type].Add(wearableInfo);
                        }
                    }
                }
            }

            agent.Wearables.All = wearables;

            if (null != logOutput)
            {
                logOutput.Invoke(string.Format("Processing baking for agent {0}", agent.Owner.FullName));
            }

            agent.BakeAppearanceFromWearablesInfo(sceneAssetService, logOutput);

            if (null != logOutput)
            {
                logOutput.Invoke(string.Format("Baking agent {0} completed", agent.Owner.FullName));
            }
        }
        #endregion

        #region Actual Baking Code
        const int MAX_WEARABLES_PER_TYPE = 5;

        public static void BakeAppearanceFromWearablesInfo(this IAgent agent, AssetServiceInterface sceneAssetService, Action<string> logOutput = null)
        {
            UUI agentOwner = agent.Owner;
            InventoryServiceInterface inventoryService = agent.InventoryService;
            AssetServiceInterface assetService = agent.AssetService;
            Dictionary<uint, double> visualParamInputs = new Dictionary<uint, double>();

            Dictionary<WearableType, List<AgentWearables.WearableInfo>> wearables = agent.Wearables.All;
            using (BakeStatus bakeStatus = new BakeStatus())
            {
                List<UUID> wearablesItemIds = new List<UUID>();
                for (int wearableIndex = 0; wearableIndex < MAX_WEARABLES_PER_TYPE; ++wearableIndex)
                {
                    for (int wearableType = 0; wearableType < (int)WearableType.NumWearables; ++wearableType)
                    {
                        List<AgentWearables.WearableInfo> wearablesList;
                        if (wearables.TryGetValue((WearableType)wearableType, out wearablesList))
                        {
                            if (wearablesList.Count > wearableIndex)
                            {
                                wearablesItemIds.Add(wearablesList[wearableIndex].ItemID);
                            }
                        }
                    }
                }

                List<InventoryItem> actualItems = inventoryService.Item[agentOwner.ID, new List<UUID>(wearablesItemIds)];
                Dictionary<UUID, InventoryItem> actualItemsInDict = new Dictionary<UUID, InventoryItem>();
                foreach (InventoryItem item in actualItems)
                {
                    actualItemsInDict.Add(item.ID, item);
                }

                int numberParams = 218;

                foreach (UUID itemId in wearablesItemIds)
                {
                    OutfitItem outfitItem;
                    AssetData outfitData;
                    InventoryItem inventoryItem;
                    if (actualItemsInDict.TryGetValue(itemId, out inventoryItem))
                    {
                        outfitItem = new OutfitItem(inventoryItem);
                        outfitItem.ActualItem = inventoryItem;
                        switch (inventoryItem.AssetType)
                        {
                            case AssetType.Bodypart:
                            case AssetType.Clothing:
                                if (assetService.TryGetValue(inventoryItem.AssetID, out outfitData))
                                {
                                    try
                                    {
                                        outfitItem.WearableData = new Wearable(outfitData);
                                    }
                                    catch (Exception e)
                                    {
                                        string info = string.Format("Asset {0} for agent {1} ({2}) failed to decode as wearable", inventoryItem.AssetID, agent.Owner.FullName, agent.Owner.ID);
                                        m_BakeLog.ErrorFormat(info, e);
                                        throw new BakingErrorException(info, e);
                                    }

                                    /* load visual params */
                                    foreach(KeyValuePair<uint, double> kvp in outfitItem.WearableData.Params)
                                    {
                                        visualParamInputs[kvp.Key] = kvp.Value;
                                    }

                                    if(outfitItem.WearableData.Type == WearableType.Physics)
                                    {
                                        numberParams = 251;
                                    }

                                    /* load textures beforehand and do not load unnecessarily */
                                    foreach (UUID textureID in outfitItem.WearableData.Textures.Values)
                                    {
                                        if (bakeStatus.Textures.ContainsKey(textureID))
                                        {
                                            /* skip we already got that one */
                                            continue;
                                        }
                                        AssetData textureData;
                                        if (!assetService.TryGetValue(textureID, out textureData))
                                        {
                                            string info = string.Format("Asset {0} for agent {1} ({2}) failed to be retrieved", inventoryItem.AssetID, agent.Owner.FullName, agent.Owner.ID);
                                            m_BakeLog.ErrorFormat(info);
                                            throw new BakingErrorException(info);
                                        }

                                        if (textureData.Type != AssetType.Texture)
                                        {
                                            string info = string.Format("Asset {0} for agent {1} ({2}) is not a texture (got {3})", inventoryItem.AssetID, agent.Owner.FullName, agent.Owner.ID, textureData.Type.ToString());
                                            m_BakeLog.ErrorFormat(info);
                                            throw new BakingErrorException(info);
                                        }

                                        try
                                        {
                                            bakeStatus.Textures.Add(textureData.ID, CSJ2K.J2kImage.FromStream(textureData.InputStream));
                                        }
                                        catch (Exception e)
                                        {
                                            string info = string.Format("Asset {0} for agent {1} ({2}) failed to decode as a texture", inventoryItem.AssetID, agentOwner.FullName, agentOwner.ID);
                                            m_BakeLog.ErrorFormat(info, e);
                                            throw new BakingErrorException(info, e);
                                        }
                                    }

                                    if (bakeStatus.Layer0TextureID == UUID.Zero &&
                                        !outfitItem.WearableData.Textures.TryGetValue(AvatarTextureIndex.HeadBodypaint, out bakeStatus.Layer0TextureID))
                                    {
                                        bakeStatus.Layer0TextureID = UUID.Zero;
                                    }
                                }
                                break;

                            default:
                                break;
                        }
                    }
                }

                /* update visual params */
                byte[] visualParams = new byte[numberParams];
                for(int p = 0; p < numberParams; ++p)
                {
                    double val;
                    VisualParamMap map = m_VisualParamMapping[p];
                    if (!visualParamInputs.TryGetValue((uint)map.ValueId, out val))
                    {
                        val = map.DefValue;
                    }
                    visualParams[p] = DoubleToByte(val, map.MinValue, map.MaxValue);
                }
                agent.VisualParams = visualParams;

                agent.CoreBakeLogic(bakeStatus, sceneAssetService);
            }
        }

        static void AddAlpha(Bitmap bmp, Image inp)
        {
            Bitmap bmpin = null;
            try
            {
                if (inp.Width != bmp.Width || inp.Height != bmp.Height)
                {
                    bmpin = new Bitmap(inp, bmp.Size);
                }
                else
                {
                    bmpin = new Bitmap(inp);
                }

                int x;
                int y;

                for (y = 0; y < bmp.Height; ++y)
                {
                    for (x = 0; x < bmp.Width; ++x)
                    {
                        System.Drawing.Color dst = bmp.GetPixel(x, y);
                        System.Drawing.Color src = bmpin.GetPixel(x, y);
                        bmp.SetPixel(x, y, System.Drawing.Color.FromArgb(
                            dst.A > src.A ? src.A : dst.A,
                            dst.R,
                            dst.G,
                            dst.B));
                    }
                }
            }
            finally
            {
                if (null != bmpin)
                {
                    bmpin.Dispose();
                }
            }
        }

        static void MultiplyLayerFromAlpha(Bitmap bmp, Image inp)
        {
            Bitmap bmpin = null;
            try
            {
                if (inp.Width != bmp.Width || inp.Height != bmp.Height)
                {
                    bmpin = new Bitmap(inp, bmp.Size);
                }
                else
                {
                    bmpin = new Bitmap(inp);
                }

                int x;
                int y;

                for (y = 0; y < bmp.Height; ++y)
                {
                    for (x = 0; x < bmp.Width; ++x)
                    {
                        System.Drawing.Color dst = bmp.GetPixel(x, y);
                        System.Drawing.Color src = bmpin.GetPixel(x, y);
                        bmp.SetPixel(x, y, System.Drawing.Color.FromArgb(
                            dst.A,
                            (byte)((dst.R * src.A) / 255),
                            (byte)((dst.G * src.A) / 255),
                            (byte)((dst.B * src.A) / 255)));
                    }
                }
            }
            finally
            {
                if (null != bmpin)
                {
                    bmpin.Dispose();
                }
            }
        }

        static System.Drawing.Color GetTint(Wearable w, BakeType bType)
        {
            SilverSim.Types.Color wColor = new SilverSim.Types.Color(1, 1, 1);
            double val;
            switch (w.Type)
            {
                case WearableType.Tattoo:
                    if (w.Params.TryGetValue(1071, out val))
                    {
                        wColor.R = val.Clamp(0, 1);
                    }
                    if (w.Params.TryGetValue(1072, out val))
                    {
                        wColor.G = val.Clamp(0, 1);
                    }
                    if (w.Params.TryGetValue(1073, out val))
                    {
                        wColor.B = val.Clamp(0, 1);
                    }
                    switch (bType)
                    {
                        case BakeType.Head:
                            if (w.Params.TryGetValue(1062, out val))
                            {
                                wColor.R = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(1063, out val))
                            {
                                wColor.G = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(1064, out val))
                            {
                                wColor.B = val.Clamp(0, 1);
                            }
                            break;
                        case BakeType.UpperBody:
                            if (w.Params.TryGetValue(1065, out val))
                            {
                                wColor.R = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(1066, out val))
                            {
                                wColor.G = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(1067, out val))
                            {
                                wColor.B = val.Clamp(0, 1);
                            }
                            break;
                        case BakeType.LowerBody:
                            if (w.Params.TryGetValue(1068, out val))
                            {
                                wColor.R = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(1069, out val))
                            {
                                wColor.G = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(1070, out val))
                            {
                                wColor.B = val.Clamp(0, 1);
                            }
                            break;

                        default:
                            break;
                    }
                    break;

                case WearableType.Jacket:
                    if (w.Params.TryGetValue(834, out val))
                    {
                        wColor.R = val.Clamp(0, 1);
                    }
                    if (w.Params.TryGetValue(835, out val))
                    {
                        wColor.G = val.Clamp(0, 1);
                    }
                    if (w.Params.TryGetValue(836, out val))
                    {
                        wColor.B = val.Clamp(0, 1);
                    }
                    switch (bType)
                    {
                        case BakeType.UpperBody:
                            if (w.Params.TryGetValue(831, out val))
                            {
                                wColor.R = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(832, out val))
                            {
                                wColor.G = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(833, out val))
                            {
                                wColor.B = val.Clamp(0, 1);
                            }
                            break;
                        case BakeType.LowerBody:
                            if (w.Params.TryGetValue(809, out val))
                            {
                                wColor.R = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(810, out val))
                            {
                                wColor.G = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(811, out val))
                            {
                                wColor.B = val.Clamp(0, 1);
                            }
                            break;

                        default:
                            break;
                    }
                    break;

                default:
                    wColor = w.GetTint();
                    break;
            }

            return System.Drawing.Color.FromArgb(wColor.R_AsByte, wColor.G_AsByte, wColor.B_AsByte);
        }

        static void ApplyTint(Bitmap bmp, SilverSim.Types.Color col)
        {
            int x;
            int y;
            for (y = 0; y < bmp.Height; ++y)
            {
                for (x = 0; x < bmp.Width; ++x)
                {
                    System.Drawing.Color inp = bmp.GetPixel(x, y);
                    bmp.SetPixel(x, y, System.Drawing.Color.FromArgb(
                        inp.A,
                        (byte)(inp.R * col.R).Clamp(0, 255),
                        (byte)(inp.G * col.G).Clamp(0, 255),
                        (byte)(inp.B * col.B).Clamp(0, 255)));
                }
            }
        }

        static AssetData BakeTexture(BakeStatus status, BakeType bake, AssetServiceInterface sceneAssetService)
        {
            int bakeDimensions = (bake == BakeType.Eyes) ? 128 : 512;
            Image srcimg;
            AssetData data = new AssetData();
            data.Type = AssetType.Texture;
            data.Local = true;
            data.Temporary = true;
            data.Flags = AssetFlags.Collectable | AssetFlags.Rewritable;
            AvatarTextureIndex[] bakeProcessTable;
            switch (bake)
            {
                case BakeType.Head:
                    bakeProcessTable = IndexesForBakeHead;
                    data.Name = "Baked Head Texture";
                    break;

                case BakeType.Eyes:
                    bakeProcessTable = IndexesForBakeEyes;
                    data.Name = "Baked Eyes Texture";
                    break;

                case BakeType.Hair:
                    bakeProcessTable = IndexesForBakeHair;
                    data.Name = "Baked Hair Texture";
                    break;

                case BakeType.LowerBody:
                    bakeProcessTable = IndexesForBakeLowerBody;
                    data.Name = "Baked Lower Body Texture";
                    break;

                case BakeType.UpperBody:
                    bakeProcessTable = IndexesForBakeUpperBody;
                    data.Name = "Baked Upper Body Texture";
                    break;

                case BakeType.Skirt:
                    bakeProcessTable = IndexesForBakeSkirt;
                    data.Name = "Baked Skirt Texture";
                    break;

                default:
                    throw new ArgumentOutOfRangeException("bake");
            }

            using (Bitmap bitmap = new Bitmap(bakeDimensions, bakeDimensions, PixelFormat.Format32bppArgb))
            {
                using (Graphics gfx = Graphics.FromImage(bitmap))
                {
                    if (bake == BakeType.Eyes)
                    {
                        /* eyes have white base texture */
                        using (SolidBrush brush = new SolidBrush(System.Drawing.Color.White))
                        {
                            gfx.FillRectangle(brush, new Rectangle(0, 0, 128, 128));
                        }
                    }
                    else if (status.Layer0TextureID != UUID.Zero &&
                        status.TryGetTexture(bake, status.Layer0TextureID, out srcimg))
                    {
                        /* all others are inited from layer 0 */
                        gfx.DrawImage(srcimg, 0, 0, 512, 512);
                    }
                    else
                    {
                        switch (bake)
                        {
                            case BakeType.Head:
                                gfx.DrawImage(BaseBakes.HeadColor, 0, 0, 512, 512);
                                AddAlpha(bitmap, BaseBakes.HeadAlpha);
                                MultiplyLayerFromAlpha(bitmap, BaseBakes.HeadSkinGrain);
                                break;

                            case BakeType.UpperBody:
                                gfx.DrawImage(BaseBakes.UpperBodyColor, 0, 0, 512, 512);
                                break;

                            case BakeType.LowerBody:
                                gfx.DrawImage(BaseBakes.LowerBodyColor, 0, 0, 512, 512);
                                break;

                            default:
                                break;
                        }
                    }

                    /* alpha blending is enabled by changing the compositing mode of the graphics object */
                    gfx.CompositingMode = CompositingMode.SourceOver;

                    foreach (AvatarTextureIndex texIndex in bakeProcessTable)
                    {
                        foreach (OutfitItem item in status.OutfitItems.Values)
                        {
                            UUID texture;
                            Image img;
                            if (null != item.WearableData && item.WearableData.Textures.TryGetValue(texIndex, out texture))
                            {
                                if (status.TryGetTexture(bake, texture, out img))
                                {
                                    /* duplicate texture */
                                    using (Bitmap bmp = new Bitmap(img))
                                    {
                                        switch (texIndex)
                                        {
                                            case AvatarTextureIndex.HeadBodypaint:
                                            case AvatarTextureIndex.UpperBodypaint:
                                            case AvatarTextureIndex.LowerBodypaint:
                                                /* no tinting here */
                                                break;

                                            default:
                                                ApplyTint(bmp, item.WearableData.GetTint());
                                                break;
                                        }

                                        gfx.DrawImage(bmp, 0, 0, bakeDimensions, bakeDimensions);
                                        AddAlpha(bitmap, bmp);
                                    }
                                }
                            }
                        }
                    }
                }

                data.Data = CSJ2K.J2KEncoder.EncodeJPEG(bitmap);
            }

            return data;
        }

        static readonly AvatarTextureIndex[] IndexesForBakeHead = new AvatarTextureIndex[]
        {
            AvatarTextureIndex.HeadAlpha,
            AvatarTextureIndex.HeadBodypaint,
            AvatarTextureIndex.HeadTattoo
        };

        static readonly AvatarTextureIndex[] IndexesForBakeUpperBody = new AvatarTextureIndex[]
        {
            AvatarTextureIndex.UpperBodypaint,
            AvatarTextureIndex.UpperGloves,
            AvatarTextureIndex.UpperUndershirt,
            AvatarTextureIndex.UpperShirt,
            AvatarTextureIndex.UpperJacket,
            AvatarTextureIndex.UpperAlpha
        };

        static readonly AvatarTextureIndex[] IndexesForBakeLowerBody = new AvatarTextureIndex[]
        {
            AvatarTextureIndex.LowerBodypaint,
            AvatarTextureIndex.LowerUnderpants,
            AvatarTextureIndex.LowerSocks,
            AvatarTextureIndex.LowerShoes,
            AvatarTextureIndex.LowerPants,
            AvatarTextureIndex.LowerJacket,
            AvatarTextureIndex.LowerAlpha
        };

        static readonly AvatarTextureIndex[] IndexesForBakeEyes = new AvatarTextureIndex[]
        {
            AvatarTextureIndex.EyesIris,
            AvatarTextureIndex.EyesAlpha
        };

        static readonly AvatarTextureIndex[] IndexesForBakeHair = new AvatarTextureIndex[]
        {
            AvatarTextureIndex.Hair,
            AvatarTextureIndex.HairAlpha
        };

        static readonly AvatarTextureIndex[] IndexesForBakeSkirt = new AvatarTextureIndex[]
        {
            AvatarTextureIndex.Skirt
        };

        public static object Owner { get; private set; }

        static void CoreBakeLogic(this IAgent agent, BakeStatus bakeStatus, AssetServiceInterface sceneAssetService)
        {
            AssetData bakeHead = BakeTexture(bakeStatus, BakeType.Head, sceneAssetService);
            AssetData bakeUpperBody = BakeTexture(bakeStatus, BakeType.UpperBody, sceneAssetService);
            AssetData bakeLowerBody = BakeTexture(bakeStatus, BakeType.LowerBody, sceneAssetService);
            AssetData bakeEyes = BakeTexture(bakeStatus, BakeType.Eyes, sceneAssetService);
            AssetData bakeHair = BakeTexture(bakeStatus, BakeType.Hair, sceneAssetService);
            AssetData bakeSkirt = null;

            bool haveSkirt = false;
            foreach (OutfitItem item in bakeStatus.OutfitItems.Values)
            {
                if (item.WearableData != null && item.WearableData.Type == WearableType.Skirt)
                {
                    haveSkirt = true;
                    break;
                }
            }

            if (haveSkirt)
            {
                bakeSkirt = BakeTexture(bakeStatus, BakeType.Skirt, sceneAssetService);
            }

            sceneAssetService.Store(bakeEyes);
            sceneAssetService.Store(bakeHead);
            sceneAssetService.Store(bakeUpperBody);
            sceneAssetService.Store(bakeLowerBody);
            sceneAssetService.Store(bakeHair);
            if (null != bakeSkirt)
            {
                sceneAssetService.Store(bakeSkirt);
            }

            agent.Textures[(int)AvatarTextureIndex.EyesBaked] = bakeEyes.ID;
            agent.Textures[(int)AvatarTextureIndex.HeadBaked] = bakeHead.ID;
            agent.Textures[(int)AvatarTextureIndex.UpperBaked] = bakeUpperBody.ID;
            agent.Textures[(int)AvatarTextureIndex.LowerBaked] = bakeLowerBody.ID;
            agent.Textures[(int)AvatarTextureIndex.HairBaked] = bakeHair.ID;
            agent.Textures[(int)AvatarTextureIndex.Skirt] = bakeSkirt != null ? bakeSkirt.ID : UUID.Zero;
        }

        #endregion

        #region Base Bake textures
        static class BaseBakes
        {
            public static readonly Image HeadAlpha;
            public static readonly Image HeadColor;
            public static readonly Image HeadHair;
            public static readonly Image HeadSkinGrain;
            public static readonly Image LowerBodyColor;
            public static readonly Image UpperBodyColor;

            static BaseBakes()
            {
                HeadAlpha = LoadResourceImage("head_alpha.tga.gz");
                HeadColor = LoadResourceImage("head_color.tga.gz");
                HeadHair = LoadResourceImage("head_hair.tga.gz");
                HeadSkinGrain = LoadResourceImage("head_skingrain.tga.gz");
                LowerBodyColor = LoadResourceImage("lowerbody_color.tga.gz");
                UpperBodyColor = LoadResourceImage("upperbody_color.tga.gz");
            }

            static Image LoadResourceImage(string name)
            {
                Assembly assembly = typeof(BaseBakes).Assembly;
                using (Stream resource = assembly.GetManifestResourceStream(assembly.GetName().Name + ".Resources." + name))
                {
                    using (GZipStream gz = new GZipStream(resource, CompressionMode.Decompress))
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            byte[] buf = new byte[10240];
                            int bytesRead;
                            for (bytesRead = gz.Read(buf, 0, buf.Length);
                                bytesRead > 0;
                                bytesRead = gz.Read(buf, 0, buf.Length))
                            {
                                ms.Write(buf, 0, bytesRead);
                            }
                            ms.Seek(0, SeekOrigin.Begin);
                            return Paloma.TargaImage.LoadTargaImage(ms);
                        }
                    }
                }
            }
        }
        #endregion
    }
}