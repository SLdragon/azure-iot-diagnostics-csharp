using System;

namespace IoTDeviceSDKWrapper.Exceptions
{
    public class ProtocalNotSupportException : Exception
    {
        public ProtocalNotSupportException()
        {
        }

        public ProtocalNotSupportException(string message): base(message)
        {
        }

        public ProtocalNotSupportException(string message, Exception inner): base(message, inner)
        {
        }
    }
}
