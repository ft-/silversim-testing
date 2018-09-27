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

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SilverSim.Types.Asset.Format
{
    public class ScriptSource : IReferencesAccessor
    {
        private static readonly Regex m_HeuristicUUIDPattern = new Regex("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");

        public ScriptSource(AssetData asset)
        {
            using (var assetdata = asset.InputStream)
            {
                string data = assetdata.ReadToStreamEnd().FromUTF8Bytes();
                foreach(Match match in m_HeuristicUUIDPattern.Matches(data))
                {
                    UUID id = new UUID(match.Value);
                    if(id != UUID.Zero && !References.Contains(id))
                    {
                        References.Add(id);
                    }
                }
            }
        }

        public List<UUID> References { get; } = new List<UUID>();
    }
}
