/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types.Inventory;
using System;
using ThreadedClasses;

namespace SilverSim.Scripting.LSL.Variants.LSL
{
    public partial class LSLScript
    {
        public string llGetScriptName()
        {
            lock (this)
            {
                try
                {
                    Part.Inventory.ForEach(delegate(ObjectPartInventoryItem item)
                    {
                        if (item.ScriptInstance == this)
                        {
                            throw new ReturnValueException<ObjectPartInventoryItem>(item);
                        }
                    });
                }
                catch (ReturnValueException<ObjectPartInventoryItem> e)
                {
                    return e.Value.Name;
                }
                return string.Empty;
            }
        }

        public void llResetScript()
        {
            throw new ResetScriptException();
        }

        public void llResetOtherScript(string name)
        {
            lock (this)
            {
                ObjectPartInventoryItem item;
                ScriptInstance si;
                if (Part.Inventory.TryGetValue(name, out item))
                {
                    si = item.ScriptInstance;
                    if (item.InventoryType != InventoryType.LSLText && item.InventoryType != InventoryType.LSLBytecode)
                    {
                        throw new Exception(string.Format("Inventory item {0} is not a script", name));
                    }
                    else if (null == si)
                    {
                        throw new Exception(string.Format("Inventory item {0} is not a compiled script", name));
                    }
                    else
                    {
                        si.PostEvent(new ResetScriptEvent());
                    }
                }
                else
                {
                    throw new Exception(string.Format("Inventory item {0} does not exist", name));
                }
            }
        }

        public int llGetScriptState(string script)
        {
            ObjectPartInventoryItem item;
            ScriptInstance si;
            lock (this)
            {
                if (Part.Inventory.TryGetValue(script, out item))
                {
                    si = item.ScriptInstance;
                    if (item.InventoryType != InventoryType.LSLText && item.InventoryType != InventoryType.LSLBytecode)
                    {
                        throw new Exception(string.Format("Inventory item {0} is not a script", script));
                    }
                    else if (null == si)
                    {
                        throw new Exception(string.Format("Inventory item {0} is not a compiled script", script));
                    }
                    else
                    {
                        return si.IsRunning ? TRUE : FALSE;
                    }
                }
                else
                {
                    throw new Exception(string.Format("Inventory item {0} does not exist", script));
                }
            }
        }

        public void llSetScriptState(string script, int running)
        {
            ObjectPartInventoryItem item;
            ScriptInstance si;
            lock (this)
            {
                if (Part.Inventory.TryGetValue(script, out item))
                {
                    si = item.ScriptInstance;
                    if (item.InventoryType != InventoryType.LSLText && item.InventoryType != InventoryType.LSLBytecode)
                    {
                        throw new Exception(string.Format("Inventory item {0} is not a script", script));
                    }
                    else if (null == si)
                    {
                        throw new Exception(string.Format("Inventory item {0} is not a compiled script", script));
                    }
                    else
                    {
                        si.IsRunning = running != 0;
                    }
                }
                else
                {
                    throw new Exception(string.Format("Inventory item {0} does not exist", script));
                }
            }
        }
    }
}
