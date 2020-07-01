using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using TouchTogglerClassLibrary;
using TouchTogglerClassLibrary.Helpers;

namespace TouchTogglerService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {

            // Alternative functions
            if (args.Length > 0)
            {
                int exitCode = 0;
                switch (args[0])
                {
                    case "install":
                        exitCode = ServiceHelper.installUninstallService(ServiceHelper.ServiceInstallerAction.Install);
                        break;
                    case "uninstall":
                        exitCode = ServiceHelper.installUninstallService(ServiceHelper.ServiceInstallerAction.Uninstall);
                        break;
                    case "start":
                        ServiceHelper.StartService();
                        break;
                    case "stop":
                        ServiceHelper.StopService();
                        break;
                    case "settouchdeviceid":
                        SaveTouchDeviceID(args[1]);
                        break;
                    case "debug":
                        TouchTogglerService service = new TouchTogglerService();
                        service.Debug();
                        break;
                }
                Environment.Exit(exitCode);
            }

            // Normal function

            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
            new TouchTogglerService()
            };
            ServiceBase.Run(ServicesToRun);
        }
        public static void Log(string message)
        {

            message = "[" + DateTime.Now + "] " + message;

            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(filepath))
            {
                // Create a file to write to.   
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(message);
                }
            }
        }

        private static bool SaveTouchDeviceID(string base64HardwareID)
        {
            string hardwareId = Strings.Base64Decode(base64HardwareID);
            try
            {
                RegistryHelper.SaveHardwareID(hardwareId);
                return true;
            } catch (Exception ex)
            {
                Log("Error saving hardware ID to registry: " + ex.Message);
            }
            return false;
        }
    }
}
