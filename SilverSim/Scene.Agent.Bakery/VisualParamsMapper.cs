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

using SilverSim.Types.Asset.Format;
using System;
using System.Collections.Generic;

namespace SilverSim.Scene.Agent.Bakery
{
    internal static class VisualParamsMapper
    {
        public static void CompleteParams(Dictionary<uint, double> param)
        {
            foreach(VisualParamMap m in m_VisualParamMapping)
            {
                double val;
                if(param.TryGetValue(m.ValueId, out val))
                {
                    val = val.LimitRange(m.MinValue, m.MaxValue);
                    if (m.OtherValues != null)
                    {
                        foreach (uint targetid in m.OtherValues)
                        {
                            if (!param.ContainsKey(targetid))
                            {
                                param.Add(targetid, val);
                            }
                        }
                    }
                }
            }
        }

        private static readonly byte[] DefaultVisualParams = new byte[] { 33, 61, 85, 23, 58, 127, 63, 85, 63, 42, 0, 85, 63, 36, 85, 95, 153, 63, 34, 0, 63, 109, 88, 132, 63, 136, 81, 85, 103, 136, 127, 0, 150, 150, 150, 127, 0, 0, 0, 0, 0, 127, 0, 0, 255, 127, 114, 127, 99, 63, 127, 140, 127, 127, 0, 0, 0, 191, 0, 104, 0, 0, 0, 0, 0, 0, 0, 0, 0, 145, 216, 133, 0, 127, 0, 127, 170, 0, 0, 127, 127, 109, 85, 127, 127, 63, 85, 42, 150, 150, 150, 150, 150, 150, 150, 25, 150, 150, 150, 0, 127, 0, 0, 144, 85, 127, 132, 127, 85, 0, 127, 127, 127, 127, 127, 127, 59, 127, 85, 127, 127, 106, 47, 79, 127, 127, 204, 2, 141, 66, 0, 0, 127, 127, 0, 0, 0, 0, 127, 0, 159, 0, 0, 178, 127, 36, 85, 131, 127, 127, 127, 153, 95, 0, 140, 75, 27, 127, 127, 0, 150, 150, 198, 0, 0, 63, 30, 127, 165, 209, 198, 127, 127, 153, 204, 51, 51, 255, 255, 255, 204, 0, 255, 150, 150, 150, 150, 150, 150, 150, 150, 150, 150, 0, 150, 150, 150, 150, 150, 0, 127, 127, 150, 150, 150, 150, 150, 150, 150, 150, 0, 0, 150, 51, 132, 150, 150, 150 };

        public static byte[] CreateVisualParams(IEnumerable<Wearable> wearables, ref double avatarHeight)
        {
            var visualParamInputs = new Dictionary<uint, double>();
            int numberParams = 218;

            /* load visual params */
            foreach (var wearable in wearables)
            {
                foreach (var kvp in wearable.Params)
                {
                    visualParamInputs[kvp.Key] = kvp.Value;
                }
                if (wearable.Type == WearableType.Physics)
                {
                    numberParams = 251;
                }
            }

            /* map params */
            CompleteParams(visualParamInputs);

            /* build visual params */
            var visualParams = new byte[numberParams];
            /* pre-load defaults */
            Buffer.BlockCopy(DefaultVisualParams, 0, visualParams, 0, Math.Min(DefaultVisualParams.Length, visualParams.Length));

            foreach(VisualParamMap map in m_VisualParamMapping)
            {
                double val;
                if(map.ValueId >= numberParams)
                {
                    break;
                }

                if (!visualParamInputs.TryGetValue(map.ValueId, out val))
                {
                    val = map.DefValue;
                }
                visualParams[map.ValueId] = DoubleToByte(val, map.MinValue, map.MaxValue);
            }

            double vpHeight;
            double vpHeelHeight;
            double vpPlatformHeight;
            double vpHeadSize;
            double vpLegLength;
            double vpNeckLength;
            double vpHipLength;

            visualParamInputs.TryGetValue(33, out vpHeight);
            visualParamInputs.TryGetValue(198, out vpHeelHeight);
            visualParamInputs.TryGetValue(503, out vpPlatformHeight);
            if(!visualParamInputs.TryGetValue(682, out vpHeadSize))
            {
                vpHeadSize = 0.5;
            }
            visualParamInputs.TryGetValue(692, out vpLegLength);
            visualParamInputs.TryGetValue(756, out vpNeckLength);
            visualParamInputs.TryGetValue(842, out vpHipLength);

            /* algorithm has to be verified later but it at least provides a basis */
            avatarHeight = 1.706 +
                (vpLegLength * 0.1918) +
                (vpHipLength * 0.0375) +
                (vpHeight * 0.12022) +
                (vpHeadSize * 0.01117) +
                (vpNeckLength * 0.038) +
                (vpHeelHeight * .08) +
                (vpPlatformHeight * .07);

            return visualParams;
        }

        private static byte DoubleToByte(double val, double min, double max)
        {
            if (val < min)
            {
                return 0;
            }
            if (val > max)
            {
                return 255;
            }
            return (byte)Math.Floor((val - min) * 255 / (max - min));
        }


        private class VisualParamMap
        {
            public uint ValueId { get; }
            public double DefValue { get; }
            public double MinValue { get; }
            public double MaxValue { get; }
            public uint[] OtherValues { get; }

            public VisualParamMap(uint valueId, double defVal, double minVal, double maxVal, uint[] othervals = null)
            {
                ValueId = valueId;
                DefValue = defVal;
                MinValue = minVal;
                MaxValue = maxVal;
                OtherValues = othervals;
            }
        }

        private static readonly VisualParamMap[] m_VisualParamMapping = new VisualParamMap[]
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
            new VisualParamMap(16, -0.5f, -0.5f, 3f, new uint[] { 870 }),
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
            new VisualParamMap(31, 0.5f, 0f, 2f, new uint[] { 872 }),
            new VisualParamMap(33, -2.3f, -2.3f, 2f),
            new VisualParamMap(34, -0.7f, -0.7f, 1.5f),
            new VisualParamMap(35, -1f, -1f, 2f),
            new VisualParamMap(36, -0.5f, -1.8f, 1.4f),
            new VisualParamMap(37, -3.2f, -3.2f, 2.8f),
            new VisualParamMap(38, -1f, -1f, 1f),
            new VisualParamMap(80, 0f, 0f, 1f, new uint[] { 32, 153, 40, 100, 857 }),
            new VisualParamMap(93, 0.8f, 0.01f, 1f, new uint[] { 1058, 1059 }),
            new VisualParamMap(98, 0f, 0f, 1f),
            new VisualParamMap(99, 0f, 0f, 1f),
            new VisualParamMap(105, 0.5f, 0f, 1f, new uint[] { 843, 627, 626 }),
            new VisualParamMap(108, 0f, 0f, 1f),
            new VisualParamMap(110, 0f, 0f, 0.1f),
            new VisualParamMap(111, 0.5f, 0f, 1f),
            new VisualParamMap(112, 0f, 0f, 1f),
            new VisualParamMap(113, 0f, 0f, 1f),
            new VisualParamMap(114, 0.5f, 0f, 1f),
            new VisualParamMap(115, 0f, 0f, 1f),
            new VisualParamMap(116, 0f, 0f, 1f),
            new VisualParamMap(117, 0f, 0f, 1f),
            new VisualParamMap(119, 0.5f, 0f, 1f, new uint[] { 1000, 1001 }),
            new VisualParamMap(130, 0.45f, 0f, 1f, new uint[] { 144, 145 }),
            new VisualParamMap(131, 0.5f, 0f, 1f, new uint[] { 146, 147 }),
            new VisualParamMap(132, 0.39f, 0f, 1f, new uint[] { 148, 149 }),
            new VisualParamMap(133, 0.25f, 0f, 1f, new uint[] { 172, 171 }),
            new VisualParamMap(134, 0.5f, 0f, 1f, new uint[] { 174, 173 }),
            new VisualParamMap(135, 0.55f, 0f, 1f, new uint[] { 176, 175 }),
            new VisualParamMap(136, 0.5f, 0f, 1f, new uint[] { 179, 178 }),
            new VisualParamMap(137, 0.5f, 0f, 1f, new uint[] { 190, 191 }),
            new VisualParamMap(140, 0f, 0f, 2f),
            new VisualParamMap(141, 0f, 0f, 2f),
            new VisualParamMap(142, 0f, 0f, 2f),
            new VisualParamMap(143, 0.125f, -4f, 1.5f),
            new VisualParamMap(150, 0f, 0f, 1f, new uint[] { 125, 126, 160, 161, 874, 878 }),
            new VisualParamMap(155, 0f, -0.9f, 1.3f, new uint[] { 29, 30 }),
            new VisualParamMap(157, 0f, 0f, 1f, new uint[] { 104, 156, 849 }),
            new VisualParamMap(162, 0f, 0f, 1f, new uint[] { 158, 159, 873 }),
            new VisualParamMap(163, 0f, 0f, 1f, new uint[] { 118 }),
            new VisualParamMap(165, 0f, 0f, 1f),
            new VisualParamMap(166, 0f, 0f, 1f, new uint[] { 1004, 1005 }),
            new VisualParamMap(167, 0f, 0f, 1f, new uint[] { 1006, 1007 }),
            new VisualParamMap(168, 0f, 0f, 1f, new uint[] { 1008, 1009 }),
            new VisualParamMap(169, 0f, 0f, 1f, new uint[] { 1010, 1011 }),
            new VisualParamMap(171, 0f, 0f, 1f),
            new VisualParamMap(177, 0f, 0f, 1f),
            new VisualParamMap(181, 0.14f, -1f, 1f),
            new VisualParamMap(182, 0.7f, -1f, 1f),
            new VisualParamMap(183, 0.05f, -1f, 1f),
            new VisualParamMap(184, 0f, 0f, 1f),
            new VisualParamMap(185, -1f, -1f, 1f),
            new VisualParamMap(192, 0f, 0f, 1f),
            new VisualParamMap(193, 0.5f, 0f, 1f, new uint[] { 188, 642, 189, 643 }),
            new VisualParamMap(196, 0f, -2f, 1f, new uint[] { 194, 195 }),
            new VisualParamMap(198, 0f, 0f, 1f, new uint[] { 197, 500 }),
            new VisualParamMap(503, 0f, 0f, 1f, new uint[] { 501, 502 }),
            new VisualParamMap(505, 0.5f, 0f, 1f, new uint[] { 26, 28 }),
            new VisualParamMap(506, -2f, -2f, 2f),
            new VisualParamMap(507, 0f, -1.5f, 2f),
            new VisualParamMap(508, -1f, -1f, 2f),
            new VisualParamMap(513, 0.5f, 0f, 1f, new uint[] { 509, 510 }),
            new VisualParamMap(514, 0.5f, 0f, 1f, new uint[] { 511, 512 }),
            new VisualParamMap(515, -1f, -1f, 3f),
            new VisualParamMap(517, -0.5f, -0.5f, 1f),
            new VisualParamMap(518, -0.3f, -0.3f, 1.5f),
            new VisualParamMap(603, 0.4f, 0.01f, 1f, new uint[] { 1042, 1043 }),
            new VisualParamMap(604, 0.85f, 0f, 1f, new uint[] { 1044, 1045 }),
            new VisualParamMap(605, 0.84f, 0f, 1f, new uint[] { 1046, 1047 }),
            new VisualParamMap(606, 0.8f, 0f, 1f, new uint[] { 1019, 1039, 1020 }),
            new VisualParamMap(607, 0.8f, 0f, 1f, new uint[] { 1021, 1040, 1022 }),
            new VisualParamMap(608, 0.8f, 0f, 1f, new uint[] { 620, 1025, 1037, 621, 1027, 1033 }),
            new VisualParamMap(609, 0.2f, 0f, 1f, new uint[] { 622, 1026, 1038, 623, 1028, 1034 }),
            new VisualParamMap(616, 0.1f, 0f, 1f, new uint[] { 1052, 1053 }),
            new VisualParamMap(617, 0.35f, 0f, 1f, new uint[] { 1050, 1051 }),
            new VisualParamMap(619, 0.3f, 0f, 1f, new uint[] { 1054, 1055 }),
            new VisualParamMap(624, 0.8f, 0f, 1f, new uint[] { 1056, 1057 }),
            new VisualParamMap(625, 0f, 0f, 1.5f),
            new VisualParamMap(629, 0.5f, 0f, 1f, new uint[] { 630, 644, 631, 645 }),
            new VisualParamMap(637, 0f, 0f, 1f, new uint[] { 633, 634, 635, 851 }),
            new VisualParamMap(638, 0f, 0f, 1.3f),
            new VisualParamMap(646, 0f, -1.3f, 1f, new uint[] { 640, 186 }),
            new VisualParamMap(647, 0f, -0.5f, 1f, new uint[] { 641, 187 }),
            new VisualParamMap(649, 0.5f, 0f, 1f, new uint[] { 648, 106 }),
            new VisualParamMap(650, -1.3f, -1.3f, 1.2f),
            new VisualParamMap(652, 0.5f, 0f, 1f, new uint[] { 651, 152 }),
            new VisualParamMap(653, -1f, -1f, 2f),
            new VisualParamMap(654, 0f, 0f, 2f),
            new VisualParamMap(656, -2f, -2f, 2f),
            new VisualParamMap(659, 0.5f, 0f, 1f, new uint[] { 658, 657 }),
            new VisualParamMap(662, 0.5f, 0f, 1f, new uint[] { 660, 661, 774 }),
            new VisualParamMap(663, 0f, -2f, 2f),
            new VisualParamMap(664, 0f, -1.3f, 1.3f),
            new VisualParamMap(665, 0f, -2f, 2f),
            new VisualParamMap(674, -0.3f, -1f, 2f),
            new VisualParamMap(675, -0.3f, -0.3f, 0.3f),
            new VisualParamMap(676, 0f, -1f, 2f, new uint[] { 855, 856 }),
            new VisualParamMap(678, 0.5f, 0f, 1f, new uint[] { 677, 106 }),
            new VisualParamMap(682, 0.5f, 0f, 1f, new uint[] { 679, 694, 680, 681, 655 }),
            new VisualParamMap(683, -0.15f, -0.4f, 0.2f),
            new VisualParamMap(684, 0f, -0.3f, 1.3f),
            new VisualParamMap(685, 0f, -0.5f, 1.1f),
            new VisualParamMap(690, 0.5f, 0f, 1f, new uint[] { 686, 687, 695, 688, 691, 689 }),
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
            new VisualParamMap(757, -1f, -4f, 2f, new uint[] { 871 }),
            new VisualParamMap(758, -1.5f, -1.5f, 1.5f),
            new VisualParamMap(759, 0.5f, -1f, 1.5f),
            new VisualParamMap(760, 0f, -1.2f, 2f),
            new VisualParamMap(762, 0f, 0f, 3f),
            new VisualParamMap(763, 0.55f, 0f, 1f, new uint[] { 761, 180 }),
            new VisualParamMap(764, -0.5f, -0.5f, 1.2f),
            new VisualParamMap(765, -0.3f, -0.3f, 2.5f),
            new VisualParamMap(769, 0.5f, 0f, 1f, new uint[] { 767, 768 }),
            new VisualParamMap(773, 0.5f, 0f, 1f, new uint[] { 770, 771, 772 }),
            new VisualParamMap(775, 0f, 0f, 1f, new uint[] { 776, 777 }),
            new VisualParamMap(779, 0.84f, 0f, 1f, new uint[] { 1048, 1049 }),
            new VisualParamMap(780, 0.8f, 0f, 1f, new uint[] { 1023, 1041, 1024 }),
            new VisualParamMap(781, 0.78f, 0f, 1f, new uint[] { 778, 1016, 1032, 903 }),
            new VisualParamMap(785, 0f, 0f, 1f, new uint[] { 782, 783, 790, 784 }),
            new VisualParamMap(789, 0f, 0f, 1f, new uint[] { 786, 787, 788 }),
            new VisualParamMap(795, 0.25f, 0f, 1f, new uint[] { 867, 794, 151, 852 }),
            new VisualParamMap(796, -0.4f, -0.4f, 3f),
            new VisualParamMap(799, 0.5f, 0f, 1f, new uint[] { 797, 798 }),
            new VisualParamMap(800, 0.89f, 0f, 1f, new uint[] { 600, 1013, 1029, 900 }),
            new VisualParamMap(801, 1f, 0f, 1f, new uint[] { 601, 1014, 1030, 901 }),
            new VisualParamMap(802, 0.78f, 0f, 1f, new uint[] { 602, 1015, 1031, 902 }),
            new VisualParamMap(803, 1f, 0f, 1f),
            new VisualParamMap(804, 1f, 0f, 1f),
            new VisualParamMap(805, 1f, 0f, 1f),
            new VisualParamMap(806, 1f, 0f, 1f),
            new VisualParamMap(807, 1f, 0f, 1f),
            new VisualParamMap(808, 1f, 0f, 1f),
            new VisualParamMap(812, 1f, 0f, 1f),
            new VisualParamMap(813, 1f, 0f, 1f),
            new VisualParamMap(814, 1f, 0f, 1f),
            new VisualParamMap(815, 0.8f, 0f, 1f, new uint[] { 615, 1018, 1036, 793, 915 }),
            new VisualParamMap(816, 0f, 0f, 1f, new uint[] { 516, 913 }),
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
            new VisualParamMap(828, 0f, 0f, 1f, new uint[] { 628, 899 }),
            new VisualParamMap(829, 1f, 0f, 1f),
            new VisualParamMap(830, 1f, 0f, 1f),
            new VisualParamMap(834, 1f, 0f, 1f, new uint[] { 809, 831 }),
            new VisualParamMap(835, 1f, 0f, 1f, new uint[] { 810, 832 }),
            new VisualParamMap(836, 1f, 0f, 1f, new uint[] { 811, 833 }),
            new VisualParamMap(840, 0f, 0f, 1.5f),
            new VisualParamMap(841, 0f, -1f, 1f, new uint[] { 853, 847 }),
            new VisualParamMap(842, -1f, -1f, 1f),
            new VisualParamMap(844, 1f, 0.01f, 1f, new uint[] { 1060, 1061 }),
            new VisualParamMap(848, 0.2f, 0f, 2f),
            new VisualParamMap(858, 0.4f, 0.01f, 1f),
            new VisualParamMap(859, 1f, 0f, 1f),
            new VisualParamMap(860, 1f, 0f, 1f),
            new VisualParamMap(861, 1f, 0f, 1f),
            new VisualParamMap(862, 1f, 0f, 1f),
            new VisualParamMap(863, 0.333f, 0f, 1f, new uint[] { 866, 846, 845 }),
            new VisualParamMap(868, 0f, 0f, 1f),
            new VisualParamMap(869, 0f, 0f, 1f),
            new VisualParamMap(877, 0f, 0f, 1f, new uint[] { 875, 876 }),
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
    }
}
