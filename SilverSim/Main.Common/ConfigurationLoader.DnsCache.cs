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

using SilverSim.Threading;
using SilverSim.Types;
using System.Collections.Generic;
using System.Text;

namespace SilverSim.Main.Common
{
    partial class ConfigurationLoader
    {
        private void ShowCachedDnsCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            if (args[0] == "help")
            {
                io.Write("Shows currently cached DNS entries");
            }
            else
            {
                var output = new StringBuilder("Cached DNS entries:\n----------------------------------------------");
                foreach (string dns in DnsNameCache.GetCachedDnsEntries())
                {
                    output.Append("\n");
                    output.Append(dns);
                }
                io.Write(output.ToString());
            }
        }

        private void RemoveCachedDnsCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            if (args[0] == "help" || args.Count < 3)
            {
                io.Write("delete cacheddns <host>\nRemoves a DNS cache entry");
            }
            else
            {
                if (DnsNameCache.RemoveCachedDnsEntry(args[2]))
                {
                    io.WriteFormatted("DNS Entry {0} removed", args[2]);
                }
                else
                {
                    io.WriteFormatted("DNS Entry {0} not found", args[2]);
                }
            }
        }
    }
}
