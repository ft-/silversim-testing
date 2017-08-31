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

using SilverSim.Types;
using SilverSim.Types.Asset.Format;
using System;
using System.Collections.Generic;

namespace SilverSim.Scene.Agent.Bakery
{
    internal static class VisualParamsMapper
    {
        public static void CompleteParams(Dictionary<uint, double> param)
        {
            AvatarLad lad = BaseBakes.DefaultAvatarLad;
            foreach (AvatarLad.DriverParam m in lad.DriverParams)
            {
                double val;
                AvatarLad.VisualParam driver;
                AvatarLad.VisualParam driven;

                if(param.TryGetValue(m.FromId, out val))
                {
                }
                else if(lad.VisualParams.TryGetValue(m.FromId, out driver))
                {
                    val = driver.DefaultValue;
                }
                else
                {
                    continue;
                }
                if (!param.ContainsKey(m.ToId))
                {
                    if (m.HaveWeight)
                    {
                        driver = lad.VisualParams[m.FromId];
                        driven = lad.VisualParams[m.ToId];
                        if (val <= m.Min1)
                        {
                            val = (m.Min1 == m.Max1 && m.Min1 <= driver.MinimumValue) ?
                                driven.MaximumValue :
                                driven.MinimumValue;
                        }
                        else if (val <= m.Max1)
                        {
                            double t = (val - m.Min1) / (m.Max1 - m.Min1);
                            val = driven.MinimumValue.Lerp(driven.MaximumValue, t);
                        }
                        else if (val <= m.Max2)
                        {
                            val = driven.MaximumValue;
                        }
                        else if (val <= m.Min2 && m.Min2 > m.Max2)
                        {
                            double t = (val - m.Max2) / (m.Min2 - m.Max2);
                            val = driven.MaximumValue.Lerp(driven.MinimumValue, t);
                        }
                        else
                        {
                            val = (m.Max2 >= driver.MaximumValue) ? driven.MaximumValue : driven.MinimumValue;
                        }
                    }
                    param.Add(m.ToId, val);
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

            uint idx = 0;
            foreach (AvatarLad.VisualParam map in BaseBakes.DefaultAvatarLad.VisualParamsTarget)
            {
                double val;
                if (!visualParamInputs.TryGetValue(map.Id, out val))
                {
                    val = map.DefaultValue;
                }
                if (idx < numberParams)
                {
                    visualParams[idx++] = DoubleToByte(val, map.MinimumValue, map.MaximumValue);
                }
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
            if (!visualParamInputs.TryGetValue(682, out vpHeadSize))
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
    }
}
