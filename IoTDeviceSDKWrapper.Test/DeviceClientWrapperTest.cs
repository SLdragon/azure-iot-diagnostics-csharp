using System;
using IoTDeviceSDKWrapper.Exceptions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Threading.Tasks;
using IoTDeviceSDKWrapper.DiagnosticProvider;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;

namespace IoTDeviceSDKWrapper.Test
{
    /// <summary>
    /// Summary description for DeviceClientWrapperTest
    /// </summary>
    [TestClass]
    public class DeviceClientWrapperTest
    {
        private readonly string fakeConnectionString = "HostName=RentuIoTHub.azure-devices.net;DeviceId=csharpsdk22;SharedAccessKey=fsFw7xcuIVdJ79d09rzVPQ0SMv5k5fq1/ZlrsRUIQTc=";

        [TestMethod]
        public void TransportSettingsDoNotContainMqttProtocal()
        {
            var transportSetting=new AmqpTransportSettings(TransportType.Amqp_Tcp_Only);
            ITransportSettings[] transportSettings = { transportSetting };
            
            Assert.ThrowsException<ProtocalNotSupportException>(() =>
            {
                DeviceClientWrapper.CreateFromConnectionString(fakeConnectionString, transportSettings);
            });

            var transportSetting2 = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            ITransportSettings[]  transportSettings2 = { transportSetting2 };


            DeviceClientWrapper.CreateFromConnectionString(fakeConnectionString, transportSettings2);

        }

        [TestMethod]
        public void UserWillNotReceiveDiagnosticTwinUpdate()
        {
            var fakeDiagnosticProvider = Substitute.For<IDiagnosticProvider>();
            
            var deviceClient = DeviceClientWrapper.CreateFromConnectionString(fakeConnectionString,fakeDiagnosticProvider);
            
            DesiredPropertyUpdateCallback userCallback = (desiredProperties, context) =>
            {
                return Task.Run(() =>
                {
                    if (desiredProperties.Contains("diag_enabled") || desiredProperties.Contains("diag_sample_rate"))
                    {
                        Assert.Fail();
                    }
                });
            };

            deviceClient.SetDesiredPropertyUpdateCallback(userCallback,new object());
            var twin=new Twin();
            twin.Properties.Desired["diag_enable"] = "true";
            twin.Properties.Desired["diag_sample_rate"] = "10";
            deviceClient.CallbackWrapper(new TwinCollection(), new object());
        }

        [TestMethod]
        public void UserWillReceiveCustomTwinUpdate()
        {
            var fakeDiagnosticProvider = Substitute.For<IDiagnosticProvider>();
           
            DeviceClientWrapper deviceClient = DeviceClientWrapper.CreateFromConnectionString(fakeConnectionString, fakeDiagnosticProvider);

            var userCallback = Substitute.For<DesiredPropertyUpdateCallback>();
            deviceClient.SetDesiredPropertyUpdateCallback(userCallback, new object());
            var twin = new Twin();
            twin.Properties.Desired["custom_settings"] = "xxxx";
            deviceClient.CallbackWrapper(twin.Properties.Desired, new object());
            userCallback.Received(1).Invoke(Arg.Any<TwinCollection>(), Arg.Any<Object>());
        }
    }
}
