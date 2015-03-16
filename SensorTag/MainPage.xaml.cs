using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System.Display;
using Windows.System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using X2CodingLab.SensorTag;
using X2CodingLab.SensorTag.Sensors;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace SensorTag
{
    public class SensorTelemetry
    {

        public SensorTelemetry()
        {
            //this.PartitionKey = "1";
            //this.RowKey = Guid.NewGuid().ToString();

            MeasurementTime = DateTime.Now;
            ThermometerAmbTemp = 0.0;
            ThermometerObjTemp = 0.0;
            AccelerometerX = 0.0;
            AccelerometerY = 0.0;
            AccelerometerZ = 0.0;
            HumidityInPercent = 0.0;
            MagnetometerX = 0.0;
            MagnetometerY = 0.0;
            MagnetometerZ = 0.0;
            BarometerAmbPres = 0.0;
            GyroscopeX = 0.0;
            GyroscopeY = 0.0;
            GyroscopeZ = 0.0;

        }
        public override string ToString()
        {
            return string.Format("{0}-Temp:{1}, Temp Obj:{1}", MeasurementTime, ThermometerAmbTemp, ThermometerObjTemp);
        }
        public DateTime MeasurementTime { get; set; }
        public double ThermometerAmbTemp { get; set; }
        public double ThermometerObjTemp { get; set; }
        public double AccelerometerX { get; set; }
        public double AccelerometerY { get; set; }
        public double AccelerometerZ { get; set; }
        public double HumidityInPercent { get; set; }
        public double MagnetometerX { get; set; }
        public double MagnetometerY { get; set; }
        public double MagnetometerZ { get; set; }
        public double BarometerAmbPres { get; set; }
        public double GyroscopeX { get; set; }
        public double GyroscopeY { get; set; }
        public double GyroscopeZ { get; set; }

    }
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        ObservableCollection<SensorTelemetry> list = new ObservableCollection<SensorTelemetry>();
        Accelerometer acc;
        Gyroscope gyro;
        HumiditySensor hum;
        SimpleKeyService ks;
        Magnetometer mg;
        PressureSensor ps;
        IRTemperatureSensor tempSen;
        private static ThreadPoolTimer timerDataTransfer;

        public MainPage()
        {
            this.InitializeComponent();
            App.Current.Suspending += this.App_Suspending;
            App.Current.Resuming += this.App_Resuming;
            this.NavigationCacheMode = NavigationCacheMode.Required;
        }
        private DisplayRequest AppDisplayRequest;
        private long DisplayRequestRefCount = 0;
        private const long LongMax = 2147483647L;


        void App_Resuming(object sender, object e)
        {
            DisplayOn();
        }

        void App_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            DisplayOff();

            deferral.Complete();
        }

        private void DisplayOn()
        {
            if (AppDisplayRequest == null)
            {
                // This call creates an instance of the displayRequest object 
                AppDisplayRequest = new DisplayRequest();
            }

            // This call activates a display-required request. If successful,  
            // the screen is guaranteed not to turn off automatically due to user inactivity.     
            if (DisplayRequestRefCount < LongMax)
            {
                AppDisplayRequest.RequestActive();
                DisplayRequestRefCount++;
            }
            else
            {
            }
        }

        private void DisplayOff()
        {
            if (AppDisplayRequest != null && DisplayRequestRefCount > 0)
            {

                // This call de-activates the display-required request. If successful, the screen 
                // might be turned off automatically due to a user inactivity, depending on the 
                // power policy settings of the system. The requestRelease method throws an exception  
                // if it is called before a successful requestActive call on this object. 
                AppDisplayRequest.RequestRelease();
                DisplayRequestRefCount--;
            }
            else
            {
            }
        }


        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.

            DisplayOn();
            ListViewTelemetry.ItemsSource = list;
            await Setup();
            timerDataTransfer = ThreadPoolTimer.CreatePeriodicTimer(dataTransmitterTimerTick, TimeSpan.FromSeconds(5));


        }
        private async void dataTransmitterTimerTick(ThreadPoolTimer timer)
        {
            var data = await ReadData();
            if (data != null)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                       () =>
                       {
                           list.Add(data);
                       });
            }


            SendToEventHub(data);
        }
        async Task Setup()
        {
            acc = new Accelerometer();
            // acc.SensorValueChanged += SensorValueChanged;
            await acc.Initialize();
            await acc.EnableSensor();

            gyro = new Gyroscope();
            //gyro.SensorValueChanged += SensorValueChanged;
            await gyro.Initialize();
            await gyro.EnableSensor();

            hum = new HumiditySensor();
            //hum.SensorValueChanged += SensorValueChanged;
            await hum.Initialize();
            await hum.EnableSensor();

            ks = new SimpleKeyService();
            //  ks.SensorValueChanged += SensorValueChanged;
            await ks.Initialize();
            await ks.EnableSensor();

            mg = new Magnetometer();
            //    mg.SensorValueChanged += SensorValueChanged;
            await mg.Initialize();
            await mg.EnableSensor();

            ps = new PressureSensor();
            //  ps.SensorValueChanged += SensorValueChanged;
            await ps.Initialize();
            await ps.EnableSensor();

            tempSen = new IRTemperatureSensor();
            //tempSen.SensorValueChanged += SensorValueChanged;
            await tempSen.Initialize();
            await tempSen.EnableSensor();

        }


        async Task<SensorTelemetry> ReadData()
        {
            SensorTelemetry item = new SensorTelemetry();

            try
            {

                byte[] tempValue = await tempSen.ReadValue();
                double ambientTemp = IRTemperatureSensor.CalculateAmbientTemperature(tempValue, TemperatureScale.Celsius);
                double targetTemp = IRTemperatureSensor.CalculateTargetTemperature(tempValue, ambientTemp, TemperatureScale.Celsius);

                item.ThermometerAmbTemp = ambientTemp;
                item.ThermometerObjTemp = targetTemp;


                byte[] accValue = await acc.ReadValue();
                double[] accAxis = Accelerometer.CalculateCoordinates(accValue, 1 / 64.0);
                item.AccelerometerX = accAxis[0];
                item.AccelerometerY = accAxis[1];
                item.AccelerometerZ = accAxis[2];


                byte[] humValue = await hum.ReadValue();
                item.HumidityInPercent = HumiditySensor.CalculateHumidityInPercent(humValue);

                /*  SimpleKeyService ks;
                */

                byte[] gyroValue = await gyro.ReadValue();
                float[] gyroAxis = Gyroscope.CalculateAxisValue(gyroValue, GyroscopeAxis.XYZ);
                item.GyroscopeX = gyroAxis[0];
                item.GyroscopeY = gyroAxis[1];
                item.GyroscopeZ = gyroAxis[2];

                byte[] mgValue = await mg.ReadValue();
                float[] mgAxis = Magnetometer.CalculateCoordinates(mgValue);
                item.MagnetometerX = mgAxis[0];
                item.MagnetometerY = mgAxis[1];
                item.MagnetometerZ = mgAxis[2];

                byte[] psValue = await ps.ReadValue();
                item.BarometerAmbPres = PressureSensor.CalculatePressure(psValue, ps.CalibrationData);

            }
            catch (Exception ex)
            {
                Setup().Wait();
                item = null;
            }
            return item;
        }

        async void SendToEventHub(SensorTelemetry telemetry)
        {
            try
            {
                var response = await PostTelemetryAsync(telemetry);
                string s = response.Content.ReadAsStringAsync().Result;
                response.EnsureSuccessStatusCode();
            }
            catch (Exception)
            {
                //TODO: handle exception
            }
        }

        //From: https://blogs.endjin.com/2015/02/send-data-into-azure-event-hubs-using-web-apis-httpclient/
        private Task<HttpResponseMessage> PostTelemetryAsync(SensorTelemetry sensorTelemetry)
        {
            //How to create SharedAccessSignature: https://github.com/sandrinodimattia/RedDog/releases/tag/0.2.0.1
            var sas = "SharedAccessSignature sr=[SHARED ACCESS SIGNATURE]";

            // Namespace info.
            var serviceNamespace = "[SERVICE BUS NAMESPACE]";
            var hubName = "[HUB NAME]";
            var url = string.Format("{0}/publishers/[SENDER]/messages", hubName);

            // Create client.
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(string.Format("https://{0}.servicebus.windows.net/", serviceNamespace))
            };

            var payload = JsonConvert.SerializeObject(sensorTelemetry);

            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", sas);

            var content = new StringContent(payload, Encoding.UTF8, "application/json");

          
            return httpClient.PostAsync(url, content);


        }
    }
}
