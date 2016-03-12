// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace SilverSim.Types.Asset
{
    public class AssetData : AssetMetadata, Format.IReferencesAccessor
    {
        public byte[] Data = new byte[0];

        public AssetData()
        {

        }

        [SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule")]
        public Stream InputStream
        {
            get
            {
                return new MemoryStream(Data);
            }
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
                }

                return new List<UUID>();
            }
        }
        #endregion
    }
}
