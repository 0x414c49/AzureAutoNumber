using System;

namespace AutoNumber.Exceptions
{
    public class UniqueIdGenerationException : Exception
    {
        public UniqueIdGenerationException(string message)
            : base(message)
        {
        }
    }
}