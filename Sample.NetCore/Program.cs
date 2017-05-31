using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.DiagnosticProvider;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

namespace Sample
{
    class Program
    {
        private static readonly string deviceConnectionString = "HostName=Win10IoTCoreRaspberryDiag.azure-devices.net;DeviceId=Raspberry;SharedAccessKey=ZWtkSkV0KRJNOwyie/ZRTT9f9IEjFN5aDUWFioS1S3o=";
        public static void Main(string[] args)
        {
            SendD2CMessage();
        }

        private static void SendD2CMessage()
        {
            var tokenSource = new CancellationTokenSource();

            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                tokenSource.Cancel();
                Console.WriteLine("Existing ...");
            };
            Console.WriteLine("Press CTRL+C to exit");

            Task.WaitAll(
                SendDeviceToCloudMessageAsync(tokenSource.Token)
                );
        }

        public static async Task SendDeviceToCloudMessageAsync(CancellationToken cancelToken)
        {

            var diagnosticProvider = new ProbabilityDiagnosticProvider(SamplingRateSource.Client, 50);
            var deviceClient = DeviceClientWrapper.CreateFromConnectionString(deviceConnectionString, diagnosticProvider);

            const int avgWindSpeed = 10; // m/s
            var rand = new Random();

            while (true)
            {
                if (cancelToken.IsCancellationRequested)
                    break;

                var currentWindSpeed = avgWindSpeed + rand.NextDouble() * 4 - 2;

                var telemetryDataPoint = new
                {
                    windSpeed = currentWindSpeed
                };

                var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                var message = new Message(Encoding.ASCII.GetBytes(messageString));
                await deviceClient.SendEventAsync(message);
                Console.WriteLine("{0} > Sending message: {1} | Count:{2}", DateTime.Now, messageString, diagnosticProvider.MessageNumber);
                await Task.Delay(500, cancelToken);
            }
        }
    }
}
