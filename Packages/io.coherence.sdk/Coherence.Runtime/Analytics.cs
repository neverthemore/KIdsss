// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime
{
    using Cloud;
    using Common;
    using Newtonsoft.Json;
    using Prefs;
    using System;
    using Log;
    using System.Threading;
    using System.Threading.Tasks;
    using Logger = Log.Logger;

    public class AnalyticsClient
    {
        private readonly Logger logger = Log.GetLogger<AnalyticsClient>();

        public string AnalyticsId { get; private set; }

        private async void OnConnect()
        {
            var request = new AnalyticsRequest
            {
                TimestampMs = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds(),
                AnalyticsId = AnalyticsId,
                EventName = "connection",
                SDKVersion = runtimeSettings.VersionInfo.Sdk,
                EngineVersion = runtimeSettings.VersionInfo.Engine,
                SimSlug = runtimeSettings.SimulatorSlug,
                SchemaId = runtimeSettings.SchemaID,
            };
            var body = Coherence.Utils.CoherenceJson.SerializeObject(request);
            try
            {
                await requestFactory.SendRequestAsync("/analytics", "POST", body, null, $"{nameof(AnalyticsClient)}.Analytics", string.Empty);
            }
            catch (RequestException requestException) when (requestException.ErrorCode == ErrorCode.TooManyRequests)
            {
                // Ignore throttling errors.
            }
            catch(Exception exception)
            {
                logger.Warning(Warning.RuntimeAnalyticsFailedToWrite, ("exception", exception));
            }
        }

        private IRequestFactory requestFactory;
        private IRuntimeSettings runtimeSettings;
        private IAuthClientInternal authClient;
        private string uniqueId;

        public AnalyticsClient(CloudCredentialsPair credentialsPair, IRuntimeSettings runtimeSettings)
        {
            this.requestFactory = credentialsPair.RequestFactory;
            this.authClient = credentialsPair.authClient;
            this.runtimeSettings = runtimeSettings;
            this.uniqueId = authClient.UniqueID;

            if (string.IsNullOrEmpty(runtimeSettings.ProjectID))
            {
                return;
            }

            ResolveAnalyticsId();

            requestFactory.OnWebSocketConnect += OnConnect;
        }

        private void ResolveAnalyticsId()
        {
            var prefsKey = GetAnalyticsIdPrefsKey();
            AnalyticsId = Prefs.GetString(prefsKey);

            if (!string.IsNullOrEmpty(AnalyticsId))
            {
                return;
            }

            AnalyticsId = Guid.NewGuid().ToString();
            Prefs.SetString(prefsKey, AnalyticsId);
        }

        private string GetAnalyticsIdPrefsKey()
        {
            return Utils.PrefsUtils.Format(PrefsKeys.AnalyticsId, runtimeSettings.ProjectID, uniqueId);
        }
    }
}
