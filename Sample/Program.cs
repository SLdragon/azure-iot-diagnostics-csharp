using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.DiagnosticProvider;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sample
{
    class Program
    {
        private static string deviceConnectionString = null;
        private static volatile bool sendInvalidMessage = false;
        public static void Main(string[] args)
        {
            SendD2CMessage();
        }

        private static void SendD2CMessage()
        {
            deviceConnectionString = ConfigurationManager.AppSettings.Get("deviceConnectionString");
            if(string.IsNullOrEmpty(deviceConnectionString))
            {
                Console.WriteLine("Please provide valid deviceConnectionString value in config file");
                return;
            }

            var tokenSource = new CancellationTokenSource();
            Task.Run(async() =>
            {
                await SendDeviceToCloudMessageAsync(tokenSource.Token);
            });

            Console.WriteLine("Press Y to send invalid messages, press any other key to exit");
            bool shouldContinue = true;
            while (shouldContinue)
            {
                var key = Console.ReadKey();
                switch(key.Key)
                {
                    case ConsoleKey.Y:
                        sendInvalidMessage = true;
                        Console.WriteLine("Sending invalid message...");
                        break;
                    default:
                        tokenSource.Cancel();
                        Thread.Sleep(1000);
                        shouldContinue = false;
                        break;
                }
            }
        }

        public static async Task SendDeviceToCloudMessageAsync(CancellationToken cancelToken)
        {
            var diagnosticProvider = new ProbabilityDiagnosticProvider(SamplingRateSource.Server, 0);
            var deviceClient = DeviceClientWrapper.CreateFromConnectionString(deviceConnectionString, diagnosticProvider);
            var rand = new Random();

            while (true)
            {
                if (cancelToken.IsCancellationRequested)
                    break;

                var randomHumity = rand.Next(0, 100);

                dynamic telemetryDataPoint;

                if(sendInvalidMessage)
                {
                    telemetryDataPoint = new
                    {
                        humity = randomHumity
                    };
                }
                else
                {
                    telemetryDataPoint = new
                    {
                        temperature = rand.Next(0, 40),
                        humity = randomHumity
                    };
                }

                var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                var message = new Message(Encoding.ASCII.GetBytes(messageString));
                await deviceClient.SendEventAsync(message);
                Console.WriteLine("{0} > Sending message: {1} | Count:{2}", DateTime.Now, messageString, diagnosticProvider.MessageNumber);
                await Task.Delay(1000, cancelToken);
            }
        }
    }
}
