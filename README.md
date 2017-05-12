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
                Console.WriteLine("{0} > Sending message: {1} | Count:{2}", DateTime.Now, messageString, diagnosticProvider.ProcessCount);
                await Task.Delay(500);
            }
        }
```

3. Replace {Your IoTHub device connection string} with correct device Connection String, the  Connection String could be found from Azure IoT Hub -> Devices -> select {your device name } -> Connection string in right panel
4. Build and run your project.



### A quick guide to set up End-to-End diagnostic C# SDK development environment

1. git clone https://github.com/VSChina/azure-iot-diagnostics-csharp.git
2. Open IoTDeviceSDKWrapper.sln using Visual Studio 2017
3. Rebuild solution
4. Right click IoTDeviceSDKWrapper.Test project, and select the context menu "Run Unit Tests"
5. Right click Sample project, and select "Set as StartUp Project"
6. Run project
7. If you can see the messages in the console window, then the development environment is ready.


### API Reference

### DeviceClientWrapper
----
This class is used to create diagnostic device client which has similar interface with [Microsoft.Azure.Devices.Client SDK](https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.devices.client.deviceclient?view=azuredevicesclient-1.2.6) 

```cs
public static DeviceClientWrapper CreateFromConnectionString(string connectionString, IDiagnosticProvider diagnosticProvider = null)
```

`DeviceClientWrapper` object which has similar interface with Microsoft.Azure.Devices.Client SDK

`connectionString` is the connection string which can be obtain from Azure IoT Hub -> Devices -> select {your device name } -> Connection string in right panel

`diagnosticProvider` is the DiagnosticProvider object  which used to set diagnostic configurations

```cs
public static DeviceClientWrapper CreateFromConnectionString(string connectionString, string deviceId, IDiagnosticProvider diagnosticProvider = null)
```

`DeviceClientWrapper` object which has similar interface with Microsoft.Azure.Devices.Client SDK

`connectionString` is the connection string can be obtain from Azure IoT Hub -> Devices -> select {your device name } -> Connection string in right panel

`deviceId` is the device ID of IoT device

`diagnosticProvider` is the DiagnosticProvider object  which used to set diagnostic configurations

```cs
public static DeviceClientWrapper CreateFromConnectionString(string connectionString, [ReadOnlyArray] ITransportSettings[] transportSettings, IDiagnosticProvider diagnosticProvider = null)
```

`DeviceClientWrapper` object which has similar interface with Microsoft.Azure.Devices.Client SDK

`connectionString` is the connection string which can be obtain from Azure IoT Hub -> Devices -> select {your device name } -> Connection string in right panel

`transportSettings` the transport settings, please refer [Microsoft.Azure.Devices.Client.ITransportSettings](https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.devices.client.itransportsettings?view=azuredevicesclient-1.2.6)

`diagnosticProvider` is the DiagnosticProvider object  which used to set diagnostic configurations

```cs
public static DeviceClientWrapper CreateFromConnectionString(string connectionString, string deviceId, [ReadOnlyArray] ITransportSettings[] transportSettings, IDiagnosticProvider diagnosticProvider = null)
```

`DeviceClientWrapper` object which has similar interface with Microsoft.Azure.Devices.Client SDK


`connectionString` is the connection string which can be obtain from Azure IoT Hub -> Devices -> select {your device name } -> Connection string in right panel

`deviceId` is the device ID of IoT device

`transportSettings` the transport settings, please refer [Microsoft.Azure.Devices.Client.ITransportSettings](https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.devices.client.itransportsettings?view=azuredevicesclient-1.2.6)

`diagnosticProvider` is the DiagnosticProvider object  which used to set diagnostic configurations

##### Other APIs in DeviceClientWrapper are similar with Microsoft.Azure.Devices.Client SDK, please refer: https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.devices.client.deviceclient?view=azuredevicesclient-1.2.6

### ContinuousDiagnosticProvider
----
This class provide a settings for continuous interval sampling, which means the diagnostic messages appear periodic and are uniform distributed in user's messages

```cs
public ContinuousDiagnosticProvider(SamplingRateSource source = SamplingRateSource.None, int samplingRate = 0)
```

`SamplingRateSource` can be set to SamplingRateSource.None/SamplingRateSource.Client/SamplingRateSource.Server, SamplingRateSource.None means do not send diagnostic message, SamplingRateSource.Client means sampling rate is based on local user settings, SamplingRateSource.Server means sampling rate is based on IoTHub device Twin settings

`samplingRate` can be set from 0 to 100, which means the sampling percentage of user message, 0 means do not insert diagnostic information to user's messages


### ProbabilityDiagnosticProvider
----
This class provide a settings for random sampling, which means the diagnostic message appear randomly and are random distributed in user's message

```cs
public ProbabilityDiagnosticProvider(SamplingRateSource source = SamplingRateSource.None, int samplingRate = 0)
```

`SamplingRateSource` can be set to SamplingRateSource.None/SamplingRateSource.Client/SamplingRateSource.Server, SamplingRateSource.None means do not send diagnostic message, SamplingRateSource.Client means sampling rate is based on local user settings, SamplingRateSource.Server means sampling rate is based on IoTHub device Twin settings

`samplingRate` can be set from 0 to 100, which means the sampling percentage of user message, 0 means do not insert diagnostic information to user's messages



### Related project
#### C# Diagnostic SDK UWP Demos
https://github.com/VSChina/win10-iot-core-diagnostic-app

#### Java Diagnostic SDK
https://github.com/VSChina/azure-iot-diagnostics-java
