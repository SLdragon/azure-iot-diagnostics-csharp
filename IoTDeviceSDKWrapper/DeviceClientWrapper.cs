using Microsoft.Azure.Devices.Client.DiagnosticProvider;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client
{
    public class DeviceClientWrapper
    {
        public RetryPolicyType RetryPolicy
        {
            get { return _deviceClient.RetryPolicy; }
            set { _deviceClient.RetryPolicy = value; }
        }

        public uint OperationTimeoutInMilliseconds
        {
            get { return _deviceClient.OperationTimeoutInMilliseconds; }
            set { _deviceClient.OperationTimeoutInMilliseconds = value; }
        }

        private readonly DeviceClient _deviceClient;
        private readonly IDiagnosticProvider _diagnosticProvider;
        private DesiredPropertyUpdateCallback _userDesiredPropertyUpdateCallback;
        private readonly int _retryCount = 3;
        private readonly int _delayTimeLowerLimit = 5 * 60 * 100;
        private readonly int _delayTimeUpperLimit = 30 * 60 * 100;

        internal DesiredPropertyUpdateCallback callbackWrapper;

        private DeviceClientWrapper(DeviceClient deviceClient, IDiagnosticProvider diagnosticProvider)
        {
            _deviceClient = deviceClient;
            _diagnosticProvider = diagnosticProvider;
            if (_diagnosticProvider.GetSamplingRateSource() == SamplingRateSource.Server)
            {
                Task.Run(async () =>
                {
                    await StartListenPropertiesChange(_deviceClient);
                });
            }
        }

        public static DeviceClientWrapper CreateFromConnectionString(string connectionString, IDiagnosticProvider diagnosticProvider = null)
        {
            if (diagnosticProvider == null)
            {
                diagnosticProvider = new ContinuousDiagnosticProvider();
            }
            var deviceClient = DeviceClient.CreateFromConnectionString(connectionString, TransportType.Mqtt);
            return new DeviceClientWrapper(deviceClient, diagnosticProvider);
        }

        public static DeviceClientWrapper Create(string hostname, IAuthenticationMethod authenticationMethod, IDiagnosticProvider diagnosticProvider = null)
        {
            if (diagnosticProvider == null)
            {
                diagnosticProvider = new ContinuousDiagnosticProvider();
            }
            var deviceClient = DeviceClient.Create(hostname, authenticationMethod);
            return new DeviceClientWrapper(deviceClient, diagnosticProvider);
        }

        public static DeviceClientWrapper Create(string hostname, IAuthenticationMethod authenticationMethod, [ReadOnlyArray] ITransportSettings[] transportSettings, IDiagnosticProvider diagnosticProvider = null)
        {
            if (diagnosticProvider == null)
            {
                diagnosticProvider = new ContinuousDiagnosticProvider();
            }
            var deviceClient = DeviceClient.Create(hostname, authenticationMethod, transportSettings);
            return new DeviceClientWrapper(deviceClient, diagnosticProvider);
        }

        public static DeviceClientWrapper CreateFromConnectionString(string connectionString, string deviceId, IDiagnosticProvider diagnosticProvider = null)
        {
            if (diagnosticProvider == null)
            {
                diagnosticProvider = new ContinuousDiagnosticProvider();
            }
            var deviceClient = DeviceClient.CreateFromConnectionString(connectionString, deviceId, TransportType.Mqtt);
            return new DeviceClientWrapper(deviceClient, diagnosticProvider);
        }

        public static DeviceClientWrapper CreateFromConnectionString(string connectionString, [ReadOnlyArray] ITransportSettings[] transportSettings, IDiagnosticProvider diagnosticProvider = null)
        {
            if (diagnosticProvider == null)
            {
                diagnosticProvider = new ContinuousDiagnosticProvider();
            }
            var mqttTransportSetting = GetMqttTransportSettings(transportSettings);
            transportSettings = new[] { mqttTransportSetting };

            var deviceClient = DeviceClient.CreateFromConnectionString(connectionString, transportSettings);
            return new DeviceClientWrapper(deviceClient, diagnosticProvider);
        }

        public static DeviceClientWrapper CreateFromConnectionString(string connectionString, string deviceId, [ReadOnlyArray] ITransportSettings[] transportSettings, IDiagnosticProvider diagnosticProvider = null)
        {
            if (diagnosticProvider == null)
            {
                diagnosticProvider = new ContinuousDiagnosticProvider();
            }
            var mqttTransportSetting = GetMqttTransportSettings(transportSettings);
            transportSettings = new[] { mqttTransportSetting };

            var deviceClient = DeviceClient.CreateFromConnectionString(connectionString, deviceId, transportSettings);
            return new DeviceClientWrapper(deviceClient, diagnosticProvider);
        }

        public Task AbandonAsync(string lockToken)
        {
            return _deviceClient.AbandonAsync(lockToken);
        }

        public Task AbandonAsync(Message message)
        {
            return _deviceClient.AbandonAsync(message);
        }

        public Task CloseAsync()
        {
            return _deviceClient.CloseAsync();
        }

        public Task CompleteAsync(string lockToken)
        {
            return _deviceClient.CompleteAsync(lockToken);
        }

        public Task CompleteAsync(Message message)
        {
            return _deviceClient.CompleteAsync(message);
        }

        public void Dispose()
        {
            _deviceClient.Dispose();
        }

        public Task<Twin> GetTwinAsync()
        {
            return _deviceClient.GetTwinAsync();
        }

        public Task OpenAsync()
        {
            return _deviceClient.OpenAsync();
        }

        public async Task<Message> ReceiveAsync(TimeSpan timeout)
        {
            return await _deviceClient.ReceiveAsync(timeout);
        }

        public Task<Message> ReceiveAsync()
        {
            return _deviceClient.ReceiveAsync();
        }

        public Task RejectAsync(string lockToken)
        {
            return _deviceClient.RejectAsync(lockToken);
        }

        public Task RejectAsync(Message message)
        {
            return _deviceClient.RejectAsync(message);
        }

        public Task SendEventAsync(Message message)
        {
            _diagnosticProvider.Process(message);
            return _deviceClient.SendEventAsync(message);
        }

        public Task SendEventBatchAsync(IEnumerable<Message> messages)
        {
            foreach (var message in messages)
            {
                _diagnosticProvider.Process(message);
            }
            return _deviceClient.SendEventBatchAsync(messages);
        }

        public Task SetDesiredPropertyUpdateCallback(DesiredPropertyUpdateCallback callback, object userContext)
        {
            _userDesiredPropertyUpdateCallback = callback;

            DesiredPropertyUpdateCallback callbackWrapper = (desiredProperties, context) =>
            {
                return Task.Run(() =>
                {
                    if (_diagnosticProvider.GetSamplingRateSource() == SamplingRateSource.Server)
                    {
                        ((BaseDiagnosticProvider)_diagnosticProvider).OnDesiredPropertyChange(desiredProperties, context);
                    }

                    _userDesiredPropertyUpdateCallback(desiredProperties, context);

                });
            };
            this.callbackWrapper = callbackWrapper;
            return _deviceClient.SetDesiredPropertyUpdateCallback(callbackWrapper, userContext);
        }

        [Obsolete("Please use SetMethodHandlerAsync.")]
        public void SetMethodHandler(string methodName, MethodCallback methodHandler, object userContext)
        {
            _deviceClient.SetMethodHandler(methodName, methodHandler, userContext);
        }
        public Task SetMethodHandlerAsync(string methodName, MethodCallback methodHandler, object userContext)
        {
            return _deviceClient.SetMethodHandlerAsync(methodName, methodHandler, userContext);
        }

        public Task UpdateReportedPropertiesAsync(TwinCollection reportedProperties)
        {
            return _deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
        }

        public Task UploadToBlobAsync(string blobName, Stream source)
        {
            return _deviceClient.UploadToBlobAsync(blobName, source);
        }

        public DeviceClient GetOriginalDeviceClient()
        {
            return _deviceClient;
        }

        public IDiagnosticProvider GetDiagnosticProvider()
        {
            return _diagnosticProvider;
        }

        private async Task StartListenPropertiesChange(DeviceClient deviceClient)
        {
            var retryCountRemain = _retryCount;
            while (true)
            {
                try
                {
                    var twin = await deviceClient.GetTwinAsync();
                    ((BaseDiagnosticProvider)_diagnosticProvider).SetSamplingConfigFromTwin(twin.Properties.Desired);
                    await deviceClient.SetDesiredPropertyUpdateCallback(((BaseDiagnosticProvider)_diagnosticProvider).OnDesiredPropertyChange, null);
                    break;
                }
                catch (Exception e)
                {
                    retryCountRemain--;
                    var randomDelayTime = new Random().Next(_delayTimeLowerLimit, _delayTimeUpperLimit);
                    await Task.Delay(randomDelayTime);
                    if (retryCountRemain < 0)
                    {
                        Console.WriteLine("Error occur when get twin settings from server:\n" + e.Message + "\nStop retry and ignore diagnostic.");
                        break;
                    }
                    Console.WriteLine("Error occur when get twin settings from server:\n" + e.Message + "\nRetry...");
                }
            }
        }

        private static ITransportSettings GetMqttTransportSettings(ITransportSettings[] transportSettings)
        {
            ITransportSettings mqttTransportSetting = null;
            foreach (var transportSetting in transportSettings)
            {
                var setting = transportSetting.GetTransportType();
                if (setting != TransportType.Mqtt && setting != TransportType.Mqtt_Tcp_Only && setting != TransportType.Mqtt_WebSocket_Only)
                {
                    continue;
                }
                mqttTransportSetting = transportSetting;
            }

            if (mqttTransportSetting == null)
            {
                throw new ProtocalNotSupportException("Cannot find MQTT protocal in transport settings: diagnostic only support MQTT protocal");
            }

            return mqttTransportSetting;
        }
    }
}
