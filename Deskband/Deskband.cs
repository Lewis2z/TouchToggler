using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using CSDeskBand;
using CSDeskBand.ContextMenu;
using TouchTogglerClassLibrary;
using TouchTogglerClassLibrary.Exceptions;
using TouchTogglerClassLibrary.Helpers;
using TouchTogglerDeskBand.ClassLibrary;
using TouchTogglerDeskBand.UI;
using MessageBox = System.Windows.Forms.MessageBox;

namespace TouchTogglerDeskBand
{
    [ComVisible(true)]
    [Guid("AA01ACB3-6CCC-497C-9CE6-9211F2EDFC10")]
    [CSDeskBandRegistration(Name = "Touch Toggler", ShowDeskBand = true)]
    public class Deskband : CSDeskBandWpf
    {
        private IServiceConnection TTService = null;
        System.Timers.Timer serviceConnectionTicker = new System.Timers.Timer();

        protected override UIElement UIElement => new UserControl1(UserControlLoaded);
        private UserControl1 deskbandControl = null;

        public Deskband()
        {
            // Set the deskband size
            Options.MinHorizontalSize = new DeskBandSize(90, -1);

            // Set the connection ticker to check every 10 seconds
            serviceConnectionTicker.Interval = 10000;
        }

        private void UserControlLoaded(UserControl1 control)
        {
            // The user control should be set here for easy accessibility
            // This leaves the UIElement variable alone to be dealt with by the DeskBand parent functionality
            deskbandControl = control;

            // Provide the function in this class that performs the device enable/disable action
            deskbandControl.SetEnabledDisabledDeviceFunction(EnableDisableDevice);

            // Provide the function to show the settings window
            deskbandControl.SetShowSettingsWindowFunction(ShowSettingsWindow);

            // Initialise the connection to the Windows service
            if (InitialiseServiceConnection(false))
            {
                // Get the selected touch screen device hardware ID
                try
                {
                    string deviceHardwareId = TTService.GetSelectedHardwareID();
                    if (deviceHardwareId == null)
                    {
                        // If no ID is specified, show the settings window
                        ShowSettingsWindow();
                    }
                }
                catch (CommunicationException)
                {
                    // Do nothing
                }
            }

            // Set up the watcher for hardware changes
            WqlEventQuery query = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent");
            ManagementEventWatcher watcher = new ManagementEventWatcher(query);
            watcher.EventArrived += new EventArrivedEventHandler(HardwareChangeEvent);
            watcher.Start();

            // Set the serivce connection ticker event
            serviceConnectionTicker.Elapsed += (object source, System.Timers.ElapsedEventArgs e2) =>
            {
                if (TTService != null)
                {
                    // If a connection exists
                    try
                    {
                        // Test the connection
                        TTService.TestServiceConnection();
                    }
                    catch (CommunicationException)
                    {
                        // Retry the connection
                        ServiceCommunicationFailure(false);
                    }
                }
                else
                {
                    // Attempt to make a connection
                    InitialiseServiceConnection(allowUserAdminActionInput: false);
                }
            };
        }

        private bool InitialiseServiceConnection(bool allowUserAdminActionInput = true)
        {
            // Reset things
            TTService = null;
            deskbandControl.UpdateUIImage(inoperableOverride: true, UserControl1.EnableDisableButtonToolTips.ServiceNotConnected);

            // Attempt to form a connection to the installed Windows service
            if (ServiceHelper.IsServiceInstalled() && ServiceHelper.IsServiceRunning())
            {
                try
                {
                    // Establish a connection to the service
                    TTService = ServiceConnectionClient.Start(
                        onClose: (object sender, EventArgs e) => deskbandControl.UpdateUIImage(inoperableOverride: true, UserControl1.EnableDisableButtonToolTips.ServiceNotConnected)
                    );

                    serviceConnectionTicker.Start();

                    // Once a connection is established, immediately get the device status
                    GetTouchDeviceStatus();

                    // Initialisation success
                    return true;
                }
                catch (System.ServiceModel.EndpointNotFoundException)
                {
                    // The Windows service is detected to be running but it is not functioning correctly
                    MessageBox.Show("There was an error connecting to the installed service.", "Touch Toggle Service Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;
            }
            else
            {
                // The user may need to install or start the Windows service
                if (allowUserAdminActionInput) return GetUserAdminActionInput();

                // Return initialisation failure
                return false;
            }
        }


        private bool GetUserAdminActionInput()
        {
            // Begin message
            string message = "To enable and disable the touch screen this process requires administrator access.\n\n";

            if (!ServiceHelper.IsServiceInstalled())
            {
                // The Windows service is not installed

                message += "The Windows service will be installed.\n";
                MessageBox.Show(message, "Touch Toggler", MessageBoxButtons.OK, MessageBoxIcon.Information);

                string servicePath = PathHelper.GetPathToApplicationComponent(PathHelper.ApplicationComponent.ServiceExecutable);
                Process process;
                try
                {
                    process = ProcessHelper.StartAdminProcess(servicePath, "install");
                }
                catch (ProcessCancelledException)
                {
                    // User refused UAC
                    return false;
                }
                catch (FIleNotFoundException)
                {
                    MessageBox.Show("Unable to find the service application", "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                while (!process.WaitForExit(5000))
                {
                    // Wait
                }

                if (process.ExitCode != 0)
                {
                    MessageBox.Show("There was a problem starting the Windows service.\nExit code " + process.ExitCode, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            else
            {
                // The Windows service is not running

                message += "The Windows service is installed but it is not running. The service will attempt to be started.";
                MessageBox.Show(message, "Touch Toggler", MessageBoxButtons.OK, MessageBoxIcon.Information);

                string servicePath = PathHelper.GetPathToApplicationComponent(PathHelper.ApplicationComponent.ServiceExecutable);
                Process process;
                try
                {
                    process = ProcessHelper.StartAdminProcess(servicePath, "start");
                }
                catch (ProcessCancelledException)
                {
                    // User refused UAC
                    return false;
                }
                catch (FIleNotFoundException)
                {
                    MessageBox.Show("Unable to find the service application", "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                while (!process.WaitForExit(5000))
                {
                    // Wait
                }

                if (process.ExitCode != 0)
                {
                    MessageBox.Show("There was a problem starting the Windows service.\nExit code " + process.ExitCode, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            // Restart the initialisation process
            return InitialiseServiceConnection();
        }
        private bool ServiceCommunicationFailure(bool allowUserAdminActionInput = true)
        {
            // Stop the serivice connection timer so the issue won't be compounded
            serviceConnectionTicker.Stop();

            // Attempt a number of tries before giving up. This helps with brief disruptions in communication without closing the client
            int tries = 5;
            while (tries > 0)
            {
                try
                {
                    if (TTService == null)
                    {
                        if (!InitialiseServiceConnection(allowUserAdminActionInput: false))
                        {
                            break;
                        }
                    }

                    // Try
                    TTService.TestServiceConnection();

                    GetTouchDeviceStatus();

                    serviceConnectionTicker.Start();

                    // Return true to allow the caller to try again
                    return true;

                }
                catch (CommunicationException)
                {
                    // If there is one more try left after this, attempt to start a new connection.
                    // If it fails, it can be tried again. This shouldn't be done on the first try because
                    // communication blips are common and can be rectified by retrying without re-initialisation.
                    if (tries == 2)
                    {
                        // Start a new connection and proceed to re-test below
                        try
                        {
                            InitialiseServiceConnection(allowUserAdminActionInput);
                        }
                        catch (System.ServiceModel.CommunicationException)
                        {
                            // Do nothing
                        }
                    }
                }

                // Delay the re-try for time for the connection to be re-established
                Thread.Sleep(1000);

                tries--;
            }

            // Start the timer again so it will re-test the connection
            serviceConnectionTicker.Start();

            // Show that the link isn't working
            deskbandControl.UpdateUIImage(inoperableOverride: true, UserControl1.EnableDisableButtonToolTips.ServiceNotConnected);

            return false;
        }


        private void GetTouchDeviceStatus()
        {
            // By default
            deskbandControl.deviceIsEnabled = true;
            Device touchDevice = null;

            while (true)
            {
                try
                {
                    string deviceHardwareId = TTService.GetSelectedHardwareID();
                    if (deviceHardwareId == null)
                    {
                        deskbandControl.UpdateUIImage(inoperableOverride: true, UserControl1.EnableDisableButtonToolTips.DeviceNotSelected);
                        return;
                    }
                    // Get the device info
                    touchDevice = TTService.GetTouchDevice();
                    break;
                }
                catch (CommunicationException)
                {
                    if (!ServiceCommunicationFailure(false)) return;
                }
            }

            if (touchDevice != null)
            {
                // If the device is found (connected) update the UI with the device status
                deskbandControl.deviceIsEnabled = touchDevice.Enabled;
                deskbandControl.UpdateUIImage();
            }
            else
            {
                deskbandControl.UpdateUIImage(inoperableOverride: true, UserControl1.EnableDisableButtonToolTips.DeviceNotConnected);
            }
        }

        private void HardwareChangeEvent(object sender, EventArrivedEventArgs e)
        {
            // A hardware change is detected so if the service connection is established, get the device status for the UI
            if (TTService != null)
            {
                GetTouchDeviceStatus();
            }
        }


        private bool EnableDisableDevice(bool enabled)
        {
            while (true)
            {
                try
                {

                    // Establish a new connection to the Windows service if there isn't one available to complete the action
                    if (TTService == null)
                    {
                        if (InitialiseServiceConnection())
                        {
                            continue; // retry
                        }
                        else
                        {
                            return false; // failed, abort action
                        }
                    }

                    // Perform the enable/disable action
                    string errorMsg = TTService.EnableDevice(enabled);

                    // If not success
                    if (errorMsg != "")
                    {
                        string userMessage = "Unknown error";

                        if (errorMsg.Contains("No device found matching filter"))
                        {
                            userMessage = "Device not found";
                        }

                        // Show the user a message
                        MessageBox.Show(userMessage, "Touch Toggler Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false; // failed to make device change
                    }

                    // Success
                    return true;
                }
                catch (CommunicationException)
                {
                    if (!ServiceCommunicationFailure()) return false;
                }
            }
        }

        private void ShowSettingsWindow()
        {
            List<Device> HIDList = new List<Device>();
            string currentHardwareId = null;

            while (true)
            {
                try
                {

                    // Establish a new connection to the Windows service if there isn't one available to complete the action
                    if (TTService == null)
                    {
                        if (InitialiseServiceConnection())
                        {
                            continue; // retry
                        }
                        else
                        {
                            return; // failed, abort action
                        }
                    }

                    // Perform the enable/disable action
                    HIDList = TTService.GetHumanInterfaceDevices();

                    // Get the currently selected hardware ID
                    currentHardwareId = TTService.GetSelectedHardwareID();

                    break;
                }
                catch (CommunicationException)
                {
                    if (!ServiceCommunicationFailure()) return;
                }
            }

            SettingsWindow settingsWindow = new SettingsWindow(HIDList, currentHardwareId, (hardwareId) => {


                string servicePath = PathHelper.GetPathToApplicationComponent(PathHelper.ApplicationComponent.ServiceExecutable);
                Process process;
                try
                {
                    process = ProcessHelper.StartAdminProcess(servicePath, "settouchdeviceid \"" + Strings.Base64Encode(hardwareId) + "\"");
                }
                catch (ProcessCancelledException)
                {
                    return SettingsWindow.SaveResult.AbortedUAC;
                }
                catch (FIleNotFoundException)
                {
                    return SettingsWindow.SaveResult.Error;
                }

                while (!process.WaitForExit(5000))
                {
                    // Wait
                }

                if (process.ExitCode != 0)
                {
                    return SettingsWindow.SaveResult.Error;
                }

                GetTouchDeviceStatus();

                return SettingsWindow.SaveResult.Success;
            });

            settingsWindow.Focus();
            settingsWindow.ShowDialog();
        }

    }
}
