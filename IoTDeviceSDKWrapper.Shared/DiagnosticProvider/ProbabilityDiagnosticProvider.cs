using System;

namespace Microsoft.Azure.Devices.Client.DiagnosticProvider
{
    public class ProbabilityDiagnosticProvider : BaseDiagnosticProvider
    {
        private readonly Random _random = new Random();
        private int _randomNum;

        public ProbabilityDiagnosticProvider(SamplingRateSource source = SamplingRateSource.None, int samplingRate = 0) : base(source, samplingRate)
        {
            _randomNum = _random.Next(1, 101);
        }

        public override bool ShouldAddDiagnosticProperties()
        {
            return _randomNum <= SamplingRatePercentage;
        }

        public override void OnProcessCompleted()
        {
            base.OnProcessCompleted();
            _randomNum = _random.Next(1, 101);
        }
    }
}
