﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Scene.Types.Script
{
    public interface IScriptCompilerRegistry
    {
        IScriptCompiler this[string name]
        {
            get;
            set;
        }
    }
}
