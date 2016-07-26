using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace TempCheckPiUI
{
    public class InboundPairingEventArgs
    {
        public InboundPairingEventArgs(DeviceInformation di)
        {
            DeviceInfo = di;
        }
        public DeviceInformation DeviceInfo
        {
            get;
            private set;
        }
    }
    // Callback handler delegate type for Inbound pairing requests
    public delegate void InboundPairingRequestedHandler(object sender, InboundPairingEventArgs inboundArgs);

    /// <summary>
    /// Stellt das anwendungsspezifische Verhalten bereit, um die Standardanwendungsklasse zu ergänzen.
    /// </summary>
    sealed partial class App : Application
    {
        // Handler for Inbound pairing requests
        public static event InboundPairingRequestedHandler InboundPairingRequested;

        // Don't try and make discoverable if this has already been done
        private static bool isDiscoverable = false;

        public static bool IsBluetoothDiscoverable
        {
            get
            {
                return isDiscoverable;
            }

            set
            {
                isDiscoverable = value;
            }
        }

        /// <summary>
        /// Initialisiert das Singletonanwendungsobjekt. Dies ist die erste Zeile von erstelltem Code
        /// und daher das logische Äquivalent von main() bzw. WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Wird aufgerufen, wenn die Anwendung durch den Endbenutzer normal gestartet wird. Weitere Einstiegspunkte
        /// werden z. B. verwendet, wenn die Anwendung gestartet wird, um eine bestimmte Datei zu öffnen.
        /// </summary>
        /// <param name="e">Details über Startanforderung und -prozess.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {

            /*#if DEBUG
                        if (System.Diagnostics.Debugger.IsAttached)
                        {
                            this.DebugSettings.EnableFrameRateCounter = true;
                        }
            #endif*/

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();
                // Set the default language
                rootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter

//#if !FORCE_OOBE_WELCOME_SCREEN
                //if (ApplicationData.Current.LocalSettings.Values.ContainsKey(Constants.HasDoneOOBEKey))
                //{
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                //}
                //else
//#endif
                //{
                //    rootFrame.Navigate(typeof(OOBEWelcome), e.Arguments);
                //}

            }
            // Ensure the current window is active
            Window.Current.Activate();

            //Screensaver.InitializeScreensaver();
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            // Spot if we are being activated due to inbound pairing request
            if (args.Kind == ActivationKind.DevicePairing)
            {
                // Ensure the main app loads first
                OnLaunched(null);

                // Get the arguments, which give information about the device which wants to pair with this app
                var devicePairingArgs = (DevicePairingActivatedEventArgs)args;
                var di = devicePairingArgs.DeviceInformation;

                // Automatically switch to Bluetooth Settings page
                //NavigationUtils.NavigateToScreen(typeof(Settings));

                int bluetoothSettingsIndex = 2;
                Frame rootFrame = Window.Current.Content as Frame;
                ListView settingsListView = null;
                settingsListView = (rootFrame.Content as FrameworkElement).FindName("SettingsChoice") as ListView;
                settingsListView.Focus(FocusState.Programmatic);
                bluetoothSettingsIndex = Math.Min(bluetoothSettingsIndex, settingsListView.Items.Count - 1);
                settingsListView.SelectedIndex = bluetoothSettingsIndex;
                // Appropriate Bluetooth Listview grid content is forced by App_InboundPairingRequested call to SwitchToSelectedSettings

                // Fire the event letting subscribers know there's a new inbound request.
                // In this case Scenario should be subscribed.
                if (InboundPairingRequested != null)
                {
                    InboundPairingEventArgs inboundEventArgs = new InboundPairingEventArgs(di);
                    InboundPairingRequested(this, inboundEventArgs);
                }
            }
        }

        /// <summary>
        /// Wird aufgerufen, wenn die Navigation auf eine bestimmte Seite fehlschlägt
        /// </summary>
        /// <param name="sender">Der Rahmen, bei dem die Navigation fehlgeschlagen ist</param>
        /// <param name="e">Details über den Navigationsfehler</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Wird aufgerufen, wenn die Ausführung der Anwendung angehalten wird.  Der Anwendungszustand wird gespeichert,
        /// ohne zu wissen, ob die Anwendung beendet oder fortgesetzt wird und die Speicherinhalte dabei
        /// unbeschädigt bleiben.
        /// </summary>
        /// <param name="sender">Die Quelle der Anhalteanforderung.</param>
        /// <param name="e">Details zur Anhalteanforderung.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Anwendungszustand speichern und alle Hintergrundaktivitäten beenden
            deferral.Complete();
        }
    }
}
