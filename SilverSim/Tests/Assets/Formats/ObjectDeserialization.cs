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
using SilverSim.Scene.Types.Object;
using SilverSim.Tests.Extensions;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;

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
                using (XmlTextReader reader = new XmlTextReader(resource))
                {
                    List<ObjectGroup> objgroup;
                    try
                    {
                        objgroup = ObjectXML.fromXml(reader, UUI.Unknown);
                    }
                    catch (Exception e)
                    {
                        m_Log.InfoFormat("Failed to parse asset {0}: {1}\n{2}", e.GetType().FullName, e.StackTrace, e.StackTrace.ToString());
                        return false;
                    }

                }

                List<UUID> reflist = new List<UUID>();
                resource = GetType().Assembly.GetManifestResourceStream(manifest);
                using (XmlTextReader reader = new XmlTextReader(resource))
                {
                    ObjectReferenceDecoder.GetReferences(reader, "", reflist);
                    m_Log.InfoFormat("Found {0} references", reflist.Count);
                }
            }

            return true;
        }

        public void Startup(ConfigurationLoader loader)
        {
        }
    }
}
