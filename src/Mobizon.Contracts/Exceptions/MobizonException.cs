using System;

namespace Mobizon.Contracts.Exceptions
{
    /// <summary>
    /// Base exception for all errors raised by the Mobizon.Net SDK.
    /// </summary>
    public class MobizonException : Exception
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MobizonException"/> with a specified error message.
        /// </summary>
        /// <param name="message">A message that describes the error.</param>
        public MobizonException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MobizonException"/> with a specified error message
        /// and a reference to the inner exception that caused this exception.
        /// </summary>
        /// <param name="message">A message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of this exception.</param>
        public MobizonException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
