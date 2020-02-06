using System;

namespace AzureHailstone
{
    public class UniqueIdGenerationException : Exception
    {
        public UniqueIdGenerationException(string message)
            : base(message)
        {
        }
    }
}