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
            ParameterType = typeof(string);
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
