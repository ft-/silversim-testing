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

namespace SilverSim.Types
{
    internal static class AccessorTypecastHelper
    {
        public static bool TryConvertTo<T>(this IValue iv, out T result)
        {
            Type targetType = typeof(T);
            Type sourceType = iv.GetType();
            result = default(T);
            if(sourceType == targetType)
            {
                result = (T)iv;
            }
            else if(targetType == typeof(string))
            {
                result = (T)Convert.ChangeType(iv.ToString(), targetType);
            }
            else if(targetType == typeof(UUID) && sourceType == typeof(AString))
            {
                result = (T)Convert.ChangeType(UUID.Parse(iv.ToString()), targetType);
            }
            else if (targetType == typeof(int))
            {
                result = (T)Convert.ChangeType(iv.AsInt, targetType);
            }
            else if (targetType == typeof(long))
            {
                result = (T)Convert.ChangeType(iv.AsLong, targetType);
            }
            else if (targetType == typeof(uint))
            {
                result = (T)Convert.ChangeType(iv.AsUInt, targetType);
            }
            else if (targetType == typeof(ulong))
            {
                result = (T)Convert.ChangeType(iv.AsULong, targetType);
            }
            else if (targetType == typeof(double) || targetType == typeof(float))
            {
                result = (T)Convert.ChangeType((double)iv.AsReal, targetType);
            }
            else if (targetType == typeof(Vector3))
            {
                result = (T)Convert.ChangeType(iv.AsVector3, targetType);
            }
            else if (targetType == typeof(Quaternion))
            {
                result = (T)Convert.ChangeType(iv.AsQuaternion, targetType);
            }
            else if (targetType == typeof(bool))
            {
                result = (T)Convert.ChangeType((bool)iv.AsBoolean, targetType);
            }
            else
            {
                throw new InvalidCastException($"{sourceType} to {targetType} is not supported in accessor");
            }

            return true;
        }
    }
}
