using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Band;
using System.Threading.Tasks;
using Windows.UI.Core;
using Quobject.SocketIoClientDotNet.Client;

namespace HeartRate
{
    public sealed partial class MainPage : Page
    {
        private App viewModel;
        private Socket socket;

        public MainPage()
        {
            this.InitializeComponent();
            this.DataContext = viewModel = App.Current;

            initSocket("http://heartrate.azurewebsites.net");
        }

        private void initSocket(string server)
        {
            socket = IO.Socket(server);

            socket.On(Socket.EVENT_CONNECT, async () =>
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,() =>
                {
                    viewModel.ServerStatus = "Connected";
                }).AsTask();
            });


            socket.On(Socket.EVENT_CONNECT_ERROR, async () =>
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    viewModel.ServerStatus = "Can't connect";
                }).AsTask();
            });

            socket.On(Socket.EVENT_DISCONNECT, async () =>
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    viewModel.ServerStatus = "Not connected";
                }).AsTask();
            });
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the list of Microsoft Bands paired to the phone.
                IBandInfo[] pairedBands = await BandClientManager.Instance.GetBandsAsync();
                if (pairedBands.Length < 1)
                {
                    viewModel.BandStatus = "Not found";
                    return;
                }
                viewModel.BandStatus = "Connecting...";

                // Connect to Microsoft Band.
                using (IBandClient bandClient = await BandClientManager.Instance.ConnectAsync(pairedBands[0]))
                {
                    viewModel.BandStatus = "Connected";

                    bool heartRateConsentGranted;

                    // Check whether the user has granted access to the HeartRate sensor.
                    if (bandClient.SensorManager.HeartRate.GetCurrentUserConsent() == UserConsent.Granted)
                    {
                        viewModel.HeartRateSensorStatus = "Acess granted";
                        heartRateConsentGranted = true;
                    }
                    else
                    {
                        viewModel.HeartRateSensorStatus = "Requesting access...";
                        heartRateConsentGranted = await bandClient.SensorManager.HeartRate.RequestUserConsentAsync();
                    }

                    if (!heartRateConsentGranted)
                    {
                        viewModel.HeartRateSensorStatus = "Access denied";
                    }
                    else
                    {
                        // Subscribe to HeartRate data.
                        bandClient.SensorManager.HeartRate.ReadingChanged += HeartRate_ReadingChanged;
                        viewModel.HeartRateSensorStatus = "Reading...";

                        await bandClient.SensorManager.HeartRate.StartReadingsAsync();
                        viewModel.HeartRateSensorStatus = "Acquiring...";
                        await Task.Delay(TimeSpan.FromSeconds(2000));
                        await bandClient.SensorManager.HeartRate.StopReadingsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                viewModel.BandStatus = "Can't Connect";
            }
        }

        private async void HeartRate_ReadingChanged(object sender, Microsoft.Band.Sensors.BandSensorReadingEventArgs<Microsoft.Band.Sensors.IBandHeartRateReading> args)
        {

            socket.Emit("rate", args.SensorReading.HeartRate);

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
               () =>
               {
                   System.Diagnostics.Debug.WriteLine(args.SensorReading.HeartRate.ToString());
                   viewModel.HeartRateSensorStatus = args.SensorReading.Quality.ToString();
                   viewModel.HeartRate = string.Format("{0} bpm", args.SensorReading.HeartRate);
               }).AsTask();
        }
    }
}
