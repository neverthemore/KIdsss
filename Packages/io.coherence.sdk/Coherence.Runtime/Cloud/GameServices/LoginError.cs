// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Cloud
{
    using System;
    using Runtime;

    /// <summary>
    /// Represents an <see cref="AuthClient"/> error that has occurred either during an attempted login operation,
    /// or after a successful login operation, when the client's connection to coherence Cloud has been forcefully
    /// closed by the server (<see cref="ErrorType.ConcurrentConnection"/>).
    /// </summary>
    /// <seealso cref="AuthClient.OnError"/>
    public sealed class LoginError : Exception
    {
        /// <summary>
        /// Describes the type of the error.
        /// </summary>
        public ErrorType Type;

        /// <summary>
        /// The raw json response from the server for the login request.
        /// </summary>
        internal string ResponseBody { get; }

        internal LoginError(ErrorType type, string message = "", string responseBody = "") : base(message)
        {
            Type = type;
            ResponseBody = responseBody;
        }

        public override string ToString() => Message is { Length: > 0 } ? Type + ": " + Message : Type.ToString();
    }
}
