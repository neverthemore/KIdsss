// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
#define UNITY
#endif

namespace Coherence.Runtime
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Cloud;
    using Coherence.Utils;
    using Common;
    using Connection;
    using Log;
    using Newtonsoft.Json;
    using Utils;
#if UNITY_WEBGL && !UNITY_EDITOR
    using Web;
#endif

    public sealed class WebSocket : IUpdatable, IDisposableInternal
    {
        private static readonly TimeSpan TimeOutSpan = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan TimeoutCheckSpan = TimeSpan.FromSeconds(1);

        private readonly IRuntimeSettings runtimeSettings;
        private readonly RequestIdSource idSource;
        private readonly Logger logger = Log.GetLogger<WebSocket>();
        private ClientWebSocket ws;
        private bool wsConnected = false;
        private bool wsConnecting = false;
        private ConcurrentQueue<String> receiveQueue;
        private ConcurrentQueue<(int counter, string requestID)> failQueue;
        private List<RequestCallback> requestCallbacks;
        private Dictionary<string, RequestCallback> pushCallbacks;
        private Task receiveTask;
        private Task sendTask = Task.CompletedTask;
        private CancellationTokenSource abortToken;
        private readonly Stopwatch connectBackoffStopwatch = new Stopwatch();
        private TimeSpan connectBackoff = Constants.minBackoff;
        private DateTime nextCheck;
        private string resumeId;
        private bool validatedWsParameters;

#if UNITY_WEBGL
        private const string pingEndpoint = "/health";
        private const int pingIntervalSeconds = 30;
        private DateTime nextPingTime;
        private bool isWebGlConnected;
        private int id;
#endif

        public bool Enabled { get; private set; }
        public bool IsConnecting() => ws?.State == WebSocketState.Connecting;
        public bool IsConnected()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return isWebGlConnected;
#else
            return ws?.State == WebSocketState.Open;
#endif
        }

        public Int64 ServerTimestamp { get; private set; }
        string IDisposableInternal.InitializationContext { get; set; }
        string IDisposableInternal.InitializationStackTrace { get; set; }
        bool IDisposableInternal.IsDisposed { get; set; }

        private enum Event { Connected, Disconnected }
        private ConcurrentQueue<Event> eventQueue = new ConcurrentQueue<Event>();

        public event Action OnConnect;
        public event Action OnDisconnect;
        public event Action OnWebSocketFail;
#pragma warning disable CS0067 // this had to be added to get WebGL builds to complete successfully
#pragma warning disable CS0414
        public event Action<string> OnWebSocketParametersNotValid;
#pragma warning restore CS0067
#pragma warning restore CS0414
        public event Action<string> OnReceive;

        public WebSocket(IRuntimeSettings runtimeSettings, RequestIdSource idSource)
        {
            this.OnInitialized();
            this.runtimeSettings = runtimeSettings;
            this.idSource = idSource;
            receiveQueue = new ConcurrentQueue<string>();
            failQueue = new ConcurrentQueue<(int, string)>();
            requestCallbacks = new List<RequestCallback>();
            pushCallbacks = new Dictionary<string, RequestCallback>();
            nextCheck = DateTime.UtcNow;
#if UNITY_WEBGL
            id = GetHashCode();
#endif
        }

        ~WebSocket()
        {
#if UNITY
            if (SimulatorUtility.UseSharedCloudCredentials)
            {
                logger.Info($"Won't call {nameof(Dispose)} even through finalizer was executed, because {nameof(SimulatorUtility)}.{nameof(SimulatorUtility.UseSharedCloudCredentials)} is True, and we want to preserve WebSocket instance's integrity for Simulators running in the Cloud.");
                return;
            }
#endif

            if (!this.OnFinalized())
            {
                logger.Warning(Warning.RuntimeWebsocketResourceLeak, this.GetResourceLeakWarningMessage());
            }
        }

        public void Connect()
        {
            if (ws != null)
            {
                logger.Error(Error.RuntimeWebsocketAlreadyConnected, idSource.IdBaseLogParam);
                return;
            }

            logger.Debug("Enabled");
            Enabled = true;
        }

        public void Disconnect() => _ = DisconnectAsync();

        private async Task DisconnectAsync()
        {
            logger.Debug("Disabled", idSource.IdBaseLogParam);
            Enabled = false;
            await CloseSocket();
        }

        public bool AddPushCallback(string requestID, OnRequest callback)
        {
            if (pushCallbacks.ContainsKey(requestID))
            {
                return false;
            }

            pushCallbacks[requestID] = new RequestCallback
            {
                requestId = requestID,
                onRequest = callback,
            };

            return true;
        }

        private string endpoint
        {
            get
            {
                return runtimeSettings.WebSocketEndpoint + "?runtime_key=" + runtimeSettings.RuntimeKey;
            }
        }

        private string clientVersion
        {
            get
            {
                return "unity-sdk-v" + runtimeSettings.VersionInfo.Sdk;
            }
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private void OnWebMessage(string message)
        {
            logger.Debug($"Received message", idSource.IdBaseLogParam);
            receiveQueue.Enqueue(message);
        }

        private void OnWebConnect()
        {
            logger.Debug("Connected", idSource.IdBaseLogParam);
            eventQueue.Enqueue(Event.Connected);
            isWebGlConnected = true;
        }

        private void OnWebDisconnect()
        {
            logger.Debug("Disconnected", idSource.IdBaseLogParam);
            eventQueue.Enqueue(Event.Disconnected);
            Backoff();
            isWebGlConnected = false;
        }

        private void OnWebError(string error)
        {
            logger.Error(Error.RuntimeWebsocketWebError,
                idSource.IdBaseLogParam,
                ("message", error));
            WebSocketInterop.DisconnectSocket(id);
            Backoff();
            isWebGlConnected = false;
        }

        // This is needed for the webgl version, since there we can not instantiate a ClientWebSocket
        private bool wsInitialized = false;

        private async Task OpenSocket()
        {
            logger.Debug("Connecting", idSource.IdBaseLogParam);

            if (!wsInitialized)
            {
                WebSocketInterop.InitializeConnection(id, OnWebMessage, OnWebConnect, OnWebDisconnect, OnWebError);
                wsInitialized = true;
            }

            string requestID = idSource.Next();
            string finalEndpoint = GetFinalWsEndpoint(requestID);

            WebSocketInterop.ConnectSocket(id, finalEndpoint);
            wsConnected = true;
            nextPingTime = DateTime.UtcNow.AddSeconds(pingIntervalSeconds);

            await Task.Delay(1);
        }
#else

        private async Task OpenSocket()
        {
            abortToken = new CancellationTokenSource();
            ws = new ClientWebSocket();
            wsConnecting = true;

            string requestID = idSource.Next();
            string finalEndpoint = GetFinalWsEndpoint(requestID);

            logger.Debug("Connecting", ("requestID", requestID), ("endpoint", finalEndpoint));

            try
            {
                await ws.ConnectAsync(new Uri(finalEndpoint), abortToken.Token);
                connectBackoff = Constants.minBackoff;
            }
            catch (WebSocketException webSocketException)
            {
                if (webSocketException.InnerException is OperationCanceledException)
                {
                    // This particular exception is expected if the
                    // connection is closed while connecting, so it's not an error.
                    wsConnecting = false;
                    return;
                }

                if (!await ValidateWebSocketParameters())
                {
                    return;
                }

                logger.Error(Error.RuntimeWebsocketCloudFailed,
                    ("requestID", requestID),
                    ("endpoint", endpoint),
                    ("backoff", connectBackoff),
                    ("errorCode", webSocketException.ErrorCode),
                    ("wsErrorCode", webSocketException.WebSocketErrorCode),
                    ("exception", webSocketException));

                Backoff();
                wsConnecting = false;

                return;
            }
            catch (Exception ex)
            {
                Backoff();
                logger.Error(Error.RuntimeWebsocketOpenFailed,
                    ("requestID", requestID),
                    ("endpoint", endpoint),
                    ("backoff", connectBackoff),
                    ("exception", ex));
                wsConnecting = false;
                return;
            }

            wsConnected = true;
            wsConnecting = false;
            receiveTask = RunReceive();
            eventQueue.Enqueue(Event.Connected);

            logger.Debug("Opening socket success", ("requestID", requestID));
#if UNITY_WEBGL
            nextPingTime = DateTime.UtcNow.AddSeconds(pingIntervalSeconds);
#endif
        }

        private async Task<bool> ValidateWebSocketParameters()
        {
            if (validatedWsParameters)
            {
                return true;
            }

            try
            {
                await Request.ExecuteAsync("/validate", "GET", null, null, runtimeSettings, string.Empty,
                    idSource.Next(), true);
            }
            catch (RequestException requestException)
            {
                logger.Warning(Warning.RuntimeWebsocketWebError, requestException.Message);

                await DisconnectAsync();

                OnWebSocketParametersNotValid?.Invoke(requestException.Message);
                return false;
            }

            validatedWsParameters = true;

            return validatedWsParameters;
        }

        private async Task RunReceive()
        {
            var buffer = new byte[1 * 1024 * 1024];
            int receivedBytes = 0;

            try
            {
                while (!abortToken.IsCancellationRequested)
                {
                    try
                    {
                        if (ws == null)
                        {
                            return;
                        }

                        var receiveBuffer = new ArraySegment<byte>(buffer, receivedBytes, buffer.Length - receivedBytes);
                        var received = await ws.ReceiveAsync(receiveBuffer.AsMemory(), abortToken.Token);
                        receivedBytes += received.Count;

                        if (received.MessageType == WebSocketMessageType.Close)
                        {
                            await CloseSocket();
                            return;
                        }

                        if (!received.EndOfMessage)
                        {
                            if (receivedBytes >= buffer.Length)
                            {
                                throw new WebSocketException($"Response message size over the limit of {buffer.Length} bytes");
                            }

                            continue;
                        }
                    }
                    catch (NullReferenceException)
                    {
                        // Mono specific case - that's nullref from the ClientWebSocket internals
                        // which means that the connection was closed.
                        throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely);
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
                    if (!string.IsNullOrEmpty(message))
                    {
                        logger.Debug($"Received message", idSource.IdBaseLogParam
#if UNITY_EDITOR
                            ,("message", message)
#endif
                            );
                        receiveQueue.Enqueue(message);
                    }

                    receivedBytes = 0;
#if UNITY_WEBGL
                    nextPingTime = DateTime.UtcNow.AddSeconds(pingIntervalSeconds);
#endif
                }
            }
            catch (Exception exception)
            {
                switch (exception)
                {
                    case OperationCanceledException _:
                        break;
                    case WebSocketException { WebSocketErrorCode: WebSocketError.ConnectionClosedPrematurely }:
                        logger.Warning(Warning.RuntimeWebsocketClosedPrematurely, idSource.IdBaseLogParam);
                        break;
                    default:
                        logger.Error(Error.RuntimeWebsocketReceiveException,
                            idSource.IdBaseLogParam,
                            ("exception", exception));
                        break;
                }

                await CloseSocket();
            }
        }
#endif // UNITY_WEBGL && !UNITY_EDITOR

        private string GetFinalWsEndpoint(string requestID)
        {
            var finalEndpoint =
                $"{endpoint}&client={clientVersion}&engine={runtimeSettings.VersionInfo.Engine}&schema_id={runtimeSettings.SchemaID}&req_id={requestID}";
            return finalEndpoint;
        }

        private void Backoff()
        {
            connectBackoff = TimeSpan.FromSeconds(Math.Min(connectBackoff.TotalSeconds * 2, Constants.maxBackoff.TotalSeconds));
            logger.Trace("Backoff", idSource.IdBaseLogParam, ("backoff", connectBackoff));
            OnWebSocketFail?.Invoke();
            Reset();
        }

        private void Reset()
        {
            ws?.Dispose();
            ws = null;
            wsConnected = false;
        }


#if UNITY_WEBGL && !UNITY_EDITOR
        private async Task CloseSocket()
        {
            logger.Debug("Closing socket", idSource.IdBaseLogParam);

            WebSocketInterop.DisconnectSocket(id);
            Reset();
            eventQueue.Enqueue(Event.Disconnected);

            await Task.Delay(1);
        }
#else
        private async Task CloseSocket()
        {
            if (ws == null)
            {
                return;
            }

            logger.Debug("Closing socket", idSource.IdBaseLogParam);

            try
            {
                if (IsConnected())
                {
                    await ws?.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    abortToken.Cancel();
                }
            }
            catch
            {
                // This is fine.
            }

            Reset();
            eventQueue.Enqueue(Event.Disconnected);
        }
#endif

        internal void SendRequest(string path, string method, string body, Dictionary<string, string> headers, string sessionToken, OnRequest callback)
        {
            if (!Enabled)
            {
                logger.Error(Error.RuntimeWebsocketSendFailedNotEnabled,
                    ("path", path),
                    ("method", method),
                    idSource.IdBaseLogParam);
                callback((int)HttpStatusCode.ServiceUnavailable, null);
                return;
            }

            string requestId = idSource.Next(out int counter);
            RequestMeta request = new RequestMeta
            {
                Id = counter,
                ResumeId = resumeId,
            };

            logger.Debug($"Building Request", ("requestID", requestId),
                ("path", path), ("method", method), ("resumeID", resumeId),
                ("headers", $"[{string.Join(", ", headers?.Select(kv => $"{kv.Key}: {kv.Value}") ?? Array.Empty<string>())}]"));

            request.Headers = new Dictionary<string, string>();
            request.Headers[CloudService.RequestIDHeader] = requestId;
            if (!string.IsNullOrEmpty(sessionToken))
            {
                request.Headers["X-Coherence-Play-Session"] = sessionToken;
            }

            if (!(headers is null))
            {
                foreach (var header in headers)
                {
                    request.Headers[header.Key] = header.Value;
                }
            }
            request.Method = method;
            request.Path = path;

            RequestCallback requestCallback = new RequestCallback
            {
                onRequest = callback,
                maxTime = DateTime.UtcNow.Add(TimeOutSpan),
                requestId = requestId,
                meta = request
            };
            requestCallbacks.Add(requestCallback);

            byte[] b1 = Encoding.UTF8.GetBytes(CoherenceJson.SerializeObject(request));
            byte[] b2 = Encoding.UTF8.GetBytes("\n");
            byte[] b3 = Encoding.UTF8.GetBytes(body ?? "{}");
            byte[] buffer = new byte[b1.Length + b2.Length + b3.Length];
            Buffer.BlockCopy(b1, 0, buffer, 0, b1.Length);
            Buffer.BlockCopy(b2, 0, buffer, b1.Length, b2.Length);
            Buffer.BlockCopy(b3, 0, buffer, b1.Length + b2.Length, b3.Length);
            SendBuffer(request.Id, requestId, buffer);
#if UNITY_WEBGL
            nextPingTime = DateTime.UtcNow.AddSeconds(pingIntervalSeconds);
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private void SendBuffer(int _, string _t, byte[] buffer)
        {
            WebSocketInterop.SendSocketMessage(id, Encoding.UTF8.GetString(buffer));
        }
#else

        private void SendBuffer(int requestCounter, string requestId, byte[] buffer)
        {
            sendTask = sendTask.ContinueWith(async (task) => await RunSend(requestCounter, requestId, buffer)).Unwrap();
        }

        private async Task RunSend(int requestCounter, string requestId, byte[] buffer)
        {
            while (!IsConnected() && connectBackoffStopwatch.Elapsed.Seconds < 5)
            {
                await Task.Delay(50);
            }

            if (!IsConnected())
            {
                failQueue.Enqueue((requestCounter, requestId));
                return;
            }

            var sendBuf = new ArraySegment<byte>(buffer);
            try
            {
                logger.Trace($"Sending Request", ("requestID", requestId), ("resumeID", resumeId), ("data", Encoding.Default.GetString(buffer)));
                await ws.SendAsync(sendBuf, WebSocketMessageType.Text, true, abortToken.Token);
            }
            catch (Exception exception)
            {
                switch (exception)
                {
                    case OperationCanceledException _:
                    case WebSocketException { WebSocketErrorCode: WebSocketError.InvalidState }:
                        break;
                    case NullReferenceException _:
                    case WebSocketException { WebSocketErrorCode: WebSocketError.ConnectionClosedPrematurely }:
                        logger.Warning(Warning.RuntimeWebsocketClosedPrematurely, ("requestID", requestId));
                        break;
                    default:
                        logger.Error(Error.RuntimeWebsocketSendException,
                            ("requestID", requestId),
                            ("exception", exception));
                        break;
                }

                failQueue.Enqueue((requestCounter, requestId));
                await CloseSocket();
            }
        }
#endif

        public void Update()
        {
            if (Enabled && !wsConnected && !wsConnecting)
            {
                if (!connectBackoffStopwatch.IsRunning || connectBackoffStopwatch.Elapsed > connectBackoff)
                {
                    _ = OpenSocket();
                    connectBackoffStopwatch.Restart();
                }
            }

            while (eventQueue.TryDequeue(out Event ev))
            {
                switch (ev)
                {
                    case Event.Connected:
                        OnConnect?.Invoke();
                        break;
                    case Event.Disconnected:
                        OnDisconnect?.Invoke();
                        break;
                    default: break;
                }
            }

            while (failQueue.TryDequeue(out (int counter, string ID) request))
            {
                if (FindRequestCallback(request.counter, request.ID, out RequestCallback cb))
                {
                    logger.Warning(Warning.RuntimeWebsocketRequestFailed,
                        ("requestID", request.ID),
                        ("path", cb.meta.Path),
                        ("method", cb.meta.Method)
#if UNITY_EDITOR
                        ,("headers", $"[{string.Join(", ", cb.meta.Headers?.Select(kv => $"{kv.Key}: {kv.Value}") ?? Array.Empty<string>())}]")
#endif
                        );
                    RemoveRequestCallback(request.counter, request.ID);
                    cb.onRequest((int)HttpStatusCode.ServiceUnavailable, null);
                }
                else
                {
                    logger.Warning(Warning.RuntimeWebsocketCallbackNotFoundForFailedRequest, ("requestID", request.ID));
                }
            }

            while (receiveQueue.TryDequeue(out String s))
            {
                HandleResponse(s);
            }

            var now = DateTime.UtcNow;
            if (nextCheck < now)
            {
                nextCheck = now.Add(TimeoutCheckSpan);
                var timeouts = new List<RequestCallback>();
                foreach (var callback in requestCallbacks)
                {
                    if (callback.maxTime < now)
                    {
                        timeouts.Add(callback);
                    }
                }

                foreach (RequestCallback cb in timeouts)
                {
                    logger.Warning(Warning.RuntimeWebsocketRequestTimedOut,
                        ("requestID", cb.requestId),
                        ("path", cb.meta.Path),
                        ("method", cb.meta.Method)
#if UNITY_EDITOR
                        ,("headers", $"[{string.Join(", ", cb.meta.Headers?.Select(kv => $"{kv.Key}: {kv.Value}") ?? Array.Empty<string>())}]")
#endif
                        );
                    RemoveRequestCallback(cb.meta.Id, cb.requestId);
                    cb.onRequest?.Invoke((int)HttpStatusCode.RequestTimeout, null);
                }
            }

#if UNITY_WEBGL
            if (Enabled && wsConnected && nextPingTime < now)
            {
                nextPingTime = now.AddSeconds(pingIntervalSeconds);
                SendRequest(pingEndpoint, "GET", null, null, null,(code, body, requestId) => {});
            }
#endif
        }

        private void HandleResponse(string text)
        {
            int pos = text.IndexOf("\n", StringComparison.Ordinal);
            string metaSrc = pos == -1 ? text : text.Substring(0, pos);
            string body = pos == -1 ? null : text.Substring(pos + 1);

            ResponseMeta meta;

            try
            {
                meta = CoherenceJson.DeserializeObject<ResponseMeta>(metaSrc);
            }
            catch (Exception exception)
            {
                logger.Error(Error.RuntimeWebsocketFailedToDeserializeResponse,
                    idSource.IdBaseLogParam,
#if UNITY_EDITOR
                    ("response", text),
#endif
                    ("exception", exception));
                return;
            }

            ServerTimestamp = meta.Timestamp;
            resumeId = meta.ResumeId;

            if (meta.Id == 0 && string.IsNullOrEmpty(meta.RequestId))
            {
                logger.Debug("Received ID-less message", idSource.IdBaseLogParam,
                    ("requestID", meta.RequestId), ("statusCode", meta.StatusCode));
                OnReceive?.Invoke(body);
                return;
            }

            RequestCallback cb;
            if (FindPushCallback(meta.RequestId, out cb))
            {
                logger.Debug($"Received Push Message", ("requestID", cb.requestId), ("resumeID", meta.ResumeId), ("statusCode", meta.StatusCode)
#if UNITY_EDITOR
                    ,("body", body)
#endif
                    );
                cb.onRequest?.Invoke(meta.StatusCode, body, meta.RequestId);
                return;
            }

            if (!FindRequestCallback(meta.Id, meta.RequestId, out cb))
            {
                logger.Error(Error.RuntimeWebsocketMissingResponseCallback,
                    idSource.IdBaseLogParam,
                    ("requestID", meta.RequestId),
                    ("statusCode", meta.StatusCode));
                return;
            }

            logger.Debug($"Received Response Message", ("requestID", cb.requestId), ("responseID", meta.RequestId), ("resumeID", meta.ResumeId),
                ("path", cb.meta.Path), ("method", cb.meta.Method), ("statusCode", meta.StatusCode)
#if UNITY_EDITOR
                ,("body", body)
#endif
                );

            RemoveRequestCallback(meta.Id, cb.requestId);
            cb.onRequest?.Invoke(meta.StatusCode, body, meta.RequestId);
        }

        private bool FindPushCallback(string requestID, out RequestCallback callback)
        {
            return pushCallbacks.TryGetValue(requestID, out callback);
        }

        private bool FindRequestCallback(int counter, string requestID, out RequestCallback callback)
        {
            foreach (RequestCallback cb in requestCallbacks)
            {
                if (cb.requestId == requestID || cb.meta.Id == counter)
                {
                    callback = cb;
                    return true;
                }
            }

            callback = default;
            return false;
        }

        private bool RemoveRequestCallback(int counter, string requestID)
        {
            for (int i = 0; i < requestCallbacks.Count; i++)
            {
                RequestCallback callback = requestCallbacks[i];
                if (callback.requestId == requestID || callback.meta.Id == counter)
                {
                    requestCallbacks.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public void Dispose()
        {
            if (this.OnDisposed())
            {
                return;
            }

            if (ws is not null)
            {
                logger.Debug("Disposing socket", idSource.IdBaseLogParam);

                try
                {
                    abortToken.Cancel();
                    ws.Dispose();
                }
                catch
                {
                    // This is fine.
                }

                ws = null;
            }

            wsConnected = false;
            eventQueue.Clear();
            abortToken?.Dispose();
            logger?.Dispose();
            OnConnect = null;
            OnDisconnect = null;
            OnWebSocketFail = null;
            OnWebSocketParametersNotValid = null;
            OnReceive = null;
        }
    }
}
