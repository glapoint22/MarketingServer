using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OAuth;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace MarketingServer
{
    public class ApplicationOAuthProvider : OAuthAuthorizationServerProvider
    {
        private readonly string _publicClientId;
        private MarketingEntities db = new MarketingEntities();

        public ApplicationOAuthProvider(string publicClientId)
        {
            if (publicClientId == null)
            {
                throw new ArgumentNullException("publicClientId");
            }

            _publicClientId = publicClientId;
        }

        public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {
            var userManager = context.OwinContext.GetUserManager<ApplicationUserManager>();



            ApplicationUser user = await userManager.FindAsync(context.UserName, context.Password);

            if (user == null)
            {
                context.SetError("invalid_grant", "The user name or password is incorrect.");
                return;
            }

            ClaimsIdentity oAuthIdentity = await user.GenerateUserIdentityAsync(userManager,
               OAuthDefaults.AuthenticationType);
            ClaimsIdentity cookiesIdentity = await user.GenerateUserIdentityAsync(userManager,
                CookieAuthenticationDefaults.AuthenticationType);


            AuthenticationProperties properties = CreateProperties(user.UserName);
            properties.Dictionary.Add("ClientID", context.ClientId);
            AuthenticationTicket ticket = new AuthenticationTicket(oAuthIdentity, properties);
            context.Validated(ticket);
            context.Request.Context.Authentication.SignIn(cookiesIdentity);
        }



        public override Task TokenEndpointResponse(OAuthTokenEndpointResponseContext context)
        {
            //MarketingEntities db = new MarketingEntities();
            //Token token = db.Tokens.SingleOrDefault();
            //bool tokenModified = true;

            //if (token == null)
            //{
            //    token = new Token();
            //    tokenModified = false;
            //}

            //token.AccessToken = context.AccessToken;
            //token.AccessTokenExpires = DateTime.Parse((string)context.AdditionalResponseParameters[".expires"]);
            //token.RefreshToken = context.TokenEndpointRequest.RefreshTokenGrant.RefreshToken;
            //token.RefreshTokenExpires = context.Properties.ExpiresUtc.Value.DateTime;

            //if (tokenModified)
            //{
            //    db.Entry(token).State = EntityState.Modified;
            //}
            //else
            //{
            //    db.Tokens.Add(token);
            //}


            //db.SaveChanges();

            context.AdditionalResponseParameters.Add("refreshTokenExpires", context.Properties.ExpiresUtc.Value.DateTime);

            return Task.FromResult<object>(null);
        }

        public override Task TokenEndpoint(OAuthTokenEndpointContext context)
        {
            foreach (KeyValuePair<string, string> property in context.Properties.Dictionary)
            {
                context.AdditionalResponseParameters.Add(property.Key, property.Value);
            }

            return Task.FromResult<object>(null);
        }

        public override Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        {
            string clientId;
            string clientSecret;
            Client client;


            context.TryGetFormCredentials(out clientId, out clientSecret);

            if (context.ClientId == null)
            {
                context.SetError("Invalid Request");
                return Task.FromResult<object>(null);
            }

            client = db.Clients.Find(clientId);

            if (client == null || client.Secret != Hashing.GetHash(client.SecurityStamp + clientSecret))
            {
                context.SetError("Invalid Request");
                return Task.FromResult<object>(null);
            }

            context.Validated();

            return Task.FromResult<object>(null);
        }

        public override Task ValidateClientRedirectUri(OAuthValidateClientRedirectUriContext context)
        {
            if (context.ClientId == _publicClientId)
            {
                Uri expectedRootUri = new Uri(context.Request.Uri, "/");

                if (expectedRootUri.AbsoluteUri == context.RedirectUri)
                {
                    context.Validated();
                }
            }

            return Task.FromResult<object>(null);
        }

        public static AuthenticationProperties CreateProperties(string userName)
        {
            IDictionary<string, string> data = new Dictionary<string, string>
            {
                { "userName", userName }
            };
            return new AuthenticationProperties(data);
        }
    }
}