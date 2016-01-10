// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.ServiceInterfaces.ServerParam
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    [Serializable]
    public class ServerParamAttribute : Attribute
    {
        public string ParameterName { get; private set; }
        public ServerParamAttribute(string name)
        {
            ParameterName = name;
        }
    }
}
