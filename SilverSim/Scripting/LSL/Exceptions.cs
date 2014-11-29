using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Scripting.LSL
{
    public class ResetScriptException : Exception
    {
        public ResetScriptException()
        {

        }
    }

    public class ChangeStateException : Exception
    {
        public string NewState { get; private set; }
        public ChangeStateException(string newstate)
        {
            NewState = newstate;
        }
    }
}
