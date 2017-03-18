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

using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Types;
using SilverSim.Updater;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Nini.Config;

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
        }

        [AdminWebIfRequiredRight("packages.manage")]
        void PackagesAvailableList(HttpRequest req, Map jsondata)
        {
            Map res = new Map();
            try
            {
                CoreUpdater.Instance.UpdatePackageFeed();
                AnArray pkglist = new AnArray();
                foreach(PackageDescription desc in CoreUpdater.Instance.AvailablePackages.Values)
                {
                    Map pkg = new Map();
                    pkg.Add("name", desc.Name);
                    pkg.Add("version", desc.Version);
                    pkg.Add("description", desc.Description);
                    pkglist.Add(pkg);
                }
                res.Add("packages", pkglist);
                res.Add("success", true);
            }
            catch(Exception e)
            {
                res.Add("success", false);
                res.Add("message", e.Message);
            }

            m_WebIF.SuccessResponse(req, res);
        }

        [AdminWebIfRequiredRight("packages.view")]
        void PackageUpdatesAvailable(HttpRequest req, Map jsondata)
        {
            Map res = new Map();
            try
            {
                CoreUpdater.Instance.UpdatePackageFeed();
                res.Add("available", CoreUpdater.Instance.AreUpdatesAvailable);
                res.Add("success", true);
            }
            catch (Exception e)
            {
                res.Add("success", false);
                res.Add("message", e.Message);
            }
            m_WebIF.SuccessResponse(req, res);
        }

        [AdminWebIfRequiredRight("packages.view")]
        void PackagesInstalledList(HttpRequest req, Map jsondata)
        {
            Map res = new Map();
            foreach (KeyValuePair<string, string> kvp in CoreUpdater.Instance.InstalledPackages)
            {
                res.Add(kvp.Key, kvp.Value);
            }
            m_WebIF.SuccessResponse(req, res);
        }

        [AdminWebIfRequiredRight("packages.manage")]
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
                res.Add("success", true);
            }
            catch (Exception e)
            {
                res.Add("success", false);
                res.Add("message", e.Message);
            }
            m_WebIF.SuccessResponse(req, res);
        }

        [AdminWebIfRequiredRight("packages.manage")]
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
                res.Add("success", true);
            }
            catch (Exception e)
            {
                res.Add("success", false);
                res.Add("message", e.Message);
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
