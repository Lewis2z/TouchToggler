using System;
using System.Diagnostics;
using System.ServiceProcess;

namespace TouchTogglerClassLibrary.Helpers
{
    public static class ServiceHelper
    {

        public static string serviceName = "TouchTogglerService";
        public static string installUtilPath = @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\installutil.exe";

        public enum ServiceInstallerAction {
            Install,
            Uninstall
        };

        private static ServiceController GetService()
        {
            try
            {
                ServiceController service = new ServiceController(serviceName);
                ServiceControllerStatus testStatus = service.Status;
                return service;

            } catch (InvalidOperationException ex)
            {
                if (ex.InnerException.Message == "The specified service does not exist as an installed service")
                {
                    return null;
                }
            }
            return null;
        }

        public static bool IsServiceInstalled()
        {
            ServiceController service = GetService();
            return (service != null);
        }

        public static bool IsServiceRunning()
        {
            ServiceController service = GetService();
            if (service == null)
            {
                return false;
            }
            return (service.Status != ServiceControllerStatus.Stopped);
        }

        public static int installUninstallService(ServiceInstallerAction action)
        {

            string servicePath = PathHelper.GetPathToApplicationComponent(PathHelper.ApplicationComponent.ServiceExecutable);

            string arguments = "";
            if (action == ServiceInstallerAction.Install)
            {
                arguments = "\"" + servicePath + "\"";
            }
            else if (action == ServiceInstallerAction.Uninstall)
            {
                arguments = "-u \"" + servicePath + "\"";
            }
            else
            {
                throw new InvalidOperationException("Unknown installer action");
            }

            Process process = ProcessHelper.StartAdminProcess(installUtilPath, arguments);

            while (!process.WaitForExit(5000))
            {
                // Wait
            }

            if (process.ExitCode == 0)
            {
                if (IsServiceInstalled())
                {
                    StartService();
                }
            }

            return process.ExitCode;
        }

        public static void StartService(int timeoutMilliseconds = 20000)
        {
            ServiceController service = GetService();
            if (service == null) return;

            TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

            service.Start();
            service.WaitForStatus(ServiceControllerStatus.Running, timeout);
        }
        public static void StopService(int timeoutMilliseconds = 20000)
        {
            ServiceController service = GetService();
            if (service == null) return;

            TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

            service.Stop();
            service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
        }
        public static void RestartService(int timeoutMilliseconds = 20000)
        {
            StopService(timeoutMilliseconds);
            StartService(timeoutMilliseconds);
        }
    }
}
