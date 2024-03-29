﻿using System;

namespace Microsoft.Azure.Devices.Client.Exceptions
{
    public class InvalidDiagTwinException : Exception
    {
        public InvalidDiagTwinException()
        {
        }

        public InvalidDiagTwinException(string message) : base(message)
        {
        }

        public InvalidDiagTwinException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
