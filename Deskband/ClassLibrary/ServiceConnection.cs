using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Windows.Documents;
using TouchTogglerClassLibrary.Helpers;
using TouchTogglerDeskBand.ClassLibrary;

namespace TouchTogglerClassLibrary
{
    [ServiceContract]
    public interface IServiceConnection
    {
        [OperationContract]
        bool TestServiceConnection();

        [OperationContract]
        string GetSelectedHardwareID();

        [OperationContract]
        string EnableDevice(bool enabled = true);

        [OperationContract]
        Device GetTouchDevice();

        [OperationContract]
        List<Device> GetHumanInterfaceDevices();
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class ServiceConnection : IServiceConnection
    {
        private Action<string> logFunction;
        private string selectedHardwareID = null;
        //private string selectedHardwareID = @"HID\ELAN2514&COL01";

        public bool TestServiceConnection()
        {
            return true;
        }
        public string GetSelectedHardwareID()
        {
            selectedHardwareID = null;

            try
            {
                selectedHardwareID = RegistryHelper.GetHardwareID();
            } catch (Exception ex)
            {
                logFunction?.Invoke("Error retrieving hardware ID from registry");
            }

            return selectedHardwareID;
        }
        public void SetLogFunction(Action<string> logFunction)
        {
            this.logFunction = logFunction;
        }
        public string EnableDevice(bool enabled = true)
        {
            GetSelectedHardwareID();

            if (selectedHardwareID == null)
            {
                return "Touch screen device not selected";
            }

            try
            {
                HardwareManager.DisableDeviceByID(selectedHardwareID, !enabled);
            }
            catch (ApplicationException ex)
            {
                return ex.Message;
            }

            return "";
        }
        public Device GetTouchDevice()
        {
            GetSelectedHardwareID();

            if (selectedHardwareID == null) return null;

            List<Device> deviceList = HardwareManager.ListDevices(
                device => device.ID.Contains(selectedHardwareID)
            );
            if (deviceList.Count > 0)
            {
                return deviceList.First<Device>();
            } else
            {
                return null;
            }
        }
        public List<Device> GetHumanInterfaceDevices()
        {
            return HardwareManager.ListDevices(device => {
                return (device.Class != null) ? device.Class.Contains(@"HIDClass") : false;
            });
        }
    }
}
