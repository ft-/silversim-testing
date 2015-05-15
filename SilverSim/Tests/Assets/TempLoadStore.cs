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

using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Tests.Extensions;
using SilverSim.Types;
using SilverSim.Types.Asset;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SilverSim.Tests.Assets
{
    public class TempLoadStore : ITest
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static UUID Asset1ID = new UUID("11223344-0001-0001-0001-000000000003");
        public static UUID Asset2ID = new UUID("11223344-0001-0001-0001-000000000004");
        public static byte[] Asset1Data = new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10};
        public static byte[] Asset2Data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };

        AssetServiceInterface m_AssetService;

        public void Startup(ConfigurationLoader loader)
        {
            IConfig config = loader.Config.Configs[GetType().FullName];
            m_AssetService = loader.GetService<AssetServiceInterface>(config.GetString("AssetService"));
        }

        public string BytesToHex(byte[] b)
        {
            string o = "";
            for(int i = 0; i < b.Length; ++i)
            {
                if(o != "")
                {
                    o += " ";
                }
                o += string.Format("{0:x2}", b[i]);
            }
            return o;
        }

        public bool BytesCmp(byte[] a, byte[] b)
        {
            if(a.Length != b.Length)
            {
                return false;
            }
            for (int i = 0; i < a.Length; ++i)
            {
                if(a[i] != b[i])
                {
                    return false;
                }
            }

                return true;
        }

        public bool Run()
        {
            AssetData asset = new AssetData();
            AssetMetadata assetmetadata;
            byte[] data;
            byte[] nopdata = new byte[1];
            Stream assetstream;

            #region testing exists function
            m_Log.Info("Testing non-existence of Asset 1");
            try
            {
                m_AssetService.exists(Asset1ID);
                m_Log.Fatal("Failed to detect non-existence of asset 1");
                return false;
            }
            catch
            {
            }

            m_Log.Info("Testing non-existence of Asset 2");
            try
            {
                m_AssetService.exists(Asset2ID);
                m_Log.Fatal("Failed to detect non-existence of asset 2");
                return false;
            }
            catch
            {
            }

            m_Log.Info("Testing multiple non-existence");
            try
            {
                Dictionary<UUID, bool> assetExists = new Dictionary<UUID, bool>();
                assetExists = m_AssetService.exists(new List<UUID> { Asset1ID, Asset2ID });
                if (assetExists[Asset1ID])
                {
                    m_Log.Fatal("Failed to detect non-existence of asset 1 via multiple exist");
                    return false;
                }
                if (assetExists[Asset2ID])
                {
                    m_Log.Fatal("Failed to detect non-existence of asset 2 via multiple exist");
                    return false;
                }
            }
            catch
            {
                m_Log.Fatal("Failed to detect multiple non-existence of asset 1 and 2");
                return false;
            }
            #endregion

            UUI theCreator1 = new UUI();
            theCreator1.ID = UUID.Random;
            theCreator1.HomeURI = new Uri("http://1.example.com/");
            theCreator1.FirstName = "The";
            theCreator1.LastName = "Creator";

            UUI theCreator2 = new UUI();
            theCreator2.ID = UUID.Random;
            theCreator2.HomeURI = new Uri("http://2.example.com/");
            theCreator2.FirstName = "The";
            theCreator2.LastName = "Creator";

            m_Log.Info("Storing asset 1");
            asset.Name = "Asset 1";
            asset.ID = Asset1ID;
            asset.Type = AssetType.CallingCard;
            asset.Data = Asset1Data;
            asset.Creator = theCreator1;
            asset.Temporary = true;
            asset.Flags = AssetFlags.Normal;

            m_AssetService.Store(asset);

            m_Log.Info("Testing multiple exist (1 exists, 2 missing)");
            m_Log.Info("Testing multiple non-existence");
            try
            {
                Dictionary<UUID, bool> assetExists = new Dictionary<UUID, bool>();
                assetExists = m_AssetService.exists(new List<UUID> { Asset1ID, Asset2ID });
                if (!assetExists[Asset1ID])
                {
                    m_Log.Fatal("Failed to detect existence of asset 1 via multiple exist");
                    return false;
                }
                if (assetExists[Asset2ID])
                {
                    m_Log.Fatal("Failed to detect non-existence of asset 2 via multiple exist");
                    return false;
                }
            }
            catch
            {
                m_Log.Fatal("Failed to detect multiple non-existence of asset 1 and 2");
                return false;
            }
            try
            {
                Dictionary<UUID, bool> assetExists = new Dictionary<UUID, bool>();
                assetExists = m_AssetService.exists(new List<UUID> { Asset2ID, Asset1ID });
                if (!assetExists[Asset1ID])
                {
                    m_Log.Fatal("Failed to detect existence of asset 1 via multiple exist");
                    return false;
                }
                if (assetExists[Asset2ID])
                {
                    m_Log.Fatal("Failed to detect non-existence of asset 2 via multiple exist");
                    return false;
                }
            }
            catch
            {
                m_Log.Fatal("Failed to detect multiple non-existence of asset 2");
                return false;
            }

            m_Log.Info("Storing asset 2");
            asset.Name = "Asset 2";
            asset.ID = Asset2ID;
            asset.Type = AssetType.Mesh;
            asset.Data = Asset2Data;
            asset.Creator = theCreator2;
            asset.Temporary = true;
            asset.Flags = AssetFlags.Collectable;
            m_AssetService.Store(asset);

            #region testing exists function
            m_Log.Info("Testing existence of Asset 1");
            try
            {
                m_AssetService.exists(Asset1ID);
            }
            catch
            {
                m_Log.Fatal("Failed to detect existence of asset 1");
                return false;
            }

            m_Log.Info("Testing existence of Asset 2");
            try
            {
                m_AssetService.exists(Asset2ID);
            }
            catch
            {
                m_Log.Fatal("Failed to detect existence of asset 2");
                return false;
            }

            m_Log.Info("Testing multiple existence of Asset 1 and Asset 2");
            try
            {
                Dictionary<UUID, bool> assetExists = new Dictionary<UUID, bool>();
                assetExists = m_AssetService.exists(new List<UUID> { Asset1ID, Asset2ID });
                if (!assetExists[Asset1ID])
                {
                    m_Log.Fatal("Failed to detect existence of asset 1 via multiple exist");
                    return false;
                }
                if (!assetExists[Asset2ID])
                {
                    m_Log.Fatal("Failed to detect existence of asset 2 via multiple exist");
                    return false;
                }
            }
            catch
            {
                m_Log.Fatal("Failed to detect multiple existence of asset 1 and 2");
                return false;
            }
            #endregion

            #region Asset1 Access Test
            m_Log.Info("Testing stored asset 1");
            asset = m_AssetService[Asset1ID];
            if (asset.Name != "Asset 1")
            {
                m_Log.Error("Asset could not be retrieved correctly (Name)");
                return false;
            }
            if (asset.Type != AssetType.CallingCard)
            {
                m_Log.Error("Asset could not be retrieved correctly (AssetType)");
                return false;
            }
            if (!BytesCmp(asset.Data, Asset1Data))
            {
                m_Log.ErrorFormat("Asset could not be retrieved correctly (AssetData {0} / {1})", BytesToHex(asset.Data), BytesToHex(Asset1Data));
                return false;
            }
            if (!asset.Creator.ID.Equals(theCreator1.ID))
            {
                m_Log.ErrorFormat("Asset could not be retrieved correctly (Creator {0} / {1})", asset.Creator, theCreator1.ID);
                return false;
            }
            if (!asset.Temporary)
            {
                m_Log.ErrorFormat("Asset could not be retrieved correctly (Temporary {0} / {1})", asset.Temporary, true);
                return false;
            }
            if(asset.Flags != AssetFlags.Normal)
            {
                m_Log.Error("Asset could not be retrieved correctly (Flags)");
                return false;
            }
            #endregion

            #region Asset 2 Access Test
            m_Log.Info("Testing stored asset 2");
            asset = m_AssetService[Asset2ID];
            if(asset.Name != "Asset 2")
            {
                m_Log.Error("Asset could not be retrieved correctly (Name)");
                return false;
            }
            if (asset.Type != AssetType.Mesh)
            {
                m_Log.Error("Asset could not be retrieved correctly (AssetType)");
                return false;
            }
            if (!BytesCmp(asset.Data, Asset2Data))
            {
                m_Log.ErrorFormat("Asset could not be retrieved correctly (AssetData {0} / {1})", BytesToHex(asset.Data), BytesToHex(Asset2Data));
                return false;
            }
            if (!asset.Creator.ID.Equals(theCreator2.ID))
            {
                m_Log.ErrorFormat("Asset could not be retrieved correctly (Creator {0} / {1})", asset.Creator, theCreator2.ID);
                return false;
            }
            if (!asset.Temporary)
            {
                m_Log.ErrorFormat("Asset could not be retrieved correctly (Temporary {0} / {1})", asset.Temporary, true);
                return false;
            }
            if (asset.Flags != AssetFlags.Collectable)
            {
                m_Log.ErrorFormat("Asset could not be retrieved correctly (Flags {0} / {1})", (uint)asset.Flags, (uint)AssetFlags.Collectable);
                return false;
            }
            #endregion

            #region Asset 1 Metadata test
            m_Log.Info("Testing stored asset metadata 1");
            assetmetadata = m_AssetService.Metadata[Asset1ID];
            if (assetmetadata.Name != "Asset 1")
            {
                m_Log.Error("Asset could not be retrieved correctly (Name)");
                return false;
            }
            if (assetmetadata.Type != AssetType.CallingCard)
            {
                m_Log.Error("Asset could not be retrieved correctly (AssetType)");
                return false;
            }
            if (!assetmetadata.Creator.ID.Equals(theCreator1.ID))
            {
                m_Log.ErrorFormat("Asset could not be retrieved correctly (Creator {0} / {1})", assetmetadata.Creator, theCreator1.ID);
                return false;
            }
            if (!assetmetadata.Temporary)
            {
                m_Log.ErrorFormat("Asset could not be retrieved correctly (Temporary {0} / {1})", assetmetadata.Temporary, true);
                return false;
            }
            if (assetmetadata.Flags != AssetFlags.Normal)
            {
                m_Log.Error("Asset could not be retrieved correctly (Flags)");
                return false;
            }
            #endregion

            #region Asset 2 Metadata test
            m_Log.Info("Testing stored asset metadata 2");
            assetmetadata = m_AssetService.Metadata[Asset2ID];
            if (assetmetadata.Name != "Asset 2")
            {
                m_Log.Error("Asset could not be retrieved correctly (Name)");
                return false;
            }
            if (assetmetadata.Type != AssetType.Mesh)
            {
                m_Log.Error("Asset could not be retrieved correctly (AssetType)");
                return false;
            }
            if (!assetmetadata.Creator.ID.Equals(theCreator2.ID))
            {
                m_Log.ErrorFormat("Asset could not be retrieved correctly (Creator {0} / {1})", assetmetadata.Creator, theCreator2.ID);
                return false;
            }
            if (!assetmetadata.Temporary)
            {
                m_Log.ErrorFormat("Asset could not be retrieved correctly (Temporary {0} / {1})", assetmetadata.Temporary, true);
                return false;
            }
            if (assetmetadata.Flags != AssetFlags.Collectable)
            {
                m_Log.ErrorFormat("Asset could not be retrieved correctly (Flags {0} / {1})", (uint)assetmetadata.Flags, (uint)AssetFlags.Collectable);
                return false;
            }
            #endregion

            #region Asset1 Data Test
            m_Log.Info("Testing stored asset data 1");
            assetstream = m_AssetService.Data[Asset1ID];
            data = new byte[Asset1Data.Length];
            try
            {
                assetstream.Read(data, 0, Asset1Data.Length);
            }
            catch
            {
                m_Log.Error("Asset length does not match (Too Short)");
                return false;
            }
            try
            {
                if(0 == assetstream.Read(nopdata, 0, 1))
                {
                    throw new Exception();
                }
                m_Log.Error("Asset length does not match (Too Long)");
                return false;
            }
            catch
            {
            }
            if (!BytesCmp(data, Asset1Data))
            {
                m_Log.ErrorFormat("Asset could not be retrieved correctly (AssetData {0} / {1})", BytesToHex(data), BytesToHex(Asset1Data));
                return false;
            }
            #endregion

            #region Asset2 Data Test
            m_Log.Info("Testing stored asset data 2");
            assetstream = m_AssetService.Data[Asset2ID];
            data = new byte[Asset2Data.Length];
            try
            {
                assetstream.Read(data, 0, Asset2Data.Length);
            }
            catch
            {
                m_Log.Error("Asset length does not match (Too Short)");
                return false;
            }
            try
            {
                if(0 == assetstream.Read(nopdata, 0, 1))
                {
                    throw new Exception();
                }
                m_Log.Error("Asset length does not match (Too Long)");
                return false;
            }
            catch
            {
            }
            if (!BytesCmp(data, Asset2Data))
            {
                m_Log.ErrorFormat("Asset could not be retrieved correctly (AssetData {0} / {1})", BytesToHex(data), BytesToHex(Asset2Data));

                return false;
            }
            #endregion

            return true;
        }
    }
}
