using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;

namespace MarketingServer
{
    public class Session
    {
        public static void SetSessionID(string sessionId, HttpRequestMessage request, ref HttpResponseMessage response)
        {
            CookieHeaderValue cookie = new CookieHeaderValue("session", sessionId);
            cookie.Expires = DateTimeOffset.Now.AddYears(1);
            cookie.Domain = request.RequestUri.Host;
            cookie.Path = "/";
            response.Headers.AddCookies(new CookieHeaderValue[] { cookie });
        }

        public static string GetSessionID(HttpRequestHeaders headers)
        {
            CookieHeaderValue cookie = headers.GetCookies("session").FirstOrDefault();
            if (cookie != null)
            {
                return Hashing.GetHash(cookie["session"].Value);
            }
            return null;
        }
    }
}