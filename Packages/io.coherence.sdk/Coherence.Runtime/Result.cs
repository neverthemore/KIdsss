// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime
{
    using System.Collections.Generic;

    /// <summary>
    /// Specifies the different types of outcomes that can occur when attempting to log in using <see cref="IAuthClient"/>.
    /// </summary>
    /// <seealso cref="LoginResult"/>
    public enum Result
    {
        /// <summary>
        /// Log in operation was completed successfully.
        /// </summary>
        Success = 1,

        /// <inheritdoc cref="ErrorType.ServerError"/>
        ServerError = 2,

        /// <inheritdoc cref="ErrorType.InvalidCredentials"/>
        InvalidCredentials = 3,

        /// <inheritdoc cref="ErrorType.FeatureDisabled"/>
        FeatureDisabled = 4,

        /// <inheritdoc cref="ErrorType.InvalidResponse"/>
        InvalidResponse = 5,

        /// <inheritdoc cref="ErrorType.TooManyRequests"/>
        TooManyRequests = 6,

        /// <inheritdoc cref="ErrorType.AlreadyLoggedIn"/>
        AlreadyLoggedIn = 7
    }

    internal static class ResultUtils
    {
        private static readonly Dictionary<ErrorCode, Result> errorToResultMap = new()
        {
            { ErrorCode.InvalidCredentials, Result.InvalidCredentials },
            { ErrorCode.FeatureDisabled, Result.FeatureDisabled },
            { ErrorCode.TooManyRequests, Result.TooManyRequests }
        };

        public static Result? ErrorToResult(ErrorCode error)
        {
            return errorToResultMap.TryGetValue(error, out Result result) ? result : default;
        }
    }
}
