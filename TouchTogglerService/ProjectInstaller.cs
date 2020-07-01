using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
using TouchTogglerClassLibrary.Helpers;

namespace TouchTogglerService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }

        private void serviceInstaller_AfterInstall(object sender, InstallEventArgs e)
        {
            try
            {
                new ServiceController(serviceInstaller.ServiceName).Start();
            }
            catch (Exception)
            {
                // Do nothing
            }
        }

        private void serviceInstaller_BeforeUninstall(object sender, InstallEventArgs e)
        {
            try
            {
                new ServiceController(serviceInstaller.ServiceName).Stop();
            }
            catch (Exception)
            {
                // Do nothing
            }
        }

        private void serviceInstaller_AfterUninstall(object sender, InstallEventArgs e)
        {

        }

        private void serviceInstaller_BeforeInstall(object sender, InstallEventArgs e)
        {

        }
    }
}
