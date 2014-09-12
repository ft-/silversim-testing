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

using SilverSim.Types;
using System.Collections.Generic;
using System.Globalization;

namespace SilverSim.ServiceInterfaces.ServerParam
{
    public abstract class ServerParamServiceInterface
    {
        public ServerParamServiceInterface()
        {

        }

        public abstract string this[UUID regionID, string parameter, string defvalue]
        {
            get;
        }

        public abstract string this[UUID regionID, string parameter]
        {
            get;
            set;
        }

        public abstract List<string> this[UUID regionID]
        {
            get;
        }

        public bool GetBoolean(UUID regionID, string parameter)
        {
            return bool.Parse(this[regionID, parameter]);
        }

        public bool GetBoolean(UUID regionID, string parameter, bool defvalue)
        {
            return bool.Parse(this[regionID, parameter, defvalue.ToString()]);
        }

        public string GetString(UUID regionID, string parameter)
        {
            return this[regionID, parameter];
        }

        public string GetString(UUID regionID, string parameter, string defvalue)
        {
            return this[regionID, parameter, defvalue];
        }

        public int GetInteger(UUID regionID, string parameter)
        {
            return int.Parse(this[regionID, parameter]);
        }

        public int GetInteger(UUID regionID, string parameter, int defvalue)
        {
            return int.Parse(this[regionID, parameter, defvalue.ToString()]);
        }

        public float GetFloat(UUID regionID, string parameter)
        {
            return float.Parse(this[regionID, parameter], CultureInfo.InvariantCulture);
        }

        public float GetFloat(UUID regionID, string parameter, float defvalue)
        {
            return float.Parse(this[regionID, parameter, defvalue.ToString()], CultureInfo.InvariantCulture);
        }

        public double GetDouble(UUID regionID, string parameter)
        {
            return double.Parse(this[regionID, parameter], CultureInfo.InvariantCulture);
        }

        public double GetDouble(UUID regionID, string parameter, double defvalue)
        {
            return double.Parse(this[regionID, parameter, defvalue.ToString()], CultureInfo.InvariantCulture);
        }

        public Vector3 GetVector(UUID regionID, string parameter)
        {
            return Vector3.Parse(this[regionID, parameter]);
        }

        public Vector3 GetVector(UUID regionID, string parameter, Vector3 v)
        {
            return Vector3.Parse(this[regionID, parameter, v.ToString()]);
        }

        public UUID GetUUID(UUID regionID, string parameter)
        {
            return UUID.Parse(this[regionID, parameter]);
        }

        public UUID GetUUID(UUID regionID, string parameter, UUID defvalue)
        {
            return UUID.Parse(this[regionID, parameter, defvalue.ToString()]);
        }

        public Quaternion GetQuaternion(UUID regionID, string parameter)
        {
            return Quaternion.Parse(this[regionID, parameter]);
        }

        public Quaternion GetQuaternion(UUID regionID, string parameter, Quaternion defvalue)
        {
            return Quaternion.Parse(this[regionID, parameter, defvalue.ToString()]);
        }
    }
}
