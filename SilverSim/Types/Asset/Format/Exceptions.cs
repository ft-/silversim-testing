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

using System;
using System.Runtime.Serialization;

namespace SilverSim.Types.Asset.Format
{
    [Serializable]
    public class NotAMeshFormatException : Exception
    {
        public NotAMeshFormatException()
        {

        }

        public NotAMeshFormatException(string message)
            : base(message)
        {

        }

        protected NotAMeshFormatException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            
        }

        public NotAMeshFormatException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }

    [Serializable]
    public class NotAMaterialFormatException : Exception
    {
        public NotAMaterialFormatException()
        {

        }

        public NotAMaterialFormatException(string message)
            : base(message)
        {

        }

        protected NotAMaterialFormatException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public NotAMaterialFormatException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }

    [Serializable]
    public class NotANotecardFormatException : Exception
    {
        public NotANotecardFormatException()
        {

        }

        public NotANotecardFormatException(string message)
            : base(message)
        {

        }

        protected NotANotecardFormatException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public NotANotecardFormatException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }

    [Serializable]
    public class NotALandmarkFormatException : Exception
    {
        public NotALandmarkFormatException()
        {

        }
 
        public NotALandmarkFormatException(string message)
            : base(message)
        {

        }

        protected NotALandmarkFormatException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public NotALandmarkFormatException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
   }

    [Serializable]
    public class NotAWearableFormatException : Exception
    {
        public NotAWearableFormatException()
        {

        }

        public NotAWearableFormatException(string message)
            : base(message)
        {

        }

        protected NotAWearableFormatException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public NotAWearableFormatException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }

    [Serializable]
    public class NotAGestureFormatException : Exception
    {
        public NotAGestureFormatException()
        {

        }

        public NotAGestureFormatException(string message)
            : base(message)
        {

        }

        protected NotAGestureFormatException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public NotAGestureFormatException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }

}
