using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using TempCheckPiUI.Models;
using TempCheckPiUI.Services;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

// Die Vorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 dokumentiert.

namespace TempCheckPiUI
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public static MainPage Current;
        private CoreDispatcher MainPageDispatcher;
        private DispatcherTimer timer;
        private ConnectedDevicePresenter connectedDevicePresenter;
        private Thermocouple thermocouple;

        public CoreDispatcher UIThreadDispatcher
        {
            get
            {
                return MainPageDispatcher;
            }

            set
            {
                MainPageDispatcher = value;
            }
        }

        public MainPage()
        {
            this.InitializeComponent();

            // This is a static public property that allows downstream pages to get a handle to the MainPage instance
            // in order to call methods that are in this class.
            Current = this;

            MainPageDispatcher = Window.Current.Dispatcher;

            NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;

            this.NavigationCacheMode = NavigationCacheMode.Enabled;

            this.DataContext = LanguageManager.GetInstance();

            this.Loaded += (sender, e) =>
            {
                UpdateQrCode();
                UpdateBoardInfo();
                UpdateNetworkInfo();
                UpdateDateTime();
                UpdateConnectedDevices();

                thermocouple = new Thermocouple();
                thermocouple.InitSpi();

                timer = new DispatcherTimer();
                timer.Tick += OnTimerTick;
                timer.Interval = TimeSpan.FromSeconds(10);
                timer.Start();
            };
            this.Unloaded += (sender, e) =>
            {
                timer.Stop();
                timer = null;
            };
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(Constants.HasDoneOOBEKey))
            {
                ApplicationData.Current.LocalSettings.Values[Constants.HasDoneOOBEKey] = Constants.HasDoneOOBEValue;
            }

            base.OnNavigatedTo(e);
        }

        private async void NetworkInformation_NetworkStatusChanged(object sender)
        {
            await MainPageDispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                UpdateNetworkInfo();
            });
        }

        private async void OnTimerTick(object sender, object e)
        {
            UpdateDateTime();
            await UpdateTemperature();
        }

        private void UpdateQrCode()
        {
            var writer = new BarcodeWriter();
            writer.Format = BarcodeFormat.QR_CODE;
            writer.Options = new EncodingOptions() { Width = 300, Height = 300, Margin = 0 };
            //writer.Renderer = new BitmapRenderer();
            var result = writer.Write("TempCheck");

            QrCodeImage.Source = result.ToBitmap() as WriteableBitmap;
        }

        private async Task UpdateTemperature()
        {
            var value = thermocouple.ReadTempC();
            Temperature.Text = string.Format("{0} °C", value.ToString());

            Temperature temperature = new Temperature
            {
                DeviceId = Constants.DeviceId,
                Timestamp = DateTime.Now,
                Value = value
            };

            await AmazonWebService.Instance.SaveAsync(temperature);
        }

    //    private async Task UpdateBluetooth()
    //    {
    //        var themometerServices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(GattDeviceService.GetDeviceSelectorFromUuid(GattServiceUuids.GenericAccess), null);

    //        GattDeviceService firstThermometerService = await
    //            GattDeviceService.FromIdAsync(themometerServices[0].Id);

    //        Debug.WriteLine("Using service: " + themometerServices[0].Name);

    //        GattCharacteristic thermometerCharacteristic =
    //            firstThermometerService.GetCharacteristics(
    //                GattCharacteristicUuids.BatteryLevel)[0];

    //        thermometerCharacteristic.ValueChanged += temperatureMeasurementChanged;

    //        await thermometerCharacteristic
    //            .WriteClientCharacteristicConfigurationDescriptorAsync(
    //                GattClientCharacteristicConfigurationDescriptorValue.Notify);
    //    }

    //    void temperatureMeasurementChanged(
    //GattCharacteristic sender,
    //GattValueChangedEventArgs eventArgs)
    //    {
    //        byte[] temperatureData = new byte[eventArgs.CharacteristicValue.Length];
    //        Windows.Storage.Streams.DataReader.FromBuffer(
    //            eventArgs.CharacteristicValue).ReadBytes(temperatureData);

    //        var temperatureValue = convertTemperatureData(temperatureData);

    //        //temperatureTextBlock.Text = temperatureValue.ToString();
    //    }

    //    double convertTemperatureData(byte[] temperatureData)
    //    {
    //        // Read temperature data in IEEE 11703 floating point format
    //        // temperatureData[0] contains flags about optional data - not used
    //        UInt32 mantissa = ((UInt32)temperatureData[3] << 16) |
    //            ((UInt32)temperatureData[2] << 8) |
    //            ((UInt32)temperatureData[1]);

    //        Int32 exponent = (Int32)temperatureData[4];

    //        return mantissa * Math.Pow(10.0, exponent);
    //    }


        private void UpdateBoardInfo()
        {
            //BoardName.Text = DeviceInfoPresenter.GetBoardName();
            //BoardImage.Source = new BitmapImage(DeviceInfoPresenter.GetBoardImageUri());

            ulong version = 0;
            if (!ulong.TryParse(Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamilyVersion, out version))
            {
                var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                OSVersion.Text = loader.GetString("OSVersionNotAvailable");
            }
            else
            {
                OSVersion.Text = String.Format(CultureInfo.InvariantCulture, "{0}.{1}.{2}.{3}",
                    (version & 0xFFFF000000000000) >> 48,
                    (version & 0x0000FFFF00000000) >> 32,
                    (version & 0x00000000FFFF0000) >> 16,
                    version & 0x000000000000FFFF);
            }
        }

        private void UpdateDateTime()
        {
            // Using DateTime.Now is simpler, but the time zone is cached. So, we use a native method insead.
            SYSTEMTIME localTime;
            NativeTimeMethods.GetLocalTime(out localTime);

            DateTime t = localTime.ToDateTime();
            CurrentTime.Text = t.ToString("t", CultureInfo.CurrentCulture) + Environment.NewLine + t.ToString("d", CultureInfo.CurrentCulture);
        }

        private async void UpdateNetworkInfo()
        {
            this.DeviceName.Text = DeviceInfoPresenter.GetDeviceName();
            this.IPAddress1.Text = NetworkPresenter.GetCurrentIpv4Address();
            this.NetworkName1.Text = NetworkPresenter.GetCurrentNetworkName();
            this.NetworkInfo.ItemsSource = await NetworkPresenter.GetNetworkInformation();
        }

        private void UpdateConnectedDevices()
        {
            connectedDevicePresenter = new ConnectedDevicePresenter(MainPageDispatcher);
            this.ConnectedDevices.ItemsSource = connectedDevicePresenter.GetConnectedDevices();
        }

        private void ShutdownButton_Clicked(object sender, RoutedEventArgs e)
        {
            ShutdownDropdown.IsOpen = true;
        }

        private void CommandLineButton_Clicked(object sender, RoutedEventArgs e)
        {
            //NavigationUtils.NavigateToScreen(typeof(CommandLinePage));
        }

        private void SettingsButton_Clicked(object sender, RoutedEventArgs e)
        {
            //NavigationUtils.NavigateToScreen(typeof(Settings));
        }

        private async void Bluetooth_Clicked(object sender, RoutedEventArgs e)
        {
            //DevicePicker devicePicker = new DevicePicker();
            //devicePicker.Show(new Rect(0, 0, 300, 300));

            //await UpdateBluetooth();
        }

        private void ShutdownHelper(ShutdownKind kind)
        {
            new Task(() =>
            {
                ShutdownManager.BeginShutdown(kind, TimeSpan.FromSeconds(0));
            }).Start();
        }

        private void ShutdownListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as FrameworkElement;
            if (item == null)
            {
                return;
            }
            switch (item.Name)
            {
                case "ShutdownOption":
                    ShutdownHelper(ShutdownKind.Shutdown);
                    break;
                case "RestartOption":
                    ShutdownHelper(ShutdownKind.Restart);
                    break;
            }
        }

        private void ShutdownDropdown_Opened(object sender, object e)
        {
            var w = ShutdownListView.ActualWidth;
            if (w == 0)
            {
                // trick to recalculate the size of the dropdown
                ShutdownDropdown.IsOpen = false;
                ShutdownDropdown.IsOpen = true;
            }
            var offset = -(ShutdownListView.ActualWidth - ShutdownButton.ActualWidth);
            ShutdownDropdown.HorizontalOffset = offset;
        }
    }
}
