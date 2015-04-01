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

using SilverSim.BackendConnectors.Simian.Common;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Asset;
using System;
using System.Collections.Generic;

namespace SilverSim.BackendConnectors.Simian.Asset
{
    public class SimianAssetMetadataConnector : AssetMetadataServiceInterface
    {
        public int TimeoutMs = 20000;
        private string m_AssetURI;
        private string m_AssetCapability;

        #region Constructor
        public SimianAssetMetadataConnector(string uri, string capability)
        {
            m_AssetURI = uri;
            m_AssetCapability = capability;
        }
        #endregion

        #region Metadata accessors
        public override AssetMetadata this[UUID key]
        {
            get
            {
                Dictionary<string, string> para = new Dictionary<string, string>();
                para["RequestMethod"] = "xGetAssetMetadata";
                para["ID"] = key;
                Map m = SimianGrid.PostToService(m_AssetURI, m_AssetCapability, para, TimeoutMs);
                if(!m["Success"].AsBoolean)
                {
                    throw new AssetNotFound(key);
                }
                AssetMetadata data = new AssetMetadata();
                data.ID = key;
                data.Name = string.Empty;
                data.ContentType = m["ContentType"].ToString();
                data.Creator.FullName = m["CreatorID"].ToString();
                data.Local = false;
                data.Temporary = m["Temporary"].AsBoolean;

                string lastModifiedStr = m["Last-Modified"].ToString();
                if (!string.IsNullOrEmpty(lastModifiedStr))
                {
                    DateTime lastModified;
                    if (DateTime.TryParse(lastModifiedStr, out lastModified))
                    {
                        data.CreateTime = new Date(lastModified);
                    }
                }
                return data;
            }
        }
        #endregion
    }
}
