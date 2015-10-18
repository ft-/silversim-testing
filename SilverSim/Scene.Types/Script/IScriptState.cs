// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Xml;

namespace SilverSim.Scene.Types.Script
{
    [Serializable]
    public class ScriptStateLoaderNotImplementedException : Exception
    {
        /* do not throw this exception after calling any XmlTextReader function */
        public ScriptStateLoaderNotImplementedException()
        {

        }
    }

    public interface IScriptState
    {
        void ToXml(XmlTextWriter writer);
    }
}
