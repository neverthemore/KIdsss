// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
#if UNITY_5_3_OR_NEWER
// IMPORTANT: Used by the pure-dotnet client, DON'T REMOVE.
// Any changes to the Unity version of the request should be reflected
// in the HttpClient version.
// TODO: Separate Http client impl. with common options/policy layer (coherence/unity#1764)
#define UNITY
#endif

#if UNITY_5_3_OR_NEWER
// IMPORTANT: Used by the pure-dotnet client, DON'T REMOVE.
#define UNITY
#endif

namespace Coherence.Cloud
{
    using System.Net;
    using System.Threading.Tasks;
    using System.Net.Http;
#if UNITY
    using UnityEngine.Networking;
#endif

    public static class ReplicationServerUtils
    {
        public static async Task<bool> PingHttpServerAsync(string host, int port)
        {
#if UNITY
            var url = $"http://{host}:{port}/health";

            var request = new UnityWebRequest(url, HttpMethod.Get.Method);

            _ = request.SendWebRequest();

            while (!request.isDone)
            {
                await Task.Delay(10);
            }

            return request.responseCode == (long)HttpStatusCode.OK;
#else
            return await Task.FromResult(false);
#endif
        }

        public static bool PingHttpServer(string host, int port)
        {
#if UNITY
            var url = $"http://{host}:{port}/health";

            var request = new UnityWebRequest(url, HttpMethod.Get.Method);

            _ = request.SendWebRequest();

            while (!request.isDone)
            {
            }

            return request.responseCode == (long)HttpStatusCode.OK;
#else
            return false;
#endif
        }
    }
}

