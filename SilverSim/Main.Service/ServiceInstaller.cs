// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace SilverSim.Main.Service
{
    [RunInstaller(true)]
    public class MainServiceInstaller : Installer
    {
        public MainServiceInstaller()
        {
            ServiceProcessInstaller serviceProcessInstaller = new ServiceProcessInstaller();
            ServiceInstaller serviceInstaller = new ServiceInstaller();

            // Service Account Information
            serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
            serviceProcessInstaller.Username = null;
            serviceProcessInstaller.Password = null;

            // Service Information
            serviceInstaller.DisplayName = MainService.SERVICE_NAME;
            serviceInstaller.StartType = ServiceStartMode.Automatic;
            serviceInstaller.ServiceName = MainService.SERVICE_NAME;

            Installers.Add(serviceProcessInstaller);
            Installers.Add(serviceInstaller);
        }
    }
}
