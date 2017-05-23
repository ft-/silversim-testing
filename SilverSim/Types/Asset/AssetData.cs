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
using System.IO;

namespace SilverSim.Types.Asset
{
    public class AssetData : AssetMetadata, Format.IReferencesAccessor
    {
        public byte[] Data = new byte[0];

        public Stream InputStream
        {
            get { return new MemoryStream(Data); }
        }

        #region References accessor
        public List<UUID> References
        {
            get
            {
                switch(Type)
                {
                    case AssetType.Bodypart:
                    case AssetType.Clothing:
                        return new Format.Wearable(this).References;

                    case AssetType.Gesture:
                        return new Format.Gesture(this).References;

                    case AssetType.Material:
                        return new Format.Material(this).References;

                    case AssetType.Notecard:
                        return new Format.Notecard(this).References;

                    case AssetType.Object:
                        return Format.ObjectReferenceDecoder.GetReferences(this);

                    default:
                        break;
                }

                return new List<UUID>();
            }
        }
        #endregion
    }
}
