using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using TouchTogglerClassLibrary.Exceptions;
using TouchTogglerClassLibrary.Helpers;

namespace TouchTogglerDeskBand
{
    [RunInstaller(true)]
    public partial class DeskBandInstaller : System.Configuration.Install.Installer
    {
        public DeskBandInstaller()
        {
            InitializeComponent();

            AfterInstall += new InstallEventHandler(DeskBandInstaller_AfterInstall);
            BeforeUninstall += new InstallEventHandler(DeskBandInstaller_BeforeUninstall);
            AfterUninstall += new InstallEventHandler(DeskBandInstaller_AfterUninstall);
        }

        private void DeskBandInstaller_AfterInstall(object sender, InstallEventArgs e)
        {
            //throw new NotImplementedException();
            DeskBandInstall();
            //RestartExplorer();
        }

        private void DeskBandInstaller_BeforeUninstall(object sender, InstallEventArgs e)
        {
            try
            {
                ServiceHelper.StopService();
            } catch (Exception)
            {
                // Do nothing
            }

            DeskBandInstall(true);

            RestartExplorer();
        }

        private void RestartExplorer()
        {
            Process p = new Process();
            foreach (Process exe in Process.GetProcesses())
            {
                if (exe.ProcessName == "explorer")
                {
                    string explorerUser = GetProcessOwner(exe.Id);
                    exe.Kill();
                    //ProcessHelper.StartProcessAs(explorerUser, "explorer.exe");
                }
            }
        }

        private void DeskBandInstaller_AfterUninstall(object sender, InstallEventArgs e)
        {
        }

        private void DeskBandInstall(bool uninstall = false)
        {

            string deskBandInstallerPath = @"%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\regasm.exe";
            string deskBandPath = PathHelper.GetPathToApplicationComponent(PathHelper.ApplicationComponent.DeskBand);

            string installArgs = "/nologo /codebase";
            if (uninstall) installArgs = "/unregister";

            Process process = ProcessHelper.StartAdminProcess(deskBandInstallerPath, installArgs + " \"" + deskBandPath + "\"");

            while (!process.WaitForExit(5000))
            {
                // Wait
            }

        }
        private string GetProcessOwner(int processId)
        {
            string query = "Select * From Win32_Process Where ProcessID = " + processId;
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();

            foreach (ManagementObject obj in processList)
            {
                string[] argList = new string[] { string.Empty, string.Empty };
                int returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
                if (returnVal == 0)
                {
                    // return DOMAIN\user
                    return argList[1] + "\\" + argList[0];
                }
            }

            return "";
        }
    }
}
