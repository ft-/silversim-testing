// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Runtime.Serialization;

namespace SilverSim.Scene.Types.Script
{
    [Serializable]
    public class ScriptAbortException : Exception
    {
        public ScriptAbortException()
        {

        }

        public ScriptAbortException(string message)
            : base(message)
        {

        }

        protected ScriptAbortException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public ScriptAbortException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }

    public interface IScriptWorkerThreadPool
    {
        void AbortScript(ScriptInstance script);
    }
}
