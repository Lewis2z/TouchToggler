using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Management;
using System.ServiceModel;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using TouchTogglerClassLibrary;
using TouchTogglerClassLibrary.Exceptions;
using TouchTogglerClassLibrary.Helpers;
using TouchTogglerDeskBand.ClassLibrary;
using MessageBox = System.Windows.Forms.MessageBox;

namespace TouchTogglerDeskBand.UI
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class UserControl1 : System.Windows.Controls.UserControl
    {
        //private IServiceConnection TTService = null;

        public enum EnableDisableButtonToolTips {
            [Description("Disable the touch screen")]
            ClickToDisable,
            [Description("Enable the touch screen")]
            ClickToEnable,
            [Description("No touch screen device connected")]
            DeviceNotConnected,
            [Description("No touch screen device selected")]
            DeviceNotSelected,
            [Description("Not connected to Windows service")]
            ServiceNotConnected,
            [Description("Loading")]
            Loading
        };

        private Func<bool, bool> EnableDisableDevice;
        private Action ShowSettingsWindow;
        public bool deviceIsEnabled = true;

        public UserControl1(Action<UserControl1> onLoad = null)
        {
            InitializeComponent();

            // The initial icon state should be that of inoperability
            UpdateUIImage(inoperableOverride: true, EnableDisableButtonToolTips.Loading);


            // Provide the on load feedback
            if (onLoad != null)
            {
                Loaded += (object sender, RoutedEventArgs e) =>
                {
                    onLoad(this);
                };
            }
        }

        public void SetEnabledDisabledDeviceFunction(Func<bool, bool> func)
        {
            EnableDisableDevice = func;
        }

        public void SetShowSettingsWindowFunction(Action func)
        {
            ShowSettingsWindow = func;
        }

        public void UpdateUIImage(bool inoperableOverride = false, EnableDisableButtonToolTips inoperableReason = EnableDisableButtonToolTips.Loading)
        {
            // This is for events to feed back to the UI thread
            this.Dispatcher.Invoke(() =>
            {
                if (inoperableOverride)
                {
                    // Something is wrong so indicate this
                    EnabledImage.Opacity = 0.4;
                    EnabledImage.Visibility = Visibility.Visible;
                    DisabledImage.Visibility = Visibility.Hidden;
                    EnableDisableButton.ToolTip = inoperableReason.GetDescription();
                    return;
                }

                // Everything is alright
                EnabledImage.Opacity = 1;

                // Update which icon should be shown
                if (deviceIsEnabled)
                {
                    EnabledImage.Visibility = Visibility.Visible;
                    DisabledImage.Visibility = Visibility.Hidden;
                    EnableDisableButton.ToolTip = EnableDisableButtonToolTips.ClickToDisable.GetDescription();
                }
                else
                {
                    EnabledImage.Visibility = Visibility.Hidden;
                    DisabledImage.Visibility = Visibility.Visible;
                    EnableDisableButton.ToolTip = EnableDisableButtonToolTips.ClickToEnable.GetDescription();
                }
            });
        }

        private void Button_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DoButtonAction();
        }

        private void Button_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DoButtonAction();
        }

        private void Button_TouchUp(object sender, System.Windows.Input.TouchEventArgs e)
        {
            DialogResult confirmResult = MessageBox.Show(
                "You used the touch screen to disable the touch screen. Are you sure?",
                "Touch Toggler",
                MessageBoxButtons.YesNo
            );
            if (confirmResult == DialogResult.Yes)
            {
                DoButtonAction();
            }
        }

        private void DoButtonAction()
        {
            bool success = EnableDisableDevice(!deviceIsEnabled);
            if (success)
            {
                deviceIsEnabled = !deviceIsEnabled;
                UpdateUIImage();
            }
        }

        private void SettingsCMItem_Click(object sender, RoutedEventArgs e)
        {
            ShowSettingsWindow();
        }
    }
}
