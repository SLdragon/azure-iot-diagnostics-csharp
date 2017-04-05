using Microsoft.Azure.Devices.Client;

namespace Microsoft.Azure.Devices.Client.DiagnosticProvider
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
        bool ShouldAddDiagnosticProperty(int count);
        bool SamplingOn { get; }
        int SamplingRatePercentage { get; }
        SamplingRateSource GetSamplingRateSource();
        int GetSampledCount();
    }
}
