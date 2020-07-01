using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TouchTogglerDeskBand.ClassLibrary;

namespace TouchTogglerClassLibrary.Helpers
{
    class HardwareManager
    {

        const uint DIF_PROPERTYCHANGE = 0x12;
        const uint DICS_ENABLE = 1;
        const uint DICS_DISABLE = 2;  // disable device
        const uint DICS_FLAG_GLOBAL = 1; // not profile-specific
        const uint DIGCF_ALLCLASSES = 4;
        const uint DIGCF_PRESENT = 2;
        const uint ERROR_NO_MORE_ITEMS = 259;
        const uint ERROR_ELEMENT_NOT_FOUND = 1168;

        static DEVPROPKEY DEVPKEY_Device_DeviceDesc;
        static DEVPROPKEY DEVPKEY_Device_HardwareIds;
        static DEVPROPKEY DEVPKEY_Device_Class;
        static DEVPROPKEY DEVPKEY_Device_ProblemCode;
        static DEVPROPKEY DEVPKEY_Device_DevNodeStatus;

        [StructLayout(LayoutKind.Sequential)]
        struct SP_CLASSINSTALL_HEADER
        {
            public UInt32 cbSize;
            public UInt32 InstallFunction;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct SP_PROPCHANGE_PARAMS
        {
            public SP_CLASSINSTALL_HEADER ClassInstallHeader;
            public UInt32 StateChange;
            public UInt32 Scope;
            public UInt32 HwProfile;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct SP_DEVINFO_DATA
        {
            public UInt32 cbSize;
            public Guid classGuid;
            public UInt32 devInst;
            public UInt32 reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct DEVPROPKEY
        {
            public Guid fmtid;
            public UInt32 pid;
        }

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern IntPtr SetupDiGetClassDevsW(
            [In] ref Guid ClassGuid,
            [MarshalAs(UnmanagedType.LPWStr)]
    string Enumerator,
            IntPtr parent,
            UInt32 flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern bool SetupDiDestroyDeviceInfoList(IntPtr handle);

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern bool SetupDiEnumDeviceInfo(IntPtr deviceInfoSet,
            UInt32 memberIndex,
            [Out] out SP_DEVINFO_DATA deviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern bool SetupDiSetClassInstallParams(
            IntPtr deviceInfoSet,
            [In] ref SP_DEVINFO_DATA deviceInfoData,
            [In] ref SP_PROPCHANGE_PARAMS classInstallParams,
            UInt32 ClassInstallParamsSize);

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern bool SetupDiChangeState(
            IntPtr deviceInfoSet,
            [In] ref SP_DEVINFO_DATA deviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern bool SetupDiGetDevicePropertyW(
                IntPtr deviceInfoSet,
                [In] ref SP_DEVINFO_DATA DeviceInfoData,
                [In] ref DEVPROPKEY propertyKey,
                [Out] out UInt32 propertyType,
                IntPtr propertyBuffer,
                UInt32 propertyBufferSize,
                out UInt32 requiredSize,
                UInt32 flags);

        static HardwareManager()
        {
            HardwareManager.DEVPKEY_Device_DeviceDesc = new DEVPROPKEY()
            {
                fmtid = new Guid(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0),
                pid = 2
            };

            DEVPKEY_Device_HardwareIds = new DEVPROPKEY()
            {
                fmtid = new Guid(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0),
                pid = 3
            };

            DEVPKEY_Device_Class = new DEVPROPKEY()
            {
                fmtid = new Guid(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0),
                pid = 9
            };

            DEVPKEY_Device_DevNodeStatus = new DEVPROPKEY()
            {
                fmtid = new Guid(0x4340a6c5, 0x93fa, 0x4706, 0x97, 0x2c, 0x7b, 0x64, 0x80, 0x08, 0xa5, 0xa7),
                pid = 2
            };

            DEVPKEY_Device_ProblemCode = new DEVPROPKEY()
            {
                fmtid = new Guid(0x4340a6c5, 0x93fa, 0x4706, 0x97, 0x2c, 0x7b, 0x64, 0x80, 0x08, 0xa5, 0xa7),
                pid = 3
            };
        }

        public static List<Device> ListDevices(Func<Device, bool> filter = null)
        {
            List<Device> list = new List<Device>();
            ListDevicesAdvanced(
                action: (IntPtr info, SP_DEVINFO_DATA devdata, Device device) =>
                {
                    if (!list.Contains(device)) list.Add(device);
                },
                filter
            );
            return list;
        }
        private static void ListDevicesAdvanced(Action<IntPtr, SP_DEVINFO_DATA, Device> action, Func<Device, bool> filter = null)
        {
            IntPtr info = IntPtr.Zero;
            Guid NullGuid = Guid.Empty;
            try
            {
                info = SetupDiGetClassDevsW(
                    ref NullGuid,
                    null,
                    IntPtr.Zero,
                    DIGCF_ALLCLASSES);
                CheckError("SetupDiGetClassDevs");

                // Get first device matching device criterion.
                for (uint i = 0; ; i++)
                {
                    SP_DEVINFO_DATA devdata = new SP_DEVINFO_DATA();
                    devdata.cbSize = (UInt32)Marshal.SizeOf(devdata);

                    SetupDiEnumDeviceInfo(info,
                        i,
                        out devdata);
                    // if no items match filter, throw
                    if (Marshal.GetLastWin32Error() == ERROR_NO_MORE_ITEMS)
                    {
                        break;
                    }

                    string devicepath = GetStringPropertyForDevice(info, devdata, DEVPKEY_Device_HardwareIds);
                    if (devicepath == null) continue; // Skip

                    string deviceDescription = GetStringPropertyForDevice(info, devdata, DEVPKEY_Device_DeviceDesc);
                    string deviceClass = GetStringPropertyForDevice(info, devdata, DEVPKEY_Device_Class);
                    int deviceProblemCode = GetIntPropertyForDevice(info, devdata, DEVPKEY_Device_ProblemCode);

                    Device hwItem = new Device() {
                        ID = devicepath.ToUpperInvariant(),
                        Description = deviceDescription,
                        Class = deviceClass,
                        Status = deviceProblemCode,
                        Enabled = (deviceProblemCode == 0) // 22 = disabled
                    };

                    if (filter != null && !filter(hwItem))
                    {
                        continue;
                    }

                    if (action != null)
                    {
                        action(info, devdata, hwItem);
                    }
                }
            }
            finally
            {
                if (info != IntPtr.Zero)
                    SetupDiDestroyDeviceInfoList(info);
            }
        }
        public static void DisableDeviceByID(string hardwareId, bool disable = true)
        {
            ListDevicesAdvanced(
                action: (IntPtr info, SP_DEVINFO_DATA devdata, Device device) =>
                {
                    SP_CLASSINSTALL_HEADER header = new SP_CLASSINSTALL_HEADER();
                    header.cbSize = (UInt32)Marshal.SizeOf(header);
                    header.InstallFunction = DIF_PROPERTYCHANGE;

                    SP_PROPCHANGE_PARAMS propchangeparams = new SP_PROPCHANGE_PARAMS();
                    propchangeparams.ClassInstallHeader = header;
                    propchangeparams.StateChange = disable ? DICS_DISABLE : DICS_ENABLE;
                    propchangeparams.Scope = DICS_FLAG_GLOBAL;
                    propchangeparams.HwProfile = 0;

                    SetupDiSetClassInstallParams(info,
                        ref devdata,
                        ref propchangeparams,
                        (UInt32)Marshal.SizeOf(propchangeparams));
                    CheckError("SetupDiSetClassInstallParams");

                    SetupDiChangeState(
                        info,
                        ref devdata);
                    CheckError("SetupDiChangeState");
                },
                filter: device => device.ID.Contains(hardwareId)
            );
        }

        private static void CheckError(string message, int lasterror = -1)
        {

            int code = lasterror == -1 ? Marshal.GetLastWin32Error() : lasterror;
            if (code != 0)
                throw new ApplicationException(
                    String.Format("Error disabling hardware device (Code {0}): {1}",
                        code, message));
        }

        private static byte[] GetPropertyForDevice(IntPtr info, SP_DEVINFO_DATA devdata,
            DEVPROPKEY key)
        {
            uint proptype, outsize;
            IntPtr buffer = IntPtr.Zero;
            try
            {
                uint buflen = 512;
                buffer = Marshal.AllocHGlobal((int)buflen);
                SetupDiGetDevicePropertyW(
                    info,
                    ref devdata,
                    ref key,
                    out proptype,
                    buffer,
                    buflen,
                    out outsize,
                    0);
                byte[] lbuffer = new byte[outsize];
                Marshal.Copy(buffer, lbuffer, 0, (int)outsize);
                int errcode = Marshal.GetLastWin32Error();
                if (errcode == ERROR_ELEMENT_NOT_FOUND) return null;
                CheckError("SetupDiGetDeviceProperty", errcode);
                return lbuffer;
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                    Marshal.FreeHGlobal(buffer);
            }
        }
        private static string GetStringPropertyForDevice(IntPtr info, SP_DEVINFO_DATA devdata, DEVPROPKEY key)
        {
            byte[] data = GetPropertyForDevice(info, devdata, key);
            return (data != null) ? Encoding.Unicode.GetString(data) : null;
        }
        private static int GetIntPropertyForDevice(IntPtr info, SP_DEVINFO_DATA devdata, DEVPROPKEY key)
        {
            byte[] data = GetPropertyForDevice(info, devdata, key);
            return (data != null) ? BitConverter.ToInt32(data, 0) : -1;
        }
    }
}
