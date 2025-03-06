using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Net;
using System.Xml;

namespace Caspar
{
    static public partial class Api
    {
        public static class Vivox
        {

            private static string Base64URLEncode(string plainText)
            {
                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
                // Remove padding at the end
                var encodedString = System.Convert.ToBase64String(plainTextBytes).TrimEnd('=');
                // Substitute URL-safe characters
                string urlEncoded = encodedString.Replace("+", "-").Replace("/", "_");

                return urlEncoded;
            }

            private static string SHA256Hash(string secret, string message)
            {
                var encoding = new System.Text.ASCIIEncoding();
                byte[] keyByte = encoding.GetBytes(secret);
                byte[] messageBytes = encoding.GetBytes(message);
                using (var hmacsha256 = new HMACSHA256(keyByte))
                {
                    byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                    var hashString = Convert.ToBase64String(hashmessage).TrimEnd('=');
                    string urlEncoded = hashString.Replace("+", "-").Replace("/", "_");

                    return urlEncoded;
                }
            }

            public static byte[] Key { get; set; } = Encoding.UTF8.GetBytes("secret!");
            public static string Secret { get; set; } = "TNBpej8BdxK6hmEuZdW2wkJ4uVRE1GFa";
            public static string Issuer { get; set; } = "retiad-dev";
            public static string Domain { get; set; } = "vdx5.vivox.com";
            public static string Admin { get; set; } = "retiad-Dev-Admin";
            public static string Password { get; set; } = "uCOOoYNo8Ev2xe2F";
            public static int Vxi
            {
                get
                {
                    return System.Threading.Interlocked.Increment(ref Sequence);
                }
            }
            private static int Sequence = 0;
            private static string AuthToken { get; set; } = "";
            public static dynamic API { get; internal set; }

            public static void Login()
            {
                while (true)
                {

                    try
                    {
                        XmlDocument doc = new XmlDocument();

                        var uri = $"{API}/viv_signin.php?userid={Admin}&pwd={Password}";
                        var request = WebRequest.Create(uri) as HttpWebRequest;

                        request.Method = "GET";

                        using (WebResponse r = request.GetResponse())
                        {
                            using (System.IO.StreamReader sr = new System.IO.StreamReader(r.GetResponseStream()))
                            {
                                AuthToken = sr.ReadToEnd();
                                doc.LoadXml(AuthToken);
                                AuthToken = doc["response"]["level0"]["body"]["auth_token"].InnerText;
                            }
                        }

                        break;
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                    }
                }
            }



            public async static Task Kick(long account)
            {
                string access_token = string.Empty;

                var payload = new
                {
                    vxa = "kick",
                    iss = Issuer,
                    exp = DateTime.UtcNow.AddSeconds(60).ToUnixTime(),
                    vxi = Vxi,
                    //sub = "sip:.blindmelon-AppName-dev.jerky.@tla.vivox.com",
                    sub = $"sip:.{Issuer}.{account}.@{Domain}",
                    //f = $"sip:blindmelon-AppName-dev-Admin@{Domain}",
                    f = $"sip:{Issuer}-Admin@{Domain}",

                    //t = $"sip:blindmelon-AppName-dev-service@{Domain}"
                    t = $"sip:{Issuer}-service@{Domain}"
                };

                using (HMACSHA256 sha = new HMACSHA256(Key))
                {

                    var header = new { };



                    var json1 = JsonConvert.SerializeObject(header).ToBase64UrlEncode();
                    var json2 = JsonConvert.SerializeObject(payload).ToBase64UrlEncode();



                    var message = $"{json1}.{json2}".ToBytes();

                    // 암호화
                    var hash = sha.ComputeHash(message);

                    // base64 컨버팅
                    var encoded = hash.ToBase64UrlEncode();
                    access_token = $"{json1}.{json2}.{encoded}";


                }

                //chan_uri = sip:blindmelon-AppName-dev-service@vdx5.vivox.com
                string chan_uri = payload.t;

                //user_uri = sip:.blindmelon.jerky.@vdx5.vivox.com
                string user_uri = payload.sub;

                var uri = $"{API}/viv_adm_user_kick.php?access_token={access_token}&auth_token={AuthToken}&chan_uri={chan_uri}&user_uri={user_uri}";
                var request = WebRequest.Create(uri) as HttpWebRequest;

                request.Method = "GET";

                using (WebResponse r = await request.GetResponseAsync())
                {
                    using (System.IO.StreamReader sr = new System.IO.StreamReader(r.GetResponseStream()))
                    {
                        var ret = sr.ReadToEnd();
                    }
                }
            }

            public static string GenerateLoginAccessToken(long account, int expire)
            {
                using (HMACSHA256 sha = new HMACSHA256(Key))
                {

                    var header = new { };
                    var payload = new
                    {
                        iss = Issuer,
                        exp = DateTime.UtcNow.AddSeconds(expire).ToUnixTime(),
                        vxa = "login",
                        vxi = Vxi,
                        f = $"sip:.{Issuer}.{account}.@{Domain}"
                    };

                    var json1 = JsonConvert.SerializeObject(header).ToBase64UrlEncode();
                    var json2 = JsonConvert.SerializeObject(payload).ToBase64UrlEncode();

                    var message = $"{json1}.{json2}".ToBytes();

                    // 암호화
                    var hash = sha.ComputeHash(message);

                    // base64 컨버팅
                    var encoded = hash.ToBase64UrlEncode();
                    return $"{json1}.{json2}.{encoded}";
                }
            }
            public static string GenerateKickAccessToken(string sub, string f, string channel, int expire)
            {
                using (HMACSHA256 sha = new HMACSHA256(Key))
                {

                    var header = new { };
                    var payload = new
                    {
                        iss = Issuer,
                        exp = DateTime.UtcNow.AddSeconds(expire).ToUnixTime(),
                        vxa = "kick",
                        vxi = Vxi,
                        sub = $"sip:.{sub}.@{Domain}",
                        f = $"sip:.{f}.@{Domain}",
                        //      t = $"sip:confctl-{Prefix}-{channel}@{Domain}"
                    };

                    var json1 = JsonConvert.SerializeObject(header).ToBase64UrlEncode();
                    var json2 = JsonConvert.SerializeObject(payload).ToBase64UrlEncode();

                    var message = $"{json1}.{json2}".ToBytes();

                    // 암호화
                    var hash = sha.ComputeHash(message);

                    // base64 컨버팅
                    var encoded = hash.ToBase64UrlEncode();
                    return $"{json1}.{json2}.{encoded}";
                }

            }
            public static string GenerateJoinAccessToken(string prefix, long account, string channel, int expire, int audibleDistance = 400, int conversationalDistance = 45, double audioFadeIntensityByDistance = 1.0, int audioFadeModel = 1)
            {
                using (HMACSHA256 sha = new HMACSHA256(Key))
                {

                    var header = new { };
                    string t = string.Empty;
                    string probs = string.Empty;
                    if (prefix == "d")
                    {
                        probs = $"!p-{audibleDistance}-{conversationalDistance}-{audioFadeIntensityByDistance.ToString("0.000")}-{audioFadeModel}";

                    }


                    var payload = new
                    {
                        iss = Issuer,
                        exp = DateTime.UtcNow.AddSeconds(expire).ToUnixTime(),
                        vxa = "join",
                        vxi = Vxi,
                        f = $"sip:.{Issuer}.{account}.@{Domain}",
                        t = $"sip:confctl-{prefix}-{Issuer}.{channel}{probs}@{Domain}"
                    };

                    var json1 = JsonConvert.SerializeObject(header).ToBase64UrlEncode();
                    var json2 = JsonConvert.SerializeObject(payload).ToBase64UrlEncode();

                    var message = $"{json1}.{json2}".ToBytes();

                    // 암호화
                    var hash = sha.ComputeHash(message);

                    // base64 컨버팅
                    var encoded = hash.ToBase64UrlEncode();
                    return $"{json1}.{json2}.{encoded}";
                }
            }
        }
    }
}
