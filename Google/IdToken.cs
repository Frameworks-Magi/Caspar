using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using static Caspar.Api;


namespace Caspar.Google
{
    public static class Api
    {
        public static string ClientId { get; set; } = "";
        public static string ClientSecret { get; set; } = "";

        public static ThreadLocal<JsonWebKeySet> JWK { get; set; } = new ThreadLocal<JsonWebKeySet>();

        public static string Issuer { get; set; } = "https://accounts.google.com";
        public static List<string> ValidAudiences { get; set; } = new();

        public static ThreadLocal<TokenValidationParameters> TVP { get; set; } = new();
        public static class IdToken
        {
            public async static Task<bool> VerifyGoogleIdToken(string idToken, string clientId)
            {
                try
                {
                    HttpClient httpClient = new HttpClient();
                    HttpResponseMessage response = await httpClient.GetAsync($"https://www.googleapis.com/oauth2/v3/tokeninfo?id_token={idToken}");

                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        // Parse the response to get the required information
                        // For example, you can check the audience (aud) and client ID (azp) in the response
                        // to verify if the ID token is valid for your client application

                        // Example check: verify that the client ID matches the expected value
                        if (responseBody.Contains($"\"azp\": \"{clientId}\""))
                        {
                            // Verification succeeded
                            return true;
                        }
                    }

                    // Verification failed
                    return false;
                }
                catch (Exception ex)
                {
                    // Handle the exception or log the error
                    return false;
                }
            }

            public async static Task<System.IdentityModel.Tokens.Jwt.JwtSecurityToken> Verify(string token)
            {
                int max = 2;
                while (max > 0)
                {
                    max -= 1;
                    try
                    {
                        //var max = res.Headers.CacheControl.MaxAge;
                        if (JWK.Value == null)
                        {
                            using (var httpClient = new HttpClient())
                            {
                                var res = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v3/certs");
                                var age = res.Headers.CacheControl.MaxAge;
                                var keys = await res.Content.ReadAsStringAsync();
                                JWK.Value = new JsonWebKeySet(keys);
                            }
                            TVP.Value = new TokenValidationParameters
                            {
                                ValidateIssuerSigningKey = true,
                                IssuerSigningKeys = JWK.Value.Keys,
                                ValidateLifetime = false,
                                ValidateAudience = true,
                                ValidAudiences = ValidAudiences,
                                ValidIssuer = Issuer,
                            };
                        }

                        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                        SecurityToken validatedToken;
                        var user = handler.ValidateToken(token, TVP.Value, out validatedToken);
                        return validatedToken as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;

                    }
                    catch (Exception e)
                    {
                        //Logger.Info(e);
                        Console.WriteLine(e);
                        JWK.Value = null;
                        continue;
                    }
                }

                return null;
            }


        }
    }
}