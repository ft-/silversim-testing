// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;

namespace SilverSim.Scripting.LSL.API.XMLRPC
{
    [ScriptApiName("XMLRPC")]
    [LSLImplementation]
    public class XMLRPC_API : MarshalByRefObject, IScriptApi, IPlugin
    {

        public XMLRPC_API()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        [APILevel(APIFlags.LSL)]
        public void llCloseRemoteDataChannel(ScriptInstance Instance, LSLKey key)
        {
#warning Implement llCloseRemoteDataChannel(UUID)
            throw new NotImplementedException();
        }
    }
}
