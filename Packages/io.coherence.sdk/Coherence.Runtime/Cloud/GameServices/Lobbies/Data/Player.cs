// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;

    public struct Player : IEquatable<Player>, IComparable<Player>
    {
        public IReadOnlyList<CloudAttribute> Attributes => playerAttributes;
        
        [JsonProperty("id")]
        public string UserId;

        [JsonProperty("username")]
        public string Username;

        [JsonProperty("attributes")]
        internal List<CloudAttribute> playerAttributes;

        bool IEquatable<Player>.Equals(Player other) => Equals(other);

        public bool Equals(in Player other) => UserId == other.UserId;
        public override bool Equals(object obj) => obj is Player other && Equals(other);
        public int CompareTo(Player other) => string.Compare(UserId, other.UserId, StringComparison.Ordinal);
        public static bool operator ==(in Player left, in Player right) => left.Equals(right);
        public static bool operator !=(in Player left, in Player right) => !left.Equals(right);

        public override int GetHashCode() => UserId.GetHashCode();

        public CloudAttribute? GetAttribute(string key)
        {
            if (Attributes == null)
            {
                return null;
            }

            foreach (var attribute in Attributes)
            {
                if (attribute.Key.Equals(key))
                {
                    return attribute;
                }
            }

            return null;
        }
    }
}
