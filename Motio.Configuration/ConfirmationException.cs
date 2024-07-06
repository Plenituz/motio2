using System;

namespace Motio.Configuration
{
    public class ConfirmationException : Exception
    {
        public ConfirmationException(string message) : base(message)
        {
        }
    }
}
