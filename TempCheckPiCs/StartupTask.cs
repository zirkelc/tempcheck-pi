using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using Windows.System.Threading;
using Windows.Devices.Spi;
using System.Diagnostics;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace TempCheckPiCs
{
    public sealed class StartupTask : IBackgroundTask
    {
        BackgroundTaskDeferral _deferral;
        Thermocouple thermocouple;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            // If you start any asynchronous methods here, prevent the task
            // from closing prematurely by using BackgroundTaskDeferral as
            // described in http://aka.ms/backgroundtaskdeferral 
            _deferral = taskInstance.GetDeferral();

            thermocouple = new Thermocouple();
            thermocouple.InitSpi();

            var timer = ThreadPoolTimer.CreatePeriodicTimer(OnTimerTick, TimeSpan.FromMilliseconds(5000));
        }

        private void OnTimerTick(ThreadPoolTimer timer)
        {
            var temp = thermocouple.ReadTempC();
            Debug.WriteLine(temp);
        }
    }
}
