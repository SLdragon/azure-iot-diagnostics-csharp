using System;

namespace IoTDeviceSDKWrapper.DiagnosticProvider
{
    public class ProbabilityDiagnosticProvider:BaseDiagnosticProvider
    {
        public ProbabilityDiagnosticProvider(SamplingRateSource source = SamplingRateSource.None, int samplingRate = 0):base(source,samplingRate)
        {
        }
        public override bool NeedSampling(int count)
        {
            var randomNumber = new Random().Next(1, 101);
            return randomNumber <= SamplingRatePercentage;
        }
    }
}
