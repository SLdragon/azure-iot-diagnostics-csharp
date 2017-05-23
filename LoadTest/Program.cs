using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.DiagnosticProvider;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LoadTest
{
    class Program
    {
        private static string _deviceConnectionString;

        private static int _maxMessageCount;

        public static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("The argument length is not adequate. "+args.Length+" < 2");
            }
            else
            {
                _deviceConnectionString = args[0];
                _maxMessageCount =int.Parse(args[1]);
                SendD2CMessage();
            }
        }

        private static void SendD2CMessage()
        {
            var tokenSource = new CancellationTokenSource();
            Task.WaitAll(
                SendDeviceToCloudMessageAsync(tokenSource.Token)
                );
        }

        public static async Task SendDeviceToCloudMessageAsync(CancellationToken cancelToken)
        {
            Console.ReadLine();
            var diagnosticProvider = new ProbabilityDiagnosticProvider(SamplingRateSource.Server);
            var deviceClient = DeviceClientWrapper.CreateFromConnectionString(_deviceConnectionString, diagnosticProvider);

            const int avgWindSpeed = 10;
            var rand = new Random();
            var count = 0;
            var beforeStartSendTimeStamp =DateTime.Now;
            
            while (true)
            {
                count++;
                
                if (cancelToken.IsCancellationRequested)
                    break;

                var currentWindSpeed = avgWindSpeed + rand.NextDouble() * 4 - 2;

                var telemetryDataPoint = new
                {
                    windSpeed = currentWindSpeed
                };

                var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                var message = new Message(Encoding.ASCII.GetBytes(messageString));

                try
                {
                    var timeout = 1000000;
                    
                    Console.WriteLine("{0} > Start to send D2C message: {1} | Count:{2}", DateTime.Now, messageString, count);
                    
                    var task =  deviceClient.SendEventAsync(message);
                    
                    if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
                    {
                        Console.WriteLine("{0} > Sending D2C message success: {1} | Count:{2}", DateTime.Now, messageString, count);
                    }
                    else
                    {
                        Console.WriteLine("Send message timeout");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error occur when send D2C message:"+ ex);
                }

                if (count == _maxMessageCount)
                {
                    break;
                }
                await Task.Delay(500, cancelToken);
            }
            var afterSendTimeStamp = DateTime.Now;
            var totalTimeConsume = afterSendTimeStamp - beforeStartSendTimeStamp;
            Console.WriteLine($"StartTime: {beforeStartSendTimeStamp} | EndTime: {afterSendTimeStamp} | TotalTimeConsume: {totalTimeConsume.TotalMilliseconds}");
            Console.WriteLine("All task complete!");
        }
    }
}
