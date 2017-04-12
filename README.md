# azure-iot-diagnostics-csharp
Azure IoT Hub C# Device SDK with End-to-End Diagnostic library provides a convenient way to send diagnostic messages for IoT devices.

### Usage of End-to-End diagnostic C# SDK

```cs
        //Random Diagnostic Sampling: sampling rate is based on user settings.
        var diagnosticProvider = new ProbabilityDiagnosticProvider(SamplingRateSource.Client, 50);
        var deviceClient = DeviceClientWrapper.CreateFromConnectionString(deviceConnectionString, diagnosticProvider);

        //Periodic Diagnostic Sampling
        var diagnosticProvider = new ContinuousDiagnosticProvider(SamplingRateSource.Client, 50);
        var deviceClient = DeviceClientWrapper.CreateFromConnectionString(deviceConnectionString, diagnosticProvider);

        //You can also set SamplingRateSource.Server to obtain settings from device twin
        var diagnosticProvider = new ProbabilityDiagnosticProvider(SamplingRateSource.Server);
        var deviceClient = DeviceClientWrapper.CreateFromConnectionString(deviceConnectionString, diagnosticProvider);

```

### A quick guide to use End-to-End diagnostic C# SDK
1. Open Visual Studio 2017, and create a new C# console App
2. Download Microsoft.Azure.Devices.Client.Diagnostic NuGet package.
3. Replace the code in Program.cs using the code below:

```cs
        private static readonly string deviceConnectionString = "{Your IoTHub device connection string}";
        static void Main(string[] args)
        {
            sendD2CMessage();
        }

        private static void sendD2CMessage()
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
                Console.WriteLine("{0} > Sending message: {1} | Count:{2}", DateTime.Now, messageString, diagnosticProvider.ProcessCount);
                await Task.Delay(500);
            }
        }
```

3. Replace {Your IoTHub device connection string} with correct device Connection String, the  Connection String could be found from Azure IoT Hub -> Devices -> select {your device name } -> Connection string in right panel
4. Build and run your project.



### A quick guide to develop End-to-End diagnostic C# SDK

1. git clone https://github.com/VSChina/azure-iot-diagnostics-csharp.git
2. Open IoTDeviceSDKWrapper.sln using Visual Studio 2017
3. Rebuild solution
4. Right click IoTDeviceSDKWrapper.Test project, and select the context menu "Run Unit Tests"
5. Right click Sample project, and select "Set as StartUp Project"
6. Run project
7. If you can see the messages in the console window, then the develop environment is ready.


### Related project
azure-iot-diagnostics-java
https://github.com/VSChina/azure-iot-diagnostics-java
