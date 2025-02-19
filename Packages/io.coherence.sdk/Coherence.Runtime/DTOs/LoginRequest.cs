// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime
{
    using Newtonsoft.Json;

    internal enum LoginType
    {
        Guest = 1,
        Password = 2,
        SessionToken = 3
    }

#pragma warning disable 0649
    internal struct LoginRequest
    {
        [JsonProperty("type")]
        public LoginType Type;

        [JsonProperty("username")]
        public string Username;

        [JsonProperty("password")]
        public string Password;

        [JsonProperty("autosignup")]
        public bool Autosignup;
    }
#pragma warning restore 0649
}
