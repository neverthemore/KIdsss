// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    public class GameServices : System.IDisposable
    {
        /// <summary>
        ///     Log in the coherence Cloud.
        /// </summary>
        public IAuthClient AuthService { get; }

        public MatchmakerClient MatchmakerService { get; }
        /// <summary>
        ///     Key Value Store service, you can enable it in the Game Services section, in your Project in the coherence Dashboard.
        /// </summary>
        public KvStoreClient KvStoreService { get; }

        internal readonly IAuthClientInternal authService;

        public GameServices(CloudCredentialsPair credentialsPair)
        {
            AuthService = credentialsPair.AuthClient;
            authService = credentialsPair.authClient;
            MatchmakerService = new MatchmakerClient( credentialsPair.RequestFactory, authService);
            KvStoreService = new KvStoreClient( credentialsPair.RequestFactory, authService);
        }

        public void Dispose() => KvStoreService.Dispose();
    }
}
