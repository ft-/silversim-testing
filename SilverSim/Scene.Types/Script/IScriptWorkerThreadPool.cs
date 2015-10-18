// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.Scene.Types.Script
{
    [Serializable]
    public class ScriptAbortException : Exception
    {
        public ScriptAbortException()
        {

        }
    }

    public interface IScriptWorkerThreadPool
    {
        void AbortScript(ScriptInstance script);
    }
}
