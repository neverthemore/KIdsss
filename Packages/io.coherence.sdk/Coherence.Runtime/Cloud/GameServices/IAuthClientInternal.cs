// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Cloud
{
    /// <summary>
    /// Exposes additional members on top of the ones already offered by <see cref="IAuthClient"/>
    /// for internal-only use.
    /// </summary>
    /// <inheritdoc/>
    internal interface IAuthClientInternal : IAuthClient
    {
        new string UserId { get; }
        new SessionToken SessionToken { get; }

        [Deprecated("15/10/2024", 1, 3, 1, Reason="coherence/unity#6843")]
        string UniqueID { get; }
    }
}
