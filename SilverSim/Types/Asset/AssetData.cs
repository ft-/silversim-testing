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

using SilverSim.Types.Asset.Format;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Collections.Generic;
using System.IO;

namespace SilverSim.Types.Asset
{
    public class AssetData : AssetMetadata, IReferencesAccessor
    {
        public byte[] Data = new byte[0];

        public AssetData()
        {
        }

        public AssetData(AssetData copy)
            : base(copy)
        {
            Data = new byte[copy.Data.Length];
            Buffer.BlockCopy(copy.Data, 0, Data, 0, Data.Length);
        }

        public Stream InputStream
        {
            get { return new MemoryStream(Data); }
        }

        #region References accessor
        public List<UUID> References
        {
            get
            {
                List<UUID> refs;
                switch(Type)
                {
                    case AssetType.Bodypart:
                    case AssetType.Clothing:
                        try
                        {
                            refs = new Wearable(this).References;
                            refs.Remove(ID);
                        }
                        catch
                        {
                            refs = new List<UUID>();
                        }
                        return refs;

                    case AssetType.Gesture:
                        try
                        {
                            refs = new Gesture(this).References;
                            refs.Remove(ID);
                        }
                        catch
                        {
                            refs = new List<UUID>();
                        }
                        return refs;

                    case AssetType.Material:
                        try
                        {
                            refs = new Material(this).References;
                            refs.Remove(ID);
                        }
                        catch
                        {
                            refs = new List<UUID>();
                        }
                        return refs;

                    case AssetType.Notecard:
                        try
                        {
                            refs = new Notecard(this).References;
                            refs.Remove(ID);
                        }
                        catch
                        {
                            refs = new List<UUID>();
                        }
                        return refs;

                    case AssetType.Object:
                        try
                        {
                            return ObjectReferenceDecoder.GetReferences(this);
                        }
                        catch
                        {
                            return new List<UUID>();
                        }

                    default:
                        break;
                }

                return new List<UUID>();
            }
        }
        #endregion

        public bool Sanitize()
        {
            switch(Type)
            {
                case AssetType.Mesh:
                    return SanitizeMesh();

                default:
                    return true;
            }
        }

        private bool SanitizeMesh()
        {
            try
            {
                using (Stream inputstream = InputStream)
                {
                    var meshData = LlsdBinary.Deserialize(inputstream) as Map;
                    if (meshData == null)
                    {
                        return false;
                    }
                    if (meshData.ContainsKey("version"))
                    {
                        return true;
                    }
                    using (var meshstream = new MemoryStream())
                    {
                        LlsdBinary.Serialize(meshData, meshstream);
                        inputstream.CopyTo(meshstream);
                        Data = meshstream.ToArray();
                    }
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
