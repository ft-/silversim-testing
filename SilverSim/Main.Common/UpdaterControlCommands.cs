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

using SilverSim.Types;
using SilverSim.Updater;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SilverSim.Main.Common
{
    public static class UpdaterControlCommands
    {
        public static void RegisterCommands(ConfigurationLoader loader)
        {
            loader.CommandRegistry.AddLoadCommand("package-feed", UpdateFeed);
            loader.CommandRegistry.AddGetCommand("updates-available", CheckForUpdatesCommand);
            loader.CommandRegistry.Commands.Add("install", InstallPackageCommand);
            loader.CommandRegistry.Commands.Add("uninstall", UninstallPackageCommand);
            loader.CommandRegistry.AddShowCommand("installed-packages", ShowInstalledPackages);
        }

        static void ShowInstalledPackages(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            if (limitedToScene != UUID.Zero)
            {
                io.Write("Not supported from limited console");
            }
            else if (args[0] == "help")
            {
                io.Write("show installed-packages - Show installed packages");
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("Installed Packages\n");
                sb.Append("---------------------------------------------------------\n");
                foreach(KeyValuePair<string, string> kvp in CoreUpdater.Instance.InstalledPackages)
                {
                    sb.AppendFormat("{0}: {1}\n", kvp.Key, kvp.Value);
                }
                io.Write(sb.ToString());
            }
        }

        static void UpdateFeed(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            if(limitedToScene != UUID.Zero)
            {
                io.Write("Not supported from limited console");
            }
            else if(args[0] == "help")
            {
                io.Write("load package-feed - Updates package-feed");
            }
            else
            {
                CoreUpdater.Instance.UpdatePackageFeed();
            }
        }

        static void CheckForUpdatesCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            if (limitedToScene != UUID.Zero)
            {
                io.Write("Not supported from limited console");
            }
            else if (args[0] == "help")
            {
                io.Write("get updates-available - Show whether updates are available");
            }
            else
            {
                CoreUpdater.Instance.UpdatePackageFeed();
                if(CoreUpdater.Instance.AreUpdatesAvailable)
                {
                    io.Write("Updates are available");
                }
                else
                {
                    io.Write("Current installation is up to date");
                }
            }
        }

        static void InstallPackageCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            if (limitedToScene != UUID.Zero)
            {
                io.Write("Not supported from limited console");
            }
            else if(args.Count < 2 || args[0] == "help")
            {
                io.Write("install <package> - Installs a package");
            }
            else
            {
                CoreUpdater.Instance.InstallPackage(args[1]);
            }
        }

        static void UninstallPackageCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            if (limitedToScene != UUID.Zero)
            {
                io.Write("Not supported from limited console");
            }
            else if (args.Count < 2 || args[0] == "help")
            {
                io.Write("install <package> - Installs a package");
            }
            else if(!CoreUpdater.Instance.InstalledPackages.ContainsKey(args[1]))
            {
                io.WriteFormatted("Package {0} is not installed.", args[1]);
            }
            else
            {
                CoreUpdater.Instance.UninstallPackage(args[1]);
            }
        }
    }
}
