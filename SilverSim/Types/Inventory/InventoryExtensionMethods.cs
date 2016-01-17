// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.Types.Inventory
{
    public static class InventoryExtensionMethods
    {
        public static string FilterToAscii7Printable(this string s)
        {
            string o = string.Empty;
            foreach(char c in s)
            {
                o += (c >= 32 && c <= 126) ? c.ToString() : "??";
            }
            return o;
        }

        public static string FilterToNonControlChars(this string s)
        {
            string o = string.Empty;
            foreach (char c in s)
            {
                o += (c >= 32) ? c.ToString() : " ";
            }
            return o;
        }
    }
}
