// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace SilverSim.ServiceInterfaces.ServerParam
{
    public abstract class ServerParamServiceInterface
    {
        public ServerParamServiceInterface()
        {

        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public abstract string this[UUID regionID, string parameter, string defvalue]
        {
            get;
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public abstract string this[UUID regionID, string parameter]
        {
            get;
            set;
        }

        public abstract bool TryGetValue(UUID regionID, string parameter, out string value);

        public abstract List<string> this[UUID regionID]
        {
            get;
        }

        public abstract bool Remove(UUID regionID, string parameter);

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public bool GetBoolean(UUID regionID, string parameter)
        {
            return bool.Parse(this[regionID, parameter]);
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
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

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public int GetInteger(UUID regionID, string parameter)
        {
            return int.Parse(this[regionID, parameter]);
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public int GetInteger(UUID regionID, string parameter, int defvalue)
        {
            return int.Parse(this[regionID, parameter, defvalue.ToString()]);
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public float GetFloat(UUID regionID, string parameter)
        {
            return float.Parse(this[regionID, parameter], CultureInfo.InvariantCulture);
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public float GetFloat(UUID regionID, string parameter, float defvalue)
        {
            return float.Parse(this[regionID, parameter, defvalue.ToString()], CultureInfo.InvariantCulture);
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public double GetDouble(UUID regionID, string parameter)
        {
            return double.Parse(this[regionID, parameter], CultureInfo.InvariantCulture);
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public double GetDouble(UUID regionID, string parameter, double defvalue)
        {
            return double.Parse(this[regionID, parameter, defvalue.ToString()], CultureInfo.InvariantCulture);
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public Vector3 GetVector(UUID regionID, string parameter)
        {
            return Vector3.Parse(this[regionID, parameter]);
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public Vector3 GetVector(UUID regionID, string parameter, Vector3 v)
        {
            return Vector3.Parse(this[regionID, parameter, v.ToString()]);
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public UUID GetUUID(UUID regionID, string parameter)
        {
            return UUID.Parse(this[regionID, parameter]);
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public UUID GetUUID(UUID regionID, string parameter, UUID defvalue)
        {
            return UUID.Parse(this[regionID, parameter, defvalue.ToString()]);
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public Quaternion GetQuaternion(UUID regionID, string parameter)
        {
            return Quaternion.Parse(this[regionID, parameter]);
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public Quaternion GetQuaternion(UUID regionID, string parameter, Quaternion defvalue)
        {
            return Quaternion.Parse(this[regionID, parameter, defvalue.ToString()]);
        }
    }
}
