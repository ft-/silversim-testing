// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using System;
using System.Net;

namespace SilverSim.Types
{
    public static class IPEndPointHelpers
    {
        public static IPEndPoint CreateIPEndPoint(string endPoint)
        {
            IPEndPoint ep;
            if(!TryCreateIPEndPoint(endPoint, out ep))
            {
                throw new ArgumentException(nameof(endPoint));
            }
            return ep;
        }

        public static bool TryCreateIPEndPoint(string endPoint, out IPEndPoint ep)
        {
            int colonIndex = endPoint.LastIndexOf(':');
            ep = default(IPEndPoint);
            if(colonIndex < 0)
            {
                return false;
            }

            IPAddress ip;
            if(!IPAddress.TryParse(endPoint.Substring(0, colonIndex), out ip))
            {
                return false;
            }

            int port;
            if(!int.TryParse(endPoint.Substring(colonIndex + 1), out port))
            {
                return false;
            }
            ep = new IPEndPoint(ip, port);
            return true;
        }
    }
}
