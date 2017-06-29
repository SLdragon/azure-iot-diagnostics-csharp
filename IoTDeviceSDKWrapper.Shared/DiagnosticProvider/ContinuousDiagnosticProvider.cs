using System;

namespace Microsoft.Azure.Devices.Client.DiagnosticProvider
{
    public class ContinuousDiagnosticProvider : BaseDiagnosticProvider
    {
        public ContinuousDiagnosticProvider(SamplingRateSource source = SamplingRateSource.None, int samplingRate = 0) : base(source, samplingRate)
        {
        }

        public override bool ShouldAddDiagnosticProperties()
        {
            return Math.Floor((MessageNumber - 2) * SamplingRatePercentage / 100.0) < Math.Floor((MessageNumber - 1) * SamplingRatePercentage / 100.0);
        }
    }
}
