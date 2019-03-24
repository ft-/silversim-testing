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
using System.Collections.Generic;
using System.Text;

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
            loader.CommandRegistry.AddShowCommand("available-packages", ShowAvailablePackages);
            loader.CommandRegistry.AddGetCommand("updates", UpdateInstalledPackages);
            loader.CommandRegistry.AddShowCommand("package", ShowPackageDetails);
        }

        private static void ShowPackageDetails(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            PackageDescription desc;
            if (limitedToScene != UUID.Zero)
            {
                io.Write("Not supported from limited console");
            }
            else if (args[0] == "help" || args.Count < 3)
            {
                io.Write("show package <pkgname> - Show package details");
            }
            else if(CoreUpdater.Instance.TryGetPackageDetails(args[2], out desc))
            {
                var sb = new StringBuilder();
                sb.AppendFormat("Package {0}\n", desc.Name);
                sb.Append("---------------------------------------------------------\n");
                sb.AppendFormat("License: {0}\n", desc.License);
                sb.AppendFormat("Description:\n{0}\n", desc.Description);
                if(CoreUpdater.Instance.TryGetInstalledPackageDetails(args[2], out desc))
                {
                    sb.AppendFormat("Installed Version: {0}\n", desc.Version);
                }
                if (CoreUpdater.Instance.TryGetAvailablePackageDetails(args[2], out desc))
                {
                    sb.AppendFormat("Available Feed Version: {0}\n", desc.Version);
                }
                io.Write(sb.ToString());
            }
            else
            {
                io.WriteFormatted("Package {0} not found.\n", args[2]);
            }
        }

        private static void ShowAvailablePackages(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            if (limitedToScene != UUID.Zero)
            {
                io.Write("Not supported from limited console");
            }
            else if (args[0] == "help")
            {
                io.Write("show available-packages - Show installed packages");
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("Available Packages\n");
                sb.Append("---------------------------------------------------------\n");
                foreach (KeyValuePair<string, string> kvp in CoreUpdater.Instance.AvailablePackages)
                {
                    sb.AppendFormat("{0}: {1}\n", kvp.Key, kvp.Value);
                }
                io.Write(sb.ToString());
            }
        }

        private class UpdateLogRelay
        {
            private readonly CmdIO.TTY m_IO;

            public UpdateLogRelay(CmdIO.TTY io)
            {
                m_IO = io;
            }

            public void LogEvent(CoreUpdater.LogType type, string msg)
            {
                m_IO.Write(msg);
            }
        }

        private static void UpdateInstalledPackages(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            if (limitedToScene != UUID.Zero)
            {
                io.Write("Not supported from limited console");
            }
            else if (args[0] == "help")
            {
                io.Write("get updates - Update installed packages");
            }
            else
            {
                var relay = new UpdateLogRelay(io);
                CoreUpdater.Instance.OnUpdateLog += relay.LogEvent;
                try
                {
                    CoreUpdater.Instance.CheckForUpdates();
                }
                finally
                {
                    CoreUpdater.Instance.OnUpdateLog -= relay.LogEvent;
                }
            }
        }

        private static void ShowInstalledPackages(List<string> args, CmdIO.TTY io, UUID limitedToScene)
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
                var sb = new StringBuilder();
                sb.Append("Installed Packages\n");
                sb.Append("---------------------------------------------------------\n");
                foreach(KeyValuePair<string, string> kvp in CoreUpdater.Instance.InstalledPackages)
                {
                    sb.AppendFormat("{0}: {1}\n", kvp.Key, kvp.Value);
                }
                io.Write(sb.ToString());
            }
        }

        private static void UpdateFeed(List<string> args, CmdIO.TTY io, UUID limitedToScene)
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

        private static void CheckForUpdatesCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
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

        private static void InstallPackageCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
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

        private static void UninstallPackageCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            if (limitedToScene != UUID.Zero)
            {
                io.Write("Not supported from limited console");
            }
            else if (args.Count < 2 || args[0] == "help")
            {
                io.Write("uninstall <package> - Uninstalls a package");
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
