// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Runtime.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using Cloud;
    using Coherence.Utils;
    using Moq;

    /// <summary>
    /// Can be used to <see cref="Build"/> a mock <see cref="IRequestFactory"/>
    /// object for use in a test.
    /// </summary>
    public sealed class MockRequestFactoryBuilder
    {
        /// <summary>
        /// <see cref="LoginResponse.SessionToken"/> value that the mock returns be default when
        /// <see cref="IRequestFactory.SendRequestAsync"/> is called, if <see cref="SetSessionToken"/>
        /// is not used to override it.
        /// </summary>
        public const string DefaultSessionToken = "DefaultSessionToken";

        private Mock<IRequestFactory> mock;

        private bool isReady = true;
        private Func<bool> isReadyGetter;
        private RequestException sendRequestAsyncThrows;
        private bool isSequence;
        private string sessionToken = DefaultSessionToken;
        private (string basePath, string method, string body, Dictionary<string, string> headers, string requestName, string sessionToken)? sendRequestAsyncWasCalledWith;

        public bool SendRequestAsyncWasCalled => sendRequestAsyncWasCalledWith.HasValue;
        public (string basePath, string method, string body, Dictionary<string, string> headers, string requestName, string sessionToken) SendRequestAsyncWasCalledWith => sendRequestAsyncWasCalledWith.Value;

        public MockRequestFactoryBuilder() => isReadyGetter = ()=> isReady;

        public MockRequestFactoryBuilder SetSessionToken(string sessionToken)
        {
            this.sessionToken = sessionToken;
            return this;
        }

        public MockRequestFactoryBuilder SetIsReady(bool isReady = true)
        {
            this.isReady = isReady;
            return this;
        }

        public MockRequestFactoryBuilder OnSendRequestAsyncCalled(RequestException throws)
        {
            sendRequestAsyncThrows = throws;
            return this;
        }

        public Mock<IRequestFactory> Build()
        {
            if(mock is not null)
            {
                return mock;
            }

            mock = new Mock<IRequestFactory>();
            SetupSendRequestAsync(mock);
            SetupIsReady(mock);
            return mock;
        }

        public void Connect()
        {
            SetIsReady();
            RaiseOnWebSocketConnect();
        }

        public void Disconnect()
        {
            SetIsReady(false);
            RaiseOnWebSocketDisconnect();
        }

        public void RaiseOnWebSocketConnect() => Build().Raise(mock => mock.OnWebSocketConnect += null);
        public void RaiseOnWebSocketDisconnect() => Build().Raise(mock => mock.OnWebSocketDisconnect += null);

        public static implicit operator Mock<IRequestFactory>(MockRequestFactoryBuilder builder) => builder.Build();

        private void SetupSendRequestAsync(Mock<IRequestFactory> mock)
        {
            var sendRequestAsync = mock.Setup(GetSetupExpression());
            sendRequestAsync.Callback((string basePath, string method, string body, Dictionary<string, string> headers, string requestName, string sessionToken) => sendRequestAsyncWasCalledWith = (basePath, method, body, headers, requestName, sessionToken));

            if(sendRequestAsyncThrows is not null)
            {
                sendRequestAsync.Throws(sendRequestAsyncThrows);
            }
            else
            {
                var loginResponse = new LoginResponse { SessionToken = sessionToken };
                sendRequestAsync.Returns(Task.FromResult(CoherenceJson.SerializeObject(loginResponse)));
            }

            Expression<Func<IRequestFactory, Task<string>>> GetSetupExpression() => requestFactory => requestFactory.SendRequestAsync
            (
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            );
        }

        private void SetupIsReady(Mock<IRequestFactory> requestFactoryMock) => requestFactoryMock.SetupGet(factory => factory.IsReady).Returns(isReadyGetter);
    }
}
