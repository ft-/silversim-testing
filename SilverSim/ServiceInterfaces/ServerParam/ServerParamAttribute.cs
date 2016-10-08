// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.ServiceInterfaces.ServerParam
{
    /** 
     * <summary>ServerParam declaration</summary>
     * If used on method, it must have the following signature:
     * void UpdateFunction(UUID regionid, string value)
     */
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    [Serializable]
    public class ServerParamAttribute : Attribute
    {
        public string ParameterName { get; private set; }
        public string Description { get; set; }
        public ServerParamType Type { get; set; }
        public Type ParameterType { get; set; }

        public ServerParamAttribute(string name)
        {
            ParameterName = name;
            Type = ServerParamType.GlobalAndRegion;
            Description = string.Empty;
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
