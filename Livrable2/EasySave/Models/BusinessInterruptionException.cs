// This is a NEW file. Place it in a suitable location, e.g., EasySave.Models namespace.
using System;

namespace EasySave.Models
{
    public class BusinessSoftwareInterruptionException : Exception
    {
        public BusinessSoftwareInterruptionException(string message) : base(message) { }
        public BusinessSoftwareInterruptionException(string message, Exception innerException) : base(message, innerException) { }
    }
}