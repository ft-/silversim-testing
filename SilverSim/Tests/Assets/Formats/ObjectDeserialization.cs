// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using SilverSim.Main.Common;
using SilverSim.Scene.Types.Object;
using SilverSim.Tests.Extensions;
using SilverSim.Types;
using SilverSim.Types.Asset.Format;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SilverSim.Tests.Assets.Formats
{
    public class ObjectDeserialization : ITest
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public bool Run()
        {
            List<string> manifests = new List<string>();
            foreach(string manifest in GetType().Assembly.GetManifestResourceNames())
            {
                if(manifest.StartsWith("SilverSim.Tests.Resources.Objects.") && manifest.EndsWith(".xml"))
                {
                    manifests.Add(manifest);
                }
            }

            foreach (string manifest in manifests)
            {
                m_Log.InfoFormat("Testing decoder with asset {0}", manifest);
                Stream resource = GetType().Assembly.GetManifestResourceStream(manifest);
                List<ObjectGroup> objgroup;
                try
                {
                    objgroup = ObjectXML.fromXml(resource, UUI.Unknown);
                }
                catch (Exception e)
                {
                    m_Log.InfoFormat("Failed to parse asset {0}: {1}\n{2}", e.GetType().FullName, e.StackTrace, e.StackTrace.ToString());
                    return false;
                }

                List<UUID> reflist = new List<UUID>();
                resource = GetType().Assembly.GetManifestResourceStream(manifest);
                ObjectReferenceDecoder.GetReferences(resource, "", reflist);
                m_Log.InfoFormat("Found {0} references", reflist.Count);
            }

            return true;
        }

        public void Startup(ConfigurationLoader loader)
        {
        }
    }
}
