// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Cloud;
    using Coherence.Tests;
    using Coherence.Utils;
    using Log;
    using NUnit.Framework;
    using NUnit.Framework.Internal;
    using UnityEngine.Scripting;
    using Logger = Log.Logger;

    public class AuthClientTests : CoherenceTest
    {
        private const string TestProjectId = "testProjectId";
        private const string TestUniqueId = "testUniqueId";

        private MockRequestFactoryBuilder requestFactory;
        private SessionToken ExpectedSessionToken => new(MockRequestFactoryBuilder.DefaultSessionToken);
        private bool SendRequestAsyncWasCalled => requestFactory.SendRequestAsyncWasCalled;
        private (string basePath, string method, string body, Dictionary<string, string> headers, string requestName, string sessionToken) SendRequestAsyncWasCalledWith => requestFactory.SendRequestAsyncWasCalledWith;

        [Test]
        public void LoginWithPassword_Completes_Successfully()
        {
            using var authClient = CreateAuthClient(isReady:true);

            var loginTask = authClient.LoginWithPassword(default, default, default);

            Assert.That(SendRequestAsyncWasCalled, Is.True);
            Assert.That(loginTask.IsCompleted, Is.True);
            Assert.That(loginTask.Result.LoggedIn, Is.True);
        }

        [Test]
        public void LoginWithPassword_Result_Contains_Provided_Username()
        {
            using var authClient = CreateAuthClient(isReady:true);

            const string username = "username";
            var loginTask = authClient.LoginWithPassword(username, default, default);

            Assert.That(loginTask.Result.Username, Is.EqualTo(username));
        }

        [Test]
        public void LoginWithPassword_Result_Does_Not_Contains_Provided_Password()
        {
            using var authClient = CreateAuthClient(isReady:true);

            const string password = "password";
            var loginTask = authClient.LoginWithPassword(default, password, default);

            Assert.That(loginTask.Result.GuestPassword, Is.Empty);
        }

        [Test]
        public void LoginAsGuest_Result_Contains_Generated_Username()
        {
            using var authClient = CreateAuthClient(isReady:true);

            var loginTask = authClient.LoginAsGuest();

            Assert.That(loginTask.Result.Username, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void LoginAsGuest_Result_Contains_Generated_Password()
        {
            using var authClient = CreateAuthClient(isReady:true);

            var loginTask = authClient.LoginAsGuest();

            Assert.That(loginTask.Result.GuestPassword, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void LoginAsGuest_Completes_Successfully()
        {
            using var authClient = CreateAuthClient(isReady:true);

            var loginTask = authClient.LoginAsGuest();

            Assert.That(SendRequestAsyncWasCalled, Is.True);
            Assert.That(loginTask.IsCompleted, Is.True);
            Assert.That(loginTask.Result.LoggedIn, Is.True);
        }

        [Test]
        public void LogsOut_After_Websocket_Disconnected() => Assert.Ignore("Not implemented yet.");

        [Test]
        public void UsesCachedGuestAccount_When_LoggingInAsGuest()
        {
            LegacyLoginData.SetCredentials(TestProjectId, TestUniqueId, "testUser", "testPassword");

            using var authClient = CreateAuthClient(autoLoginAsGuest:true);
            Connect();

            var request = CoherenceJson.DeserializeObject<LoginRequest>(requestFactory.SendRequestAsyncWasCalledWith.body);

            Assert.That(authClient.LoggedIn, Is.True);
            Assert.That(request.Username, Is.EqualTo("testUser"));
            Assert.That(request.Password, Is.EqualTo("testPassword"));
            Assert.That(request.Type, Is.EqualTo(LoginType.Guest));
        }

        [Test]
        public void DoesNotUseCachedCredentials_When_LoggingInAsGuest_If_Password_Is_Missing()
        {
            LegacyLoginData.SetCredentials(TestProjectId, TestUniqueId, "testUser", "");

            using var authClient = CreateAuthClient(autoLoginAsGuest:true);
            Connect();

            var request = CoherenceJson.DeserializeObject<LoginRequest>(requestFactory.SendRequestAsyncWasCalledWith.body);

            Assert.That(authClient.LoggedIn, Is.True);
            Assert.That(request.Username, Is.Not.EqualTo("testUser"));
            Assert.That(request.Password, Has.Length.GreaterThan(0));
            Assert.That(request.Type, Is.EqualTo(LoginType.Guest));
        }

        [Test]
        public void GeneratesNewGuestAccount_When_Fresh_LoggingInAsGuest()
        {
            LegacyLoginData.SetCredentials(TestProjectId, TestUniqueId, "testUser", "testPassword");
            LegacyLoginData.Clear(TestProjectId, TestUniqueId);

            using var authClient = CreateAuthClient(autoLoginAsGuest:true);
            Connect();

            var request = CoherenceJson.DeserializeObject<LoginRequest>(requestFactory.SendRequestAsyncWasCalledWith.body);

            Assert.That(authClient.LoggedIn, Is.True);
            Assert.That(request.Username, Is.Not.EqualTo("testUser"));
            Assert.That(request.Password, Is.Not.EqualTo("testPassword"));
        }

        [Test]
        public void Should_AutoLoginAsGuest_When_Instantiating()
        {
            using var authClient = CreateAuthClient(autoLoginAsGuest:true);

            Connect();

            Assert.That(GetSessionToken(authClient), Is.EqualTo(ExpectedSessionToken));
            Assert.That(authClient.LoggedIn, Is.True);
        }

        [Test]
        public void Should_WaitForRequestFactoryReadiness_When_InstantiatingWithAutoLoginAsGuest()
        {
            using var authClient = CreateAuthClient(autoLoginAsGuest:true);

            Assert.That(GetSessionToken(authClient), Is.EqualTo(SessionToken.None));
            Assert.That(authClient.LoggedIn, Is.False);

            Connect();

            Assert.That(GetSessionToken(authClient), Is.EqualTo(ExpectedSessionToken));
            Assert.That(authClient.LoggedIn, Is.True);
        }

        [Test]
        public void Should_CallAccountEndpoint_When_InstantiatingWithAutoLoginAsGuestAndRequestBuilderIsReady()
        {
            CreateAuthClient(autoLoginAsGuest:true, isReady:true);

            Assert.That(SendRequestAsyncWasCalled, Is.True);
            Assert.That(SendRequestAsyncWasCalledWith.basePath, Is.EqualTo("/account"));
            Assert.That(SendRequestAsyncWasCalledWith.method, Is.EqualTo("POST"));
            Assert.That(SendRequestAsyncWasCalledWith.requestName, Is.EqualTo($"{nameof(AuthClient)}.Login"));
            Assert.That(SendRequestAsyncWasCalledWith.sessionToken, Is.Null.Or.Empty);
        }

        [Test]
        public void Should_RefreshLogin_When_InstantiatingWithCachedToken()
        {
            using var authClient = CreateAuthClient(autoLoginAsGuest:false, isReady:true);
            var expectedSessionToken = new SessionToken("ExpectedSessionToken");

            var result = authClient.LoginWithToken(expectedSessionToken);

            Assert.That(result.IsCompleted, Is.True);
            Assert.That(result.Result.LoggedIn, Is.True);
            Assert.That(result.Result.SessionToken, Is.EqualTo(ExpectedSessionToken));
            Assert.That(GetSessionToken(authClient), Is.EqualTo(ExpectedSessionToken));
            Assert.That(authClient.LoggedIn, Is.True);
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            requestFactory = new MockRequestFactoryBuilder()
                .SetSessionToken(ExpectedSessionToken)
                .SetIsReady();
        }

        [TearDown]
        public override void TearDown()
        {
            requestFactory = null;
            base.TearDown();
        }

        private AuthClient CreateAuthClient(bool autoLoginAsGuest = false, bool isReady = false)
        {
            var factory = requestFactory.SetIsReady(isReady).Build().Object;
            return AuthClient.ForPlayer(factory, projectId:TestProjectId, uniqueId:TestUniqueId, autoLoginAsGuest:autoLoginAsGuest);
        }

        private void Connect() => requestFactory.Connect();
        private static SessionToken GetSessionToken(AuthClient authClient) => ((IAuthClientInternal)authClient).SessionToken;
    }
}
