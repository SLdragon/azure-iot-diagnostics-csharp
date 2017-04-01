using System;

namespace IoTDeviceSDKWrapper.Exceptions
{
    public class InvalidDiagTwinException:Exception
    {
        public InvalidDiagTwinException()
        {
        }

        public InvalidDiagTwinException(string message): base(message)
        {
        }

        public InvalidDiagTwinException(string message, Exception inner): base(message, inner)
        {
        }
    }
}
