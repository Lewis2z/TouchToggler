using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using TouchTogglerClassLibrary;
using System.ServiceModel;
using System.Timers;
using System.IO;
using TouchTogglerClassLibrary.Helpers;
using TouchTogglerDeskBand.ClassLibrary;

namespace TouchTogglerService
{
    public partial class TouchTogglerService : ServiceBase
    {
        private ServiceConnection TTService;
        private ServiceHost serviceHost;

        public TouchTogglerService()
        {
            InitializeComponent();

            // Make the commander service connection object
            TTService = new ServiceConnection();

            // Allow for logging
            TTService.SetLogFunction(Program.Log);

            // Create the endpoint for the UI to communicate with the service
            serviceHost = new ServiceHost(
                TTService,
                new Uri[] {
                    new Uri("net.pipe://localhost/TouchTogglerService")
                }
            );
            serviceHost.AddServiceEndpoint(
                typeof(IServiceConnection),
                new NetNamedPipeBinding(),
                "Pipe"
            );

            // Done setting everything up. The service can now be started.
        }

        public void Debug()
        {
            OnStart(null);
            //Device device = TTService.GetTouchDevice();
        }

        protected override void OnStart(string[] args)
        {
            // Open the connection for the UI
            serviceHost.Open();

            // Finished starting
            Program.Log("Service is started");
        }

        protected override void OnStop()
        {
            serviceHost.Close();
            Program.Log("Service is stopped");
        }
    }
}
