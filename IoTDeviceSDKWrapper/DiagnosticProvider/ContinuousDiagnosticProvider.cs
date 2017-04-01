﻿using System;

namespace IoTDeviceSDKWrapper.DiagnosticProvider
{
    public class ContinuousDiagnosticProvider:BaseDiagnosticProvider
    {
        public ContinuousDiagnosticProvider(SamplingRateSource source = SamplingRateSource.None, int samplingRate = 0):base(source,samplingRate)
        {
        }

        public override bool NeedSampling(int count)
        {
            return Math.Floor((count - 2) * SamplingRatePercentage / 100.0) < Math.Floor((count - 1) * SamplingRatePercentage / 100.0);
        }
    }
}
