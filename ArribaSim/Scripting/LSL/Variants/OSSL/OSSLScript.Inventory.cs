using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArribaSim.Types;

namespace ArribaSim.Scripting.LSL.Variants.OSSL
{
    public partial class OSSLScript
    {
        public AString osGetInventoryDesc(AString item)
        {
            try
            {
                return new AString(Part.Inventory[item.ToString()].Description);
            }
            catch
            {
                return new AString();
            }
        }
    }
}
