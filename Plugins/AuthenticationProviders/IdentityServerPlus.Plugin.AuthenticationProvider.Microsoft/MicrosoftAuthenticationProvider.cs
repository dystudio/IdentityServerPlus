﻿using IdentityModel;
using IdentityServer.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServerPlus.Plugin.AuthenticationProvider.Microsoft
{
    internal class MicrosoftAuthenticationProvider : Base.Structures.AuthenticationProvider
    {
        private const string ObjectIdentfier = "http://schemas.microsoft.com/identity/claims/objectidentifier";
        public MicrosoftAuthenticationProvider() : base("Microsoft", "microsoft")
        {
            //_options = options.Value;
        }

        public override string Description => "A login provider for Microft Social and Enterprise (Office 365) Connections";

        public override AuthenticationBuilder Build(AuthenticationBuilder builder)
        {
            return builder.AddOpenIdConnect("microsoft", options =>
             {
                 options.SaveTokens = true;
                 options.Scope.Add("profile");
                 options.Scope.Add("User.Read");
                 options.Scope.Add("offline_access");
                 options.ResponseType = "id_token token";
                 options.ClientId = "7d6d3978-f958-4759-887d-498b52ede1df";
                 options.ClientSecret = "DSBjc:/?V-FBGAfY64t96gm@GG=qfcct";
                 options.MetadataAddress = "https://login.microsoftonline.com/15864aef-67b6-4b0e-8bfe-cd61201a6837/v2.0/.well-known/openid-configuration";
                 options.Authority = "https://login.microsoftonline.com/15864aef-67b6-4b0e-8bfe-cd61201a6837";
             });
        }

        public override string GetProviderId(AuthenticateResult authResult)
        {
            var claims = authResult.Principal.Claims;
            var id = claims.SingleOrDefault(x => x.Type == ObjectIdentfier);
            return id.Value;
        }



        public override ApplicationUser ProvisionUser(AuthenticateResult authResult)
        {
            var claims = authResult.Principal.Claims;
            var email = claims.SingleOrDefault(x => x.Type == JwtClaimTypes.PreferredUserName);
            var fullName = claims.SingleOrDefault(x => x.Type == JwtClaimTypes.Name);
            return new ApplicationUser
            {
                Email = email.Value,
                Username = Guid.NewGuid().ToString(),
                Claims = new List<ApplicationUserClaim>()
                {
                    new ApplicationUserClaim(new System.Security.Claims.Claim(JwtClaimTypes.Name, fullName.Value, null, Scheme, Scheme))
                }
            };
        }

        public override async Task UpdateUserAsync(ApplicationUser user, AuthenticateResult result)
        {


            //TODO: Add options for this to happen, and when
            try
            {
                var graphClient = new GraphServiceClient(new GraphAuthenticationProvider(user.Providers.SingleOrDefault(x => x.LoginProvider == Scheme).AccessToken));
                var me = await graphClient.Me.Request().GetAsync();
                user.PhoneNumber = me.MobilePhone;
                user.PhoneNumberConfirmed = true;
                
                user.Email = me.Mail;
                user.EmailConfirmed = true; // we know its confirmed if we got this far from the microsoft account.
            }
            catch (Exception) { }
            

            var provider = user.Providers.SingleOrDefault(x => x.LoginProvider == Scheme);
            if (provider != null)
            {

                
                if (!string.IsNullOrWhiteSpace(result.Properties.GetTokenValue("access_token")))
                {
                    provider.AccessToken = result.Properties.GetTokenValue("access_token");

                    provider.AccessTokenExpiry = DateTime.Parse(result.Properties.GetTokenValue("expires_at"));
                }
                if (!string.IsNullOrWhiteSpace(result.Properties.GetTokenValue("id_token")))
                {
                    provider.IdToken = result.Properties.GetTokenValue("id_token");
                    provider.IdTokenExpiry = DateTime.Parse(result.Properties.GetTokenValue("expires_at"));
                }
            }
            await base.UpdateUserAsync(user, result);
        }
    }
}