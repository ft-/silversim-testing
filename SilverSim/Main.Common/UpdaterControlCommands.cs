// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
