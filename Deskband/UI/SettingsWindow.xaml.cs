using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TouchTogglerDeskBand.ClassLibrary;

namespace TouchTogglerDeskBand.UI
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {

        protected Func<string, SaveResult> onSave = null;

        public enum SaveResult
        {
            Success,
            Error,
            AbortedUAC
        };

        [DllImport("user32.dll")]
        static extern IntPtr LoadImage(
            IntPtr hinst,
            string lpszName,
            uint uType,
            int cxDesired,
            int cyDesired,
            uint fuLoad);

        public SettingsWindow(List<Device> deviceList, string selectedId, Func<string, SaveResult> onSave)
        {
            this.onSave = onSave;

            InitializeComponent();

            var image = LoadImage(IntPtr.Zero, "#106", 1, SystemInformation.SmallIconSize.Width, SystemInformation.SmallIconSize.Height, 0);
            var imageSource = Imaging.CreateBitmapSourceFromHIcon(image, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            shieldIcon.Source = imageSource;

            PopulateDeviceList(deviceList, selectedId);
        }

        private void PopulateDeviceList(List<Device> deviceList, string selectedId)
        {
            IDictionary<string, string> uiDeviceList = new Dictionary<string, string>();
            bool doRelevancyCheck = (selectedId == null);
            int highestRelevancy = 0;

            foreach (Device d in deviceList)
            {
                if (!uiDeviceList.ContainsKey(d.ID))
                {
                    uiDeviceList.Add(d.ID, d.Description);

                    if (doRelevancyCheck)
                    {
                        int relevancy = GetDeviceDescriptionRelevancy(d.Description);
                        if (relevancy > highestRelevancy)
                        {
                            highestRelevancy = relevancy;
                            selectedId = d.ID;
                        }
                    }
                }
            }

            DeviceListBox.ItemsSource = uiDeviceList;
            DeviceListBox.SelectedValuePath = "Key";
            DeviceListBox.DisplayMemberPath = "Value";
            DeviceListBox.SelectionMode = System.Windows.Controls.SelectionMode.Single;
            DeviceListBox.SelectedItem = new KeyValuePair<string, string>(selectedId, uiDeviceList[selectedId]);
        }

        private int GetDeviceDescriptionRelevancy(string description)
        {
            int relevancy = 0;

            if (description.Contains("touch")) relevancy++;
            if (description.Contains("screen")) relevancy++;
            if (description.Contains("touch screen")) relevancy+=2;

            return relevancy;
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            object selectedHardwareId = DeviceListBox.SelectedValue;
            if (selectedHardwareId == null)
            {
                System.Windows.Forms.MessageBox.Show("You must select a device", "Invalid Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SaveResult result = onSave(selectedHardwareId.ToString());
            if (result == SaveResult.Success)
            {
                Close();
            } else if (result == SaveResult.AbortedUAC)
            {
                System.Windows.Forms.MessageBox.Show("To save these settings you must provide administrator access", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            } else if (result == SaveResult.Error)
            {
                System.Windows.Forms.MessageBox.Show("Something went wrong! Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
