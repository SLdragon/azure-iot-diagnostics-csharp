using System;

namespace Microsoft.Azure.Devices.Client.Exceptions
{
    public class SamplingPercentageOutOfRangeException : Exception
    {
        public SamplingPercentageOutOfRangeException()
        {
        }

        public SamplingPercentageOutOfRangeException(string message) : base(message)
        {
        }

        public SamplingPercentageOutOfRangeException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
