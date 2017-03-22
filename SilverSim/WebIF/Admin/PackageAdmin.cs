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

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Types;
using SilverSim.Updater;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.WebIF.Admin
{
    [Description("WebIF Package Administration")]
    public class PackageAdmin : IPlugin
    {
        IAdminWebIF m_WebIF;

        public PackageAdmin()
        {

        }

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

        Map PackageDetailsToMap(PackageDescription desc)
        {
            Map map = new Map();
            map.Add("name", desc.Name);
            map.Add("version", desc.Version);
            map.Add("description", desc.Description);
            map.Add("license", desc.License);
            return map;
        }

        [AdminWebIfRequiredRight("packages.install")]
        void PackageUpdateSystem(HttpRequest req, Map jsondata)
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
        void PackageGetInstalledDetails(HttpRequest req, Map jsondata)
        {
            PackageDescription desc;
            if (!jsondata.ContainsKey("package"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }
            else if(!CoreUpdater.Instance.TryGetInstalledPackageDetails(jsondata["package"].ToString(), out desc))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
            else
            {
                m_WebIF.SuccessResponse(req, PackageDetailsToMap(desc));
            }
        }

        [AdminWebIfRequiredRight("packages.view")]
        void PackageGetAvailableDetails(HttpRequest req, Map jsondata)
        {
            PackageDescription desc;
            if (!jsondata.ContainsKey("package"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }
            else if (!CoreUpdater.Instance.TryGetAvailablePackageDetails(jsondata["package"].ToString(), out desc))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
            else
            {
                m_WebIF.SuccessResponse(req, PackageDetailsToMap(desc));
            }
        }

        [AdminWebIfRequiredRight("packages.view")]
        void PackageGetDetails(HttpRequest req, Map jsondata)
        {
            PackageDescription desc;
            if (!jsondata.ContainsKey("package"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }
            else if (!CoreUpdater.Instance.TryGetPackageDetails(jsondata["package"].ToString(), out desc))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
            else
            {
                m_WebIF.SuccessResponse(req, PackageDetailsToMap(desc));
            }
        }

        [AdminWebIfRequiredRight("packages.view")]
        void PackagesUpdateFeed(HttpRequest req, Map jsondata)
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
        void PackagesAvailableList(HttpRequest req, Map jsondata)
        {
            Map res = new Map();
            try
            {
                CoreUpdater.Instance.UpdatePackageFeed();
                AnArray pkglist = new AnArray();
                foreach(KeyValuePair<string, string> kvp in CoreUpdater.Instance.AvailablePackages)
                {
                    Map pkg = new Map();
                    pkg.Add("name", kvp.Key);
                    pkg.Add("version", kvp.Value);
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
        void PackageUpdatesAvailable(HttpRequest req, Map jsondata)
        {
            Map res = new Map();
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
        void PackagesInstalledList(HttpRequest req, Map jsondata)
        {
            Map res = new Map();
            AnArray pkgs = new AnArray();
            foreach (KeyValuePair<string, string> kvp in CoreUpdater.Instance.InstalledPackages)
            {
                Map pkg = new Map();
                pkg.Add("name", kvp.Key);
                pkg.Add("version", kvp.Value);
                pkgs.Add(pkg);
            }
            res.Add("list", pkgs);
            m_WebIF.SuccessResponse(req, res);
        }

        [AdminWebIfRequiredRight("packages.install")]
        void PackageInstall(HttpRequest req, Map jsondata)
        {
            if (!jsondata.ContainsKey("package"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }

            Map res = new Map();
            try
            {
                CoreUpdater.Instance.InstallPackage(jsondata["package"].ToString());
            }
            catch
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
                return;
            }
            m_WebIF.SuccessResponse(req, res);
        }

        [AdminWebIfRequiredRight("packages.uninstall")]
        void PackageUninstall(HttpRequest req, Map jsondata)
        {
            if (!jsondata.ContainsKey("package"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }

            Map res = new Map();
            try
            {
                CoreUpdater.Instance.UninstallPackage(jsondata["package"].ToString());
            }
            catch
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotPossible);
                return;
            }
            m_WebIF.SuccessResponse(req, res);
        }
    }

    [PluginName("PackageAdmin")]
    public class Factory : IPluginFactory
    {
        public Factory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new PackageAdmin();
        }
    }
}
