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

namespace Framework.Caspar.Google
{
    public static class Api
    {
        public static string ClientId { get; set; } = "605618539331-533mbccnrrdpcuqbatpjlut9djr9866q.apps.googleusercontent.com";
        public static string ClientSecret { get; set; } = "Qx4XjXVCicJTLxVhrOJkDs76";

        public static ThreadLocal<JsonWebKeySet> JWK { get; set; } = new ThreadLocal<JsonWebKeySet>();

        public static string Issuer { get; set; }
        public static List<string> ValidAudiences { get; set; } = new();

        public static ThreadLocal<TokenValidationParameters> TVP { get; set; } = new();
        public static class IdToken
        {

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
                    catch
                    {
                        JWK.Value = null;
                        continue;
                    }
                }

                return null;
            }


            //public static bool GetUserIDFromIDToken(string idToken, out string user_id)
            //{
            //    string CLIENT_ID = "30437480948-fov0lmjusmr4lm45vtaska6a97ch9fcl.apps.googleusercontent.com";// ConfigurationManager.AppSettings["GoogleClientID"];

            //    JwtSecurityToken token = new JwtSecurityToken(idToken);
            //    JwtSecurityTokenHandler jsth = new JwtSecurityTokenHandler();

            //    user_id = token.Subject;

            //    if (string.IsNullOrEmpty(user_id))
            //    {
            //        return false;
            //    }

            //    if (!token.Issuer.Equals("accounts.google.com") && !token.Issuer.Equals("https://accounts.google.com"))
            //    {
            //        return false;
            //    }

            //    bool foundaudience = false;
            //    foreach (var auditem in token.Audiences)
            //    {
            //        if (auditem.Equals(CLIENT_ID))
            //        {
            //            foundaudience = true;
            //        }
            //    }

            //    if (foundaudience == false)
            //    {
            //        return false;
            //    }

            //    if (token.ValidTo < DateTime.UtcNow)
            //    {
            //        return false;
            //    }

            //    string kid = "";
            //    foreach (var headerItem in token.Header)
            //    {
            //        if (headerItem.Key.Equals("kid"))
            //        {
            //            kid = (string)headerItem.Value;
            //        }
            //    }

            //    WebRequest request = WebRequest.Create("https://www.googleapis.com/oauth2/v1/certs");

            //    StreamReader reader = new StreamReader(request.GetResponse().GetResponseStream());

            //    string responseFromServer = reader.ReadToEnd();

            //    String[] split = responseFromServer.Split(':');

            //    // There are n number of certificates returned from Google
            //    int numberOfCerts = (split.Length - 1) <= 1 ? 1 : split.Length - 1;
            //    byte[][] certBytes = new byte[numberOfCerts][];
            //    int index = 0;
            //    UTF8Encoding utf8 = new UTF8Encoding();
            //    for (int i = 0; i < split.Length; i++)
            //    {
            //        if (split[i].IndexOf(beginCert) > 0)
            //        {
            //            int startSub = split[i].IndexOf(beginCert);
            //            int endSub = split[i].IndexOf(endCert) + endCert.Length;
            //            certBytes[index] = utf8.GetBytes(split[i].Substring(startSub, endSub).Replace("\\n", "\n"));
            //            index++;
            //        }
            //    }

            //    Dictionary<String, X509Certificate2> certificates = new Dictionary<string, X509Certificate2>();
            //    for (int i = 0; i < certBytes.Length; i++)
            //    {
            //        X509Certificate2 certificate = new X509Certificate2(certBytes[i]);
            //        certificates.Add(certificate.Thumbprint, certificate);
            //    }

            //    {
            //        // Set up token validation
            //        TokenValidationParameters tvp = new TokenValidationParameters()
            //        {
            //            ValidateAudience = false,        // check the client ID
            //            ValidateIssuer = false,          // check token came from Google
            //            ValidateLifetime = false,

            //            ValidateIssuerSigningKey = true,
            //            RequireSignedTokens = true,
            //            CertificateValidator = X509CertificateValidator.None,
            //            IssuerSigningKeyResolver = (s, securityToken, identifier, parameters) =>
            //            {
            //                return identifier.Select(x =>
            //                {
            //                    // TODO: Consider returning null here if you have case sensitive JWTs.
            //                    if (certificates.ContainsKey(x.Id.ToUpper()))
            //                    {
            //                        return new X509SecurityKey(certificates[x.Id.ToUpper()]);
            //                    }
            //                    //if (googlecert.certificates.ContainsKey(x.Id.ToUpper()))
            //                    //{
            //                    //    return new X509SecurityKey(googlecert.certificates[x.Id.ToUpper()]);
            //                    //}
            //                    return null;
            //                }).First(x => x != null);
            //            }
            //        };

            //        try
            //        {
            //            // Validate using the provider
            //            SecurityToken validatedToken;
            //            ClaimsPrincipal cp = jsth.ValidateToken(idToken, tvp, out validatedToken);
            //            if (cp != null)
            //            {
            //                return true;
            //            }

            //            return false;
            //        }
            //        catch (Exception e)
            //        {
            //            return false;
            //        }
            //    }
            //}
        }
    }

}
