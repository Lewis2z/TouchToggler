using Microsoft.Win32;

namespace TouchTogglerClassLibrary.Helpers
{
    public class RegistryHelper
    {
        private static string keyPath = @"SOFTWARE\TouchToggler";
        private static string property = "HardwareID";
        public static void SaveHardwareID(string id)
        {
            RegistryKey key = Registry.LocalMachine.CreateSubKey(keyPath);

            //storing the values  
            key.SetValue(property, id, RegistryValueKind.String);
            key.Close();
        }
        public static string GetHardwareID()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(keyPath);

            if (key == null) return null;

            string id = (string)key.GetValue(property, RegistryValueKind.String);

            key.Close();

            return id;
        }
    }
}
