using System;

namespace IoTDeviceSDKWrapper.Exceptions
{
    public class PropertyConflictException : Exception
    {
        public PropertyConflictException()
        {
        }

        public PropertyConflictException(string message): base(message)
        {
        }

        public PropertyConflictException(string message, Exception inner): base(message, inner)
        {
        }
    }
}
