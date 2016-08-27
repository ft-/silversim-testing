// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.ServiceInterfaces.ServerParam
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    [Serializable]
    public class ServerParamAttribute : Attribute
    {
        public string ParameterName { get; private set; }
        public ServerParamAttribute(string name)
        {
            ParameterName = name;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    [Serializable]
    public class ServerParamStartsWithAttribute : Attribute
    {
        public string ParameterNameStartsWith { get; private set; }
        public ServerParamStartsWithAttribute(string namestartswith)
        {
            ParameterNameStartsWith = namestartswith;
        }
    }
}
