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
    class WearableFormat : ITest
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public bool Run()
        {
            Wearable wearable;
            Wearable wearableserialized;
            AssetData assetdata;
            UUI theCreator = new UUI();
            theCreator.ID = UUID.Random;
            theCreator.HomeURI = new Uri("http://example.com/");
            theCreator.FirstName = "The";
            theCreator.LastName = "Creator";

            wearable = new Wearable();
            wearable.Type = WearableType.Shape;
            wearable.Creator = theCreator;
            wearable.Description = "Wearable Description";
            wearable.Name = "Wearable Name";
            wearable.Group = UGI.Unknown;
            wearable.LastOwner = theCreator;
            wearable.Owner = theCreator;
            wearable.Params.Add(1, 1);
            wearable.Params.Add(2, 2);
            wearable.Params.Add(4, 4);
            wearable.Params.Add(8, 8);
            wearable.Params.Add(16, 16);
            wearable.Textures.Add(1, UUID.Random);
            wearable.Textures.Add(2, UUID.Random);
            wearable.Textures.Add(3, UUID.Random);

            m_Log.Info("Testing Serialization");
            assetdata = wearable.Asset();
            wearableserialized = new Wearable(assetdata);

            if(wearableserialized.Type != wearable.Type)
            {
                m_Log.Fatal("Wearable Type not identical");
                return false;
            }
            if (wearableserialized.Creator.ID != wearable.Creator.ID)
            {
                m_Log.Fatal("Wearable CreatorID not identical");
                return false;
            }
            if (wearableserialized.Description != wearable.Description)
            {
                m_Log.Fatal("Wearable Description not identical");
                return false;
            }
            if (wearableserialized.Name != wearable.Name)
            {
                m_Log.Fatal("Wearable Name not identical");
                return false;
            }
            if (wearableserialized.Group.ID != wearable.Group.ID)
            {
                m_Log.Fatal("Wearable GroupID not identical");
                return false;
            }
            if (wearableserialized.LastOwner.ID != wearable.LastOwner.ID)
            {
                m_Log.Fatal("Wearable LastOwnerID not identical");
                return false;
            }
            if (wearableserialized.Owner.ID != wearable.Owner.ID)
            {
                m_Log.Fatal("Wearable OwnerID not identical");
                return false;
            }

            if(wearable.Params.Count != wearableserialized.Params.Count)
            {
                m_Log.Fatal("Wearable Param Count not identical");
                return false;
            }
            if (wearable.Textures.Count != wearableserialized.Textures.Count)
            {
                m_Log.Fatal("Wearable Texture Count not identical");
                return false;
            }

            foreach(KeyValuePair<uint, double> kvp in wearable.Params)
            {
                if(kvp.Value != wearableserialized.Params[kvp.Key])
                {
                    m_Log.Fatal("Wearable Param not identical");
                    return false;
                }
            }
            foreach (KeyValuePair<uint, UUID> kvp in wearable.Textures)
            {
                if (kvp.Value != wearableserialized.Textures[kvp.Key])
                {
                    m_Log.Fatal("Wearable Texture not identical");
                    return false;
                }
            }

            m_Log.Info("Testing References");
            List<UUID> refs = wearableserialized.References;
            if(refs.Count != 3)
            {
                m_Log.Fatal("Wearable reference count is not 3");
                return false;
            }

            if(!refs.Contains(wearable.Textures[1]))
            {
                m_Log.Fatal("Wearable references miss texture 1");
                return false;
            }

            if (!refs.Contains(wearable.Textures[2]))
            {
                m_Log.Fatal("Wearable references miss texture 2");
                return false;
            }

            if (!refs.Contains(wearable.Textures[3]))
            {
                m_Log.Fatal("Wearable references miss texture 3");
                return false;
            }
            return true;
        }

        public void Startup(ConfigurationLoader loader)
        {
        }
    }
}
