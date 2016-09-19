using System;
using Windows.Devices.Spi;
using Windows.Devices.Enumeration;

namespace TempCheckPiUI
{
    // http://stackoverflow.com/questions/38038788/getting-spi-temperature-data-from-outside-of-class
    public sealed class Thermocouple
    {
        private SpiDevice thermocouple;

        public Thermocouple()
        {

        }

        public async void InitSpi()
        {
            try
            {
                var settings = new SpiConnectionSettings(0);
                settings.ClockFrequency = 5000000;
                settings.Mode = SpiMode.Mode0;

                string spiAqs = SpiDevice.GetDeviceSelector("SPI0");
                var deviceInfo = await DeviceInformation.FindAllAsync(spiAqs);
                thermocouple = await SpiDevice.FromIdAsync(deviceInfo[0].Id, settings);

                ReadTempC();
            }

            catch (Exception ex)
            {
                throw new Exception("SPI Initialization Failed", ex);
            }
        }

        public double ReadTempC()
        {
            var value = Read();

            //Check for error reading value.
            //if value & 0x7 {
            //    return float.NaN;

            // Check if signed bit is set.
            //if value & 0x80000000:
            //    # Negative value, take 2's compliment. Compute this with subtraction
            //    # because python is a little odd about handling signed/unsigned.
            //    v >>= 18
            //    v -= 16384
            //else:
            // Positive value, just shift the bits to get the value.
            value >>= 18;
            // Scale by 0.25 degrees C per bit and return value.
            return value * 0.25;
        }

        private int Read()
        {
            byte[] raw = new byte[4];
            thermocouple.Read(raw);

            var value = raw[0] << 24 | raw[1] << 16 | raw[2] << 8 | raw[3];

            return value;
        }
    }
}
