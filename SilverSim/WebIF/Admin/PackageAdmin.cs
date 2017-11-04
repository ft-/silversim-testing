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

#pragma warning disable IDE0018
#pragma warning disable RCS1029

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Types;
using SilverSim.Updater;
using System.ComponentModel;

namespace SilverSim.WebIF.Admin
{
    [Description("WebIF Package Administration")]
    [PluginName("PackageAdmin")]
    public class PackageAdmin : IPlugin
    {
        private IAdminWebIF m_WebIF;

        public void Startup(ConfigurationLoader loader)
        {
            m_WebIF = loader.GetAdminWebIF();
            m_WebIF.ModuleNames.Add("packageadmin");

            m_WebIF.JsonMethods.Add("packages.list.installed", PackagesInstalledList);
            m_WebIF.JsonMethods.Add("packages.list.available", PackagesAvailableList);
            m_WebIF.JsonMethods.Add("package.install", PackageInstall);
            m_WebIF.JsonMethods.Add("package.uninstall", PackageUninstall);
            m_WebIF.JsonMethods.Add("packages.updates.available", PackageUpdatesAvailable);
            m_WebIF.JsonMethods.Add("packages.update.feed", PackagesUpdateFeed);
            m_WebIF.JsonMethods.Add("package.get.installed", PackageGetInstalledDetails);
            m_WebIF.JsonMethods.Add("package.get.available", PackageGetAvailableDetails);
            m_WebIF.JsonMethods.Add("package.get", PackageGetDetails);
            m_WebIF.JsonMethods.Add("packages.update.system", PackageUpdateSystem);

            m_WebIF.AutoGrantRights["packages.install"].Add("packages.view");
            m_WebIF.AutoGrantRights["packages.uninstall"].Add("packages.view");
        }

        private Map PackageDetailsToMap(PackageDescription desc) => new Map
        {
            { "name", desc.Name },
            { "version", desc.Version },
            { "description", desc.Description },
            { "license", desc.License }
        };

        [AdminWebIfRequiredRight("packages.install")]
        private void PackageUpdateSystem(HttpRequest req, Map jsondata)
        {
            try
            {
                CoreUpdater.Instance.CheckForUpdates();
            }
            catch
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotPossible);
                return;
            }
            m_WebIF.SuccessResponse(req, new Map());
        }

        [AdminWebIfRequiredRight("packages.view")]
        private void PackageGetInstalledDetails(HttpRequest req, Map jsondata)
        {
            PackageDescription desc;
            string package;
            if (!jsondata.TryGetValue("package", out package))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }
            else if(!CoreUpdater.Instance.TryGetInstalledPackageDetails(package, out desc))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
            else
            {
                m_WebIF.SuccessResponse(req, PackageDetailsToMap(desc));
            }
        }

        [AdminWebIfRequiredRight("packages.view")]
        private void PackageGetAvailableDetails(HttpRequest req, Map jsondata)
        {
            PackageDescription desc;
            string package;
            if (!jsondata.TryGetValue("package", out package))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }
            else if (!CoreUpdater.Instance.TryGetAvailablePackageDetails(package, out desc))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
            else
            {
                m_WebIF.SuccessResponse(req, PackageDetailsToMap(desc));
            }
        }

        [AdminWebIfRequiredRight("packages.view")]
        private void PackageGetDetails(HttpRequest req, Map jsondata)
        {
            PackageDescription desc;
            string package;
            if (!jsondata.TryGetValue("package", out package))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }
            else if (!CoreUpdater.Instance.TryGetPackageDetails(package, out desc))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
            else
            {
                m_WebIF.SuccessResponse(req, PackageDetailsToMap(desc));
            }
        }

        [AdminWebIfRequiredRight("packages.view")]
        private void PackagesUpdateFeed(HttpRequest req, Map jsondata)
        {
            try
            {
                CoreUpdater.Instance.UpdatePackageFeed();
            }
            catch
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotPossible);
                return;
            }
            m_WebIF.SuccessResponse(req, new Map());
        }

        [AdminWebIfRequiredRight("packages.view")]
        private void PackagesAvailableList(HttpRequest req, Map jsondata)
        {
            var res = new Map();
            try
            {
                CoreUpdater.Instance.UpdatePackageFeed();
                var pkglist = new AnArray();
                foreach(var kvp in CoreUpdater.Instance.AvailablePackages)
                {
                    var pkg = new Map
                    {
                        { "name", kvp.Key },
                        { "version", kvp.Value }
                    };
                    pkglist.Add(pkg);
                }
                res.Add("list", pkglist);
            }
            catch
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotPossible);
                return;
            }

            m_WebIF.SuccessResponse(req, res);
        }

        [AdminWebIfRequiredRight("packages.install")]
        private void PackageUpdatesAvailable(HttpRequest req, Map jsondata)
        {
            var res = new Map();
            try
            {
                CoreUpdater.Instance.UpdatePackageFeed();
                res.Add("available", CoreUpdater.Instance.AreUpdatesAvailable);
            }
            catch
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotPossible);
                return;
            }
            m_WebIF.SuccessResponse(req, res);
        }

        [AdminWebIfRequiredRight("packages.view")]
        private void PackagesInstalledList(HttpRequest req, Map jsondata)
        {
            var res = new Map();
            var pkgs = new AnArray();
            foreach (var kvp in CoreUpdater.Instance.InstalledPackages)
            {
                var pkg = new Map
                {
                    { "name", kvp.Key },
                    { "version", kvp.Value }
                };
                pkgs.Add(pkg);
            }
            res.Add("list", pkgs);
            m_WebIF.SuccessResponse(req, res);
        }

        [AdminWebIfRequiredRight("packages.install")]
        private void PackageInstall(HttpRequest req, Map jsondata)
        {
            string package;
            if (!jsondata.TryGetValue("package", out package))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }

            try
            {
                CoreUpdater.Instance.InstallPackage(package);
            }
            catch
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
                return;
            }
            m_WebIF.SuccessResponse(req, new Map());
        }

        [AdminWebIfRequiredRight("packages.uninstall")]
        private void PackageUninstall(HttpRequest req, Map jsondata)
        {
            string package;
            if (!jsondata.TryGetValue("package", out package))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }

            try
            {
                CoreUpdater.Instance.UninstallPackage(package);
            }
            catch
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotPossible);
                return;
            }
            m_WebIF.SuccessResponse(req, new Map());
        }
    }
}
