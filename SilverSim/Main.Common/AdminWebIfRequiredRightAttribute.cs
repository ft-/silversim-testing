// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.Main.Common
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class AdminWebIfRequiredRightAttribute : Attribute
    {
        public string Right { get; private set; }

        public AdminWebIfRequiredRightAttribute(string right)
        {
            Right = right;
        }
    }
}
