// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Cloud
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Runtime;

    /// <summary>
    /// Represents the result of an <see cref="IAuthClient"/> login operation.
    /// </summary>
    /// <seealso cref="AuthClient.LoginAsGuest"/>
    /// <seealso cref="AuthClient.LoginWithPassword"/>
    /// <seealso cref="AuthClient.LoginWithToken"/>
    /// <seealso cref="AuthClient.OnLogin"/>
    public record LoginResult
    {
        /// <summary>
        /// Describes the type of the result.
        /// <para>
        /// <see cref="Result.Success"/> if the login operation was successful; otherwise, the type of login failure.
        /// </para>
        /// </summary>
        public Result Type { get; }

        /// <summary>
        /// Identifier for the user, if they successfully logged in; otherwise, an empty string.
        /// </summary>
        public string UserId { get; }

        /// <summary>
        /// Username of the user that was successfully logged in <see cref="AuthClient.LoginWithPassword">using a password</see>,
        /// or <see cref="AuthClient.LoginAsGuest">as a guest</see>; otherwise, an empty string.
        /// </summary>
        public string Username { get; }

        /// <summary>
        /// Automatically generated random password for the user, if the user was successfully
        /// <see cref="AuthClient.LoginAsGuest"> logged in as a guest</see>; otherwise, an empty string.
        /// </summary>
        public string GuestPassword { get; }

        /// <summary>
        /// Token uniquely identifying the logged-in user or guest.
        /// <para>
        /// The token can be stored on the user's device locally, and later used to
        /// <see cref="IAuthClient.LoginWithToken">log in</see> to coherence Cloud again using the same credentials,
        /// without the user needing to provide them again.
        /// </para>
        /// </summary>
        public SessionToken SessionToken { get; }

        public ErrorType ErrorType { get; }
        public string ErrorMessage { get; }

        public IReadOnlyList<KeyValuePair<string, string>> KeyValuePairStoreState { get; }
        public IReadOnlyList<string> LobbyIds { get; }

        /// <summary>
        /// <see langword="true"/> if the login operation was successful, or if it was cancelled because
        /// the client is <see cref="Result.AlreadyLoggedIn">already logged in</see>; otherwise, <see langword="false"/>.
        /// </summary>
        public bool LoggedIn => Type is Result.Success or Result.AlreadyLoggedIn;

        private readonly LoginResponse? response;

        internal LoginResult(Result type, LoginError failure)
        {
            Username = "";
            GuestPassword = "";
            Type = type;
            SessionToken = SessionToken.None;
            UserId = "";
            KeyValuePairStoreState = Array.Empty<KeyValuePair<string, string>>();
            LobbyIds = Array.Empty<string>();
            ErrorType = failure.Type;
            ErrorMessage = failure.Message;
        }

        internal LoginResult(string username, string guestPassword, Result resultType, LoginResponse response)
        {
            Username = username;
            GuestPassword = guestPassword;
            Type = resultType;
            SessionToken = new(response.SessionToken);
            UserId = response.UserId;
            KeyValuePairStoreState = response.KvStoreState?
                .Select(r => new KeyValuePair<string, string>(r.Key, r.Value))
                .ToArray() ?? Array.Empty<KeyValuePair<string, string>>();
            LobbyIds = response.LobbyIds ?? new List<string>(0);
            this.response = response;
            ErrorMessage = "";
        }

        internal bool TryGetResponse(out LoginResponse response)
        {
            if(this.response.HasValue)
            {
                response = this.response.Value;
                return true;
            }

            response = default;
            return false;
        }

        public static implicit operator Result(LoginResult loginResult) => loginResult.Type;
        public static bool operator ==(LoginResult result, Result type) => result?.Type == type;
        public static bool operator !=(LoginResult result, Result type) => result?.Type != type;
        public bool Equals(Result type) => Type == type;
        public override string ToString() => ErrorMessage is { Length: > 0 } ? Type + ": " + ErrorMessage : Type.ToString();
    }
}
