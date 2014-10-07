using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Scene.Types.Script
{
    /* this interface exists only to break the circular relationship with ScriptWorkerThreadPool */
    public interface IScriptWorkerThreadPool
    {
        void AbortScript(ScriptInstance script);
    }
}
