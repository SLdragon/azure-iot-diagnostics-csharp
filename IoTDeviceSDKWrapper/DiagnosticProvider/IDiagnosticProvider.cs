using Microsoft.Azure.Devices.Client;

namespace IoTDeviceSDKWrapper.DiagnosticProvider
{
    public enum SamplingRateSource
    {
        None,
        Client,
        Server
    }
    public interface IDiagnosticProvider
    {
        Message Process(Message message);
        bool NeedSampling(int count);
        bool SamplingOn { get; set; }
       int SamplingRatePercentage { get; set; }
        SamplingRateSource GetSamplingRateSource();
        int GetSampledCount();

        // void SetSamplingConfigFromTwin(TwinCollection desiredProperties);
        //Task OnDesiredPropertyChange(TwinCollection desiredProperties, object userContext);
    }
}
