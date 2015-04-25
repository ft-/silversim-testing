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
    class LandmarkFormat : ITest
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public bool Run()
        {
            Landmark landmark;
            Landmark landmarkserialized;
            AssetData assetdata;

            m_Log.Info("Serializing grid-local landmark variant");
            landmark = new Landmark();
            landmark.LocalPos = new Vector3(1, 2, 4);
            landmark.Location = new GridVector(1000, 2000);
            landmark.RegionID = UUID.Random;

            assetdata = landmark.Asset();
            landmarkserialized = new Landmark(assetdata);

            if (landmarkserialized.GatekeeperURI != null)
            {
                m_Log.Fatal("Landmark gatekeeper URI is unexpectedly set");
                return false;
            }

            if(landmarkserialized.LocalPos != landmark.LocalPos)
            {
                m_Log.Fatal("Landmark LocalPos not identical");
                return false;
            }

            if (landmarkserialized.Location.RegionHandle != landmark.Location.RegionHandle)
            {
                m_Log.Fatal("Landmark Location not identical");
                return false;
            }

            if (landmarkserialized.RegionID != landmark.RegionID)
            {
                m_Log.Fatal("Landmark RegionID not identical");
                return false;
            }

            m_Log.Info("Serializing HG landmark variant");
            landmark = new Landmark();
            landmark.GatekeeperURI = new URI("http://gatekeeper.example.com/");
            landmark.LocalPos = new Vector3(1, 2, 4);
            landmark.Location = new GridVector(1000, 2000);
            landmark.RegionID = UUID.Random;

            assetdata = landmark.Asset();
            landmarkserialized = new Landmark(assetdata);

            if (!landmarkserialized.GatekeeperURI.Equals(landmark.GatekeeperURI))
            {
                m_Log.FatalFormat("Landmark gatekeeper URI not identical ({0} != {1})", landmarkserialized.GatekeeperURI, landmark.GatekeeperURI);
                return false;
            }

            if (landmarkserialized.LocalPos != landmark.LocalPos)
            {
                m_Log.Fatal("Landmark LocalPos not identical");
                return false;
            }

            if (landmarkserialized.Location.RegionHandle != landmark.Location.RegionHandle)
            {
                m_Log.Fatal("Landmark Location not identical");
                return false;
            }

            if (landmarkserialized.RegionID != landmark.RegionID)
            {
                m_Log.Fatal("Landmark RegionID not identical");
                return false;
            }
            return true;
        }

        public void Startup(ConfigurationLoader loader)
        {
        }
    }
}
