using System;

namespace Microsoft.Azure.Devices.Client.DiagnosticProvider
{
    public class ProbabilityDiagnosticProvider : BaseDiagnosticProvider
    {
        private readonly Random _random = new Random();
        public ProbabilityDiagnosticProvider(SamplingRateSource source = SamplingRateSource.None, int samplingRate = 0) : base(source, samplingRate)
        {
        }
        public override bool ShouldAddDiagnosticProperty(int count)
        {
            var randomNumber = _random.Next(1, 101);
            return randomNumber <= SamplingRatePercentage;
        }
    }
}
