// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Threading;
using SilverSim.Types;
using System.Collections.Generic;
using System.Text;

namespace SilverSim.Main.Common
{
    partial class ConfigurationLoader
    {
        void ShowCachedDnsCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            if (args[0] == "help")
            {
                io.Write("Shows currently cached DNS entries");
            }
            else
            {
                StringBuilder output = new StringBuilder("Cached DNS entries:\n----------------------------------------------");
                foreach (string dns in DnsNameCache.GetCachedDnsEntries())
                {
                    output.Append("\n");
                    output.Append(dns);
                }
                io.Write(output.ToString());
            }
        }

        void RemoveCachedDnsCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
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
