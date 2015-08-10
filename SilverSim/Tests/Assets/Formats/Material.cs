// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using SilverSim.Main.Common;
using SilverSim.Tests.Extensions;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SilverSim.Tests.Assets.Formats
{
    class MaterialFormat : ITest
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public bool Run()
        {
            Material material;
            Material materialserialized;
            AssetData assetdata;

            m_Log.Info("Testing Material serialization");
            material = new Material();
            material.AlphaMaskCutoff = 1;
            material.DiffuseAlphaMode = 2;
            material.EnvIntensity = 3;
            material.NormMap = UUID.Random;
            material.NormOffsetX = 4;
            material.NormOffsetY = 8;
            material.NormRepeatX = 16;
            material.NormRepeatY = 32;
            material.NormRotation = 64;
            material.SpecColor = new ColorAlpha(1, 1, 1, 1);
            material.SpecExp = 1;
            material.SpecMap = UUID.Random;
            material.SpecOffsetX = 1;
            material.SpecOffsetY = 2;
            material.SpecRepeatX = 4;
            material.SpecRepeatY = 8;
            material.SpecRotation = 16;

            assetdata = material.Asset();
            materialserialized = new Material(assetdata);

            if(material.AlphaMaskCutoff != materialserialized.AlphaMaskCutoff)
            {
                m_Log.Fatal("Material AlphaMaskCutOff not identical");
                return false;
            }

            if (material.DiffuseAlphaMode != materialserialized.DiffuseAlphaMode)
            {
                m_Log.Fatal("Material DiffuseAlphaMode not identical");
                return false;
            }

            if (material.EnvIntensity != materialserialized.EnvIntensity)
            {
                m_Log.Fatal("Material EnvIntensity not identical");
                return false;
            }

            if (material.NormMap != materialserialized.NormMap)
            {
                m_Log.Fatal("Material NormMap not identical");
                return false;
            }

            if (material.NormOffsetX != materialserialized.NormOffsetX)
            {
                m_Log.Fatal("Material NormOffsetX not identical");
                return false;
            }

            if (material.NormOffsetY != materialserialized.NormOffsetY)
            {
                m_Log.Fatal("Material NormOffsetY not identical");
                return false;
            }

            if (material.NormRepeatX != materialserialized.NormRepeatX)
            {
                m_Log.Fatal("Material NormRepeatX not identical");
                return false;
            }

            if (material.NormRepeatY != materialserialized.NormRepeatY)
            {
                m_Log.Fatal("Material NormRepeatY not identical");
                return false;
            }

            if (material.NormRotation != materialserialized.NormRotation)
            {
                m_Log.Fatal("Material NormRotation not identical");
                return false;
            }

            if (material.SpecColor.R != materialserialized.SpecColor.R ||
                material.SpecColor.G != materialserialized.SpecColor.G ||
                material.SpecColor.B != materialserialized.SpecColor.B ||
                material.SpecColor.A != materialserialized.SpecColor.A)
            {
                m_Log.Fatal("Material SpecColor not identical");
                return false;
            }

            if (material.SpecExp != materialserialized.SpecExp)
            {
                m_Log.Fatal("Material SpecExp not identical");
                return false;
            }

            if (material.SpecMap != materialserialized.SpecMap)
            {
                m_Log.Fatal("Material SpecMap not identical");
                return false;
            }

            if (material.SpecOffsetX != materialserialized.SpecOffsetX)
            {
                m_Log.Fatal("Material SpecOffsetX not identical");
                return false;
            }

            if (material.SpecOffsetY != materialserialized.SpecOffsetY)
            {
                m_Log.Fatal("Material SpecOffsetY not identical");
                return false;
            }

            if (material.SpecRepeatX != materialserialized.SpecRepeatX)
            {
                m_Log.Fatal("Material SpecRepeatX not identical");
                return false;
            }

            if (material.SpecRepeatY != materialserialized.SpecRepeatY)
            {
                m_Log.Fatal("Material SpecRepeatY not identical");
                return false;
            }

            if (material.SpecRotation != materialserialized.SpecRotation)
            {
                m_Log.Fatal("Material SpecRotation not identical");
                return false;
            }

            List<UUID> refs = materialserialized.References;
            if(refs.Count != 2)
            {
                m_Log.Fatal("Material references count is not 2");
                return false;
            }

            if(!refs.Contains(material.NormMap))
            {
                m_Log.Fatal("Material references misses NormMap");
                return false;
            }

            if (!refs.Contains(material.SpecMap))
            {
                m_Log.Fatal("Material references misses SpecMap");
                return false;
            }

            m_Log.Info("Testing unset NormMap and SpecMap");
            material = new Material();
            refs = material.References;
            if (refs.Count != 0)
            {
                m_Log.Fatal("Material references count is not 0");
                return false;
            }

            m_Log.Info("Testing NormMap only");
            material = new Material();
            material.NormMap = UUID.Random;
            refs = material.References;
            if (refs.Count != 1)
            {
                m_Log.Fatal("Material references count is not 0");
                return false;
            }

            if (!refs.Contains(material.NormMap))
            {
                m_Log.Fatal("Material references misses NormMap");
                return false;
            }

            m_Log.Info("Testing SpecMap only");
            material = new Material();
            material.SpecMap = UUID.Random;
            refs = material.References;
            if (refs.Count != 1)
            {
                m_Log.Fatal("Material references count is not 0");
                return false;
            }

            if (!refs.Contains(material.SpecMap))
            {
                m_Log.Fatal("Material references misses SpecMap");
                return false;
            }

            return true;
        }

        public void Startup(ConfigurationLoader loader)
        {
        }
    }
}
