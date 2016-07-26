using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
                timer.Tick += timer_Tick;
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

        private void timer_Tick(object sender, object e)
        {
            UpdateDateTime();
            UpdateTemperature();
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

        private void UpdateTemperature()
        {
            var temp = thermocouple.ReadTempC();
            Temperature.Text = string.Format("{0} °C", temp.ToString());
        }

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

        private void Tutorials_Clicked(object sender, RoutedEventArgs e)
        {
            //NavigationUtils.NavigateToScreen(typeof(TutorialMainPage));
        }

        private void ShutdownHelper(ShutdownKind kind)
        {
            new System.Threading.Tasks.Task(() =>
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

    //Thermocouple thermocouple;

    //public MainPage()
    //{
    //    this.InitializeComponent();

    //    thermocouple = new Thermocouple();
    //    thermocouple.InitSpi();

    //    var timer = new DispatcherTimer();
    //    timer.Tick += OnTimerTick;
    //    timer.Interval = TimeSpan.FromMilliseconds(5000);
    //    timer.Start();
    //}

    //private void OnTimerTick(object sender, object e)
    //{
    //    var temp = thermocouple.ReadTempC();

    //    DateTimeTxt.Text = DateTime.Now.ToString();
    //    TemperatureTxt.Text = temp.ToString();
    //}
}
