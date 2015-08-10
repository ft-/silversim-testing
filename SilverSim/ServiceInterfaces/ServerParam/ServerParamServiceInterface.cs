// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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

        public abstract bool Remove(UUID regionID, string parameter);

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
