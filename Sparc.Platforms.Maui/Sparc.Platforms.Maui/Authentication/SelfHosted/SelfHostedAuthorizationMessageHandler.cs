﻿using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Sparc.Platforms.Maui
{
    public class SelfHostedAuthorizationMessageHandler : DelegatingHandler
    {
        public SelfHostedAuthorizationMessageHandler(AuthenticationStateProvider provider)
        {
            Provider = provider;
        }

        public AuthenticationStateProvider Provider { get; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            AuthenticationState result;
            try
            {
                result = await Provider.GetAuthenticationStateAsync();
            }
            catch (Exception ex)
            {
                var response = new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent(ex.Message)
                };
                return response;
            }

            var accessToken = result.User?.FindFirst("access_token");
            if (accessToken != null)
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken.Value);

            return await base.SendAsync(request, cancellationToken);
        }
    }

    public class InsecureSelfHostedAuthorizationMessageHandler : SelfHostedAuthorizationMessageHandler
    {
        public InsecureSelfHostedAuthorizationMessageHandler(AuthenticationStateProvider provider) : base(provider)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (InnerHandler is HttpClientHandler httpclienthandler)
                httpclienthandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    if (cert.Issuer.Equals("CN=localhost"))
                        return true;
                    return errors == System.Net.Security.SslPolicyErrors.None;
                };

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
