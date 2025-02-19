// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime
{
    using Newtonsoft.Json;

#pragma warning disable 649
    public struct PlayerPayload
    {
        [JsonProperty("user_id")]
        public string UserId;

        [JsonProperty("team")]
        public string Team;

        [JsonProperty("score")]
        public int Score;

        [JsonProperty("payload")]
        public string Payload;
    }
#pragma warning restore 649
}
