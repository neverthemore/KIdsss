// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
// IMPORTANT: Used by the pure-dotnet client, DON'T REMOVE.
#define UNITY
#endif

namespace Coherence.Cloud
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Common;
    using Log;
    using Prefs;
    using Runtime;
    using Runtime.Utils;
    using Utils;
    using Logger = Log.Logger;

    /// <summary>
    /// Specifies a set of methods that can be used to log in to coherence Cloud, properties to determine
    /// if we are currently logged in or not, and events for getting notified about relevant things happening.
    /// </summary>
    public sealed class AuthClient : IAuthClientInternal, IDisposableInternal
    {
        /// <inheritdoc/>
        public event Action<LoginResponse> OnLogin;

        /// <inheritdoc/>
        public event Action OnLogout;

        /// <inheritdoc/>
        public event Action<LoginError> OnError;

        /// <inheritdoc/>
        public bool LoggedIn { get; private set; }

        SessionToken IAuthClientInternal.SessionToken => sessionToken;
        string IAuthClientInternal.UserId => userId;
        string IAuthClientInternal.UniqueID => uniqueId;
        string IDisposableInternal.InitializationContext { get; set; }
        string IDisposableInternal.InitializationStackTrace { get; set; }
        bool IDisposableInternal.IsDisposed { get; set; }

        #region Obsolete
        [Deprecated("09/2024", 1, 3, 1)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("The OnSessionRefreshed event will be removed in a future version. " +
                  "The session token can instead be acquired from the result of " + nameof(LoginAsGuest) + " / " + nameof(LoginWithPassword) + " / " + nameof(LoginWithToken) + ".")]
        public event Action<Result> OnSessionRefreshed;

        [Deprecated("09/2024", 1, 3, 1)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("The OnConcurrentConnection event will be removed in a future version. " +
                  "Subscribe to the " + nameof(OnError) + " even instead, and check if the argument's " + nameof(LoginError.Type) + " equals " + nameof(ErrorType.ConcurrentConnection) + ".")]
        public event Action<ConnectionClosedResponse> OnConcurrentConnection;

        [Deprecated("09/2024", 1, 3, 1)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("The UserName property will be removed in a future version. " +
                  "The username can instead be acquired from the result of " + nameof(LoginAsGuest) + " / " + nameof(LoginWithPassword) + " / " + nameof(LoginWithToken) + ".")]
        public string UserName { get; private set; } = "";

        [Deprecated("09/2024", 1, 3, 1)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("The GuestPassword property will be removed in a future version.")]
        public string GuestPassword { get; private set; } = "";

        [Deprecated("09/2024", 1, 3, 1)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("The UserId property will be removed in a future version. " +
                  "The user id can instead be acquired from the result of " + nameof(LoginAsGuest) + " / " + nameof(LoginWithPassword) + " / " + nameof(LoginWithToken) + ".")]
        public string UserId => userId;

        [Deprecated("09/2024", 1, 3, 0)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("The SessionToken property will be removed in a future version. " +
                  "The session token can instead be acquired from the result of " + nameof(LoginAsGuest) + " / " + nameof(LoginWithPassword) + " / " + nameof(LoginWithToken) + ".")]
        public string SessionToken => sessionToken;

        [Deprecated("09/2024", 1, 3, 0)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("The SessionTokenRefreshResult property will be removed in a future version. " +
                  "Use the return value of the login method instead.")]
        public Result? SessionTokenRefreshResult { get; private set; }
        #endregion

        private const string SimulatorInCloudUniqueId = "SimulatorInCloud";
        private const string ConnectionClosedPath = "/connection/closed";

        private readonly IRequestFactory requestFactory;
        private readonly TimeSpan simulatorTokenRefreshPeriodInDays = TimeSpan.FromDays(1f);
        private readonly string uniqueId;
        private string userId;
        private SessionToken sessionToken;
        private readonly string projectId;
        private readonly bool hasPooledId;
        private CancellationTokenSource initialAuthCancellationToken;
        private Task refreshTokenTask;
        private Action onWebSocketConnect;
        private readonly Logger logger = Log.GetLogger<AuthClient>();

        /// <summary>
        /// Initializes a new instance of <see cref="AuthClient"/> for a player.
        /// </summary>
        public static AuthClient ForPlayer(IRequestFactory requestFactory, string projectId)
            => ForPlayer(requestFactory, projectId, null, false);

        /// <inheritdoc cref="ForPlayer(IRequestFactory, string)"/>
        [Deprecated("15/10/2024", 1, 4, 0, Reason = "coherence/unity#6843")]
        internal static AuthClient ForPlayer(IRequestFactory requestFactory, string projectId, string uniqueId = "", bool autoLoginAsGuest = false)
        {
            var authClient = new AuthClient(requestFactory, projectId: projectId, uniqueId: uniqueId);

            if (!autoLoginAsGuest)
            {
                return authClient;
            }

            authClient.onWebSocketConnect = LoginAsGuestIfNotAlready;
            requestFactory.OnWebSocketConnect += authClient.onWebSocketConnect;

            if (requestFactory.IsReady)
            {
                LoginAsGuestIfNotAlready();
            }

            return authClient;

            void LoginAsGuestIfNotAlready()
            {
                if (authClient.LoggedIn)
                {
                    return;
                }

                var logger = authClient.logger;
                var legacyLoginData = LegacyLoginData.Get(projectId, authClient.uniqueId);

                logger.Debug($"Logging in as guest", ("username", legacyLoginData.Username),
                    ("projectId", projectId), ("uniqueId", authClient.uniqueId));

                var hasLegacyGuestLoginData = !string.IsNullOrEmpty(legacyLoginData.Username) && !string.IsNullOrEmpty(legacyLoginData.GuestPassword);
                if (hasLegacyGuestLoginData)
                {
                    #pragma warning disable CS0618
                    authClient.UserName = legacyLoginData.Username;
                    authClient.GuestPassword = legacyLoginData.GuestPassword;
                    #pragma warning restore CS0618

                    _ = authClient.Login(LoginType.Guest, () => (legacyLoginData.Username, legacyLoginData.GuestPassword), autoSignup: true);
                    return;
                }

                authClient.LoginAsGuest().ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully && t.Result.Type is Result.Success)
                    {
                        LegacyLoginData.SetCredentials(
                            projectId,
                            authClient.uniqueId,
                            t.Result.Username,
                            t.Result.GuestPassword);
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

#if UNITY
        /// <summary>
        /// Initializes a new instance of <see cref="AuthClient"/> for a simulator.
        /// </summary>
        internal static AuthClient ForSimulator(IRequestFactory requestFactory, string projectId)
        {
            var authClient = new AuthClient(requestFactory, projectId:projectId, uniqueId:SimulatorInCloudUniqueId, sessionToken:new(SimulatorUtility.AuthToken));

            if (string.IsNullOrEmpty(authClient.projectId))
            {
                return authClient;
            }

            if (string.IsNullOrEmpty(SimulatorUtility.AuthToken))
            {
                authClient.logger.Error(Error.RuntimeCloudSimulatorMissingToken,
                    $"{nameof(ForSimulator)} was used but {nameof(SimulatorUtility)}.{nameof(SimulatorUtility.AuthToken)} was null or empty.");
            }

            requestFactory.OnWebSocketConnect += authClient.InitializeSimulatorAuthentication;

            if (requestFactory.IsReady)
            {
                authClient.InitializeSimulatorAuthentication();
            }

            return authClient;
        }
#endif

        [Deprecated("09/2024", 1, 3, 0)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This constructor will be removed in a future version. Use " + nameof(AuthClient) + "." + nameof(ForPlayer) + " instead.")]
        public AuthClient(string uniqueId, bool autoLoginAsGuest, string projectId, IRequestFactory requestFactory) : this(requestFactory, projectId: projectId, uniqueId: uniqueId)
        {
            if (!autoLoginAsGuest)
            {
                return;
            }

            onWebSocketConnect = LoginAsGuestIfNotAlready;
            requestFactory.OnWebSocketConnect += onWebSocketConnect;

            if (requestFactory.IsReady)
            {
                LoginAsGuestIfNotAlready();
            }

            void LoginAsGuestIfNotAlready()
            {
                if (!LoggedIn)
                {
                    _ = LoginAsGuest();
                }
            }
        }

        private AuthClient(IRequestFactory requestFactory, string projectId = "", string uniqueId = "", SessionToken sessionToken = default)
        {
            this.OnInitialized();
            this.requestFactory = requestFactory;
            this.uniqueId = uniqueId;
            this.projectId = projectId;
            this.sessionToken = sessionToken;

            if (string.IsNullOrEmpty(this.projectId))
            {
                return;
            }

            if (string.IsNullOrEmpty(uniqueId))
            {
                this.uniqueId = UniqueIdPool.Get(this.projectId);
                hasPooledId = true;
            }
            else
            {
                this.uniqueId = uniqueId;
            }

            requestFactory.AddPushCallback(ConnectionClosedPath, OnConnectionClosedHandler);

            // Make sure that request factory starts connecting, if it hasn't already, so that:
            // 1. The OnWebSocketConnect event will get raised at some point, if the request factory isn't ready already.
            // 2. Any faulty code that waits for IRequestFactory.IsReady to become true without
            //    first calling IRequestFactory.ForceCreateWebSocket won't get stuck waiting forever.
            requestFactory.ForceCreateWebSocket();
        }

        /// <inheritdoc/>
        public async Task<LoginResult> LoginAsGuest()
        {
            var username = Guid.NewGuid().ToString("N");
            var password = Guid.NewGuid().ToString("N");

#pragma warning disable CS0618
            UserName = username;
            GuestPassword = password;
#pragma warning restore CS0618

            return await Login(LoginType.Guest, () => (username, password), autoSignup: true);
        }

        /// <summary>
        /// Login with a specific account to the coherence Cloud. This requires Persistent Accounts to be enabled in your coherence Dashboard.
        /// </summary>
        /// <param name="username">Username for the account.</param>
        /// <param name="password">Password for the account.</param>
        /// <param name="autoSignup">If the account doesn't exist, it will be created.</param>
        /// <returns>Result of the Login operation.</returns>
        public async Task<LoginResult> LoginWithPassword(string username, string password, bool autoSignup)
        {
#pragma warning disable CS0618
            UserName = username;
            GuestPassword = "";
#pragma warning restore CS0618

            return await Login(LoginType.Password, () => (username, password), autoSignup);
        }

        public async Task<LoginResult> LoginWithToken(SessionToken sessionToken)
        {
            this.sessionToken = sessionToken;
            return await Login(LoginType.SessionToken, null, autoSignup: true);
        }

        /// <summary>
        /// Clear the cached Login credentials and be considered as logged out from the coherence Cloud.
        /// </summary>
        public void Logout()
        {
            if (!LoggedIn)
            {
                return;
            }

            LoggedIn = false;
            sessionToken = Cloud.SessionToken.None;

#pragma warning disable CS0618
            UserName = "";
            GuestPassword = "";
#pragma warning restore CS0618

            OnLogout?.Invoke();
        }

        ~AuthClient()
        {
#if UNITY
            if (SimulatorUtility.UseSharedCloudCredentials)
            {
                logger.Info($"Won't call {nameof(Dispose)} even through finalizer was executed, because {nameof(SimulatorUtility)}.{nameof(SimulatorUtility.UseSharedCloudCredentials)} is True, and we want to preserve AuthClient instance's integrity for Simulators running in the Cloud.");
                return;
            }
#endif

            if (!this.OnFinalized())
            {
                logger.Warning(Warning.RuntimeCloudGameServicesResourceLeak,
                    this.GetResourceLeakWarningMessage() + $".\n{nameof(uniqueId)}:\"{uniqueId}\".");
            }
        }

        public void Dispose()
        {
            if (this.OnDisposed())
            {
                return;
            }

            GC.SuppressFinalize(this);
            Prefs.Save();

            if (hasPooledId)
            {
                UniqueIdPool.Release(projectId, uniqueId);
            }

            if (onWebSocketConnect is not null)
            {
                requestFactory.OnWebSocketConnect -= onWebSocketConnect;
            }

            initialAuthCancellationToken?.Cancel();
            initialAuthCancellationToken?.Dispose();

            logger?.Dispose();
        }

        private void OnConnectionClosedHandler(string responseBody)
        {
            try
            {
                var response = CoherenceJson.DeserializeObject<ConnectionClosedResponse>(responseBody);
                var error = CreateLoginError(ErrorType.ConcurrentConnection, responseBody: responseBody);
                Logout();
                OnConcurrentConnection?.Invoke(response);
                OnError?.Invoke(error);
                logger.Warning(Warning.RuntimeCloudGameServicesShutdownError, error.Message);
            }
            catch (Exception exception)
            {
                logger.Error(Error.RuntimeCloudDeserializationException,
                    ("Request", nameof(ConnectionClosedResponse)),
                    ("Response", responseBody),
                    ("exception", exception));
            }
        }

        private async Task<LoginResult> Login(LoginType loginType, Func<(string username, string password)> getUsernameAndPassword, bool autoSignup)
        {
            if (LoggedIn)
            {
                var message = $"You have to call {nameof(Logout)} before attempting to log in again.";
                var error = new LoginError(ErrorType.AlreadyLoggedIn, message);
                OnError?.Invoke(error);
                return new(Result.AlreadyLoggedIn, error);
            }

            var connectionError = await WaitUntilConnected();
            if (connectionError is { Type: ErrorType.ServerError })
            {
                return new(Result.ServerError, connectionError);
            }

            if (loginType is LoginType.SessionToken)
            {
                return await HandleLoginRequestAsync(username: "", guestPassword: "", basePath: "/session", method: "POST", body: "");
            }

            initialAuthCancellationToken?.Cancel();

            var (username, password) = getUsernameAndPassword();
            var request = new LoginRequest
            {
                Type = loginType,
                Username = username,
                Password = password,
                Autosignup = autoSignup,
            };

            var requestJson = CoherenceJson.SerializeObject(request);
            var guestPassword = loginType is LoginType.Guest ? password : "";
            return await HandleLoginRequestAsync(username: username, guestPassword: guestPassword, basePath: "/account", method: "POST", body: requestJson);

            async Task<LoginError> WaitUntilConnected()
            {
                if (requestFactory.IsReady)
                {
                    return null;
                }

                var webSocketConnectCompletionSource = new TaskCompletionSource<LoginError>();

                requestFactory.OnWebSocketConnect += OnWebSocketConnect;
                requestFactory.OnWebSocketDisconnect += OnWebSocketDisconnect;
                requestFactory.OnWebSocketConnectionError += OnWebSocketConnectionError;

                return await webSocketConnectCompletionSource.Task;

                void OnWebSocketConnect() => SetResult(null);
                void OnWebSocketDisconnect()
                {
                    const string message = "Logging in failed because connection was lost.";
                    logger.Warning(Warning.RuntimeCloudLoginFailedMsg, message);
                    SetResult(new(ErrorType.ServerError, message));
                }
                void OnWebSocketConnectionError()
                {
                    const string message = "Logging in failed because of server error.";
                    logger.Warning(Warning.RuntimeCloudLoginFailedMsg, message);
                    SetResult(new(ErrorType.ServerError, message));
                }

                void SetResult(LoginError error)
                {
                    requestFactory.OnWebSocketConnect -= OnWebSocketConnect;
                    requestFactory.OnWebSocketDisconnect -= OnWebSocketDisconnect;
                    requestFactory.OnWebSocketConnectionError -= OnWebSocketConnectionError;

                    if (error is not null)
                    {
                        OnError?.Invoke(error);
                    }

                    webSocketConnectCompletionSource.SetResult(error);
                }
            }

            async Task<LoginResult> HandleLoginRequestAsync(string username, string guestPassword, string basePath, string method, string body)
            {
                try
                {
                    var response = await requestFactory.SendRequestAsync(basePath, method, body, null, GetRequestName(), sessionToken);
                    return HandleLogin(username: username, guestPassword: guestPassword, response: response);
                }
                catch (RequestException ex)
                {
                    var invalidCredentials = ex.StatusCode is StatusCodes.Unauthorized;
                    var resultType = invalidCredentials ? Result.InvalidCredentials : Result.ServerError;
                    var errorType = invalidCredentials ? ErrorType.InvalidCredentials : ErrorType.ServerError;
                    var error = CreateLoginError(errorType, exception: ex);
                    OnError?.Invoke(error);
                    logger.Warning(Warning.RuntimeCloudLoginFailedMsg, error.Message);
                    return new(resultType, new LoginError(errorType, ex.UserMessage));
                }

                static string GetRequestName() => $"{nameof(AuthClient)}.Login";
            }
        }

        private LoginError CreateLoginError(ErrorType type, string responseBody = "", RequestException exception = null)
            => //type is ErrorType.None
               //? null /
            new(type,
#pragma warning disable CS8524
            type switch
            {
                ErrorType.ServerError => "Logging in failed because of server error.",
                ErrorType.InvalidCredentials => "Logging in failed because an invalid username, password of session token was provided.",
                ErrorType.FeatureDisabled => "Logging in failed because 'Persisted Player Accounts' is not enabled in the coherence Cloud > Dashboard > Project Settings.\n\nLink to Dashboard: https://coherence.io/dashboard/",
                ErrorType.InvalidResponse => "Logging in failed because was unable to deserialize the response from the server.",
                ErrorType.TooManyRequests => "Logging in failed because too many requests have been sent within a short amount of time.\n\nPlease slow down the rate of sending requests, and try again later.",
                ErrorType.AlreadyLoggedIn => $"Already logged in. You have to call {nameof(Logout)} before attempting to log in again.",
                ErrorType.ConcurrentConnection
                    => "We have received a concurrent connection for your User. Your current credentials will be invalidated.\n\n" +
                    "Usually this happens when a concurrent connection is detected, e.g. running multiple game clients for the same player.\n\n" +
                    "When this happens the game should present a prompt to the player to inform them that there is another instance of the game running. " +
                    "The game should wait for player input and never try to reconnect on its own or else the two game clients would disconnect each other indefinitely."
            }
#pragma warning restore CS8524
            + (exception?.UserMessage is { Length: > 0 } ? "\n\n" + exception.UserMessage : ""),

            responseBody);

        private LoginResult HandleLogin(string username, string guestPassword, string response)
        {
            if (string.IsNullOrEmpty(response))
            {
                var error = CreateLoginError(ErrorType.InvalidResponse);
                OnError?.Invoke(error);
                logger.Warning(Warning.RuntimeCloudLoginFailedMsg, error.Message);
                return new(Result.InvalidResponse, error);
            }

            LoginResponse loginResponse;
            try
            {
                loginResponse = CoherenceJson.DeserializeObject<LoginResponse>(response);
            }
            catch (Exception exception)
            {
                var message = $"{nameof(LoginResponse)} deserialization exception";
                var args = new (string key, object value)[] { ("response", response), ("exception", exception) };
                var error = new LoginError(ErrorType.InvalidResponse, message + "\n" + string.Join('\n', args.Select(x => x.value)));
                OnError?.Invoke(error);
                logger.Warning(Warning.RuntimeCloudLoginFailedMsg, message, args);
                return new(Result.InvalidResponse, error);
            }

            LoggedIn = true;
            userId = loginResponse.UserId;
            sessionToken = new(loginResponse.SessionToken);

            var success = new LoginResult(username, guestPassword, Result.Success, loginResponse);

#pragma warning disable CS0618
            SessionTokenRefreshResult = Result.Success;
            OnSessionRefreshed?.Invoke(Result.Success);
#pragma warning restore CS0618

            OnLogin?.Invoke(loginResponse);

            return success;
        }

#if UNITY
        private async void InitializeSimulatorAuthentication()
        {
            initialAuthCancellationToken = new CancellationTokenSource();

            if (string.IsNullOrEmpty(sessionToken))
            {
                logger.Error(Error.RuntimeCloudSimulatorAuthToken,
                    $"{nameof(InitializeSimulatorAuthentication)} called but {nameof(sessionToken)} was null or empty.");
                return;
            }

            refreshTokenTask ??= RefreshSessionTokenPeriodically();
            await refreshTokenTask;

            async Task RefreshSessionTokenPeriodically()
            {
                while (!initialAuthCancellationToken.IsCancellationRequested)
                {
                    await RefreshSessionToken();

                    await Task.Delay(simulatorTokenRefreshPeriodInDays);
                }

                initialAuthCancellationToken = null;
            }

            async Task RefreshSessionToken()
            {
                if (string.IsNullOrEmpty(sessionToken))
                {
                    OnError?.Invoke(new(ErrorType.InvalidCredentials));
                    OnSessionRefreshed?.Invoke(Result.InvalidCredentials);
                    return;
                }

                try
                {
                    var response = await requestFactory.SendRequestAsync("/session", "POST", "", null,
                        $"{nameof(AuthClient)}.{nameof(RefreshSessionToken)}", sessionToken);
                    _ = HandleLogin(username:"", guestPassword:"", response:response);
                }
                catch (RequestException ex)
                {
                    logger.Error(Error.RuntimeCloudSimulatorAuthToken,
                        ex.ToString());
                }
            }
        }
#endif
    }
}
