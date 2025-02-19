// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

#pragma warning disable 0649
    public struct LoginResponse
    {
        [JsonProperty("session_token")]
        public string SessionToken;

        [JsonProperty("user_id")]
        public string UserId;

        [JsonProperty("kv")]
        public List<KvPair> KvStoreState;

        [JsonProperty("lobbies")]
        public List<string> LobbyIds;
    }
#pragma warning restore 0649
}
