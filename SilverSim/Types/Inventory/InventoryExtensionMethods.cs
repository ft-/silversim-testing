// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Text;

namespace SilverSim.Types.Inventory
{
    public static class InventoryExtensionMethods
    {
        public static string FilterToAscii7Printable(this string s)
        {
            StringBuilder o = new StringBuilder();
            foreach(char c in s)
            {
                o.Append((c >= 32 && c <= 126) ? c.ToString() : "??");
            }
            return o.ToString();
        }

        public static string FilterToNonControlChars(this string s)
        {
            StringBuilder o = new StringBuilder();
            foreach (char c in s)
            {
                o.Append((c >= 32) ? c.ToString() : " ");
            }
            return o.ToString();
        }
    }
}
