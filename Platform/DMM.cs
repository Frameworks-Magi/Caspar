using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using static Framework.Caspar.Api;

namespace Framework.Caspar.Platform
{
    public static class DMM
    {
        internal static string consumer_key
        {
            get
            {

                try
                {
                    return global::Framework.Caspar.Api.Config.DMM[(string)global::Framework.Caspar.Api.Config.Deploy].ConsumerKey;
                }
                catch (Exception)
                {

                }

                return "b7sdPnWEIHmOBNoU"; //live
                //return "XgRzprUApTmUEI5p"; //qa
            }
        }
        internal static string consumer_secret
        {
            get
            {
                try
                {
                    return global::Framework.Caspar.Api.Config.DMM[(string)global::Framework.Caspar.Api.Config.Deploy].ConsumerSecret;
                }
                catch (Exception)
                {

                }

                return "vdY?p0p@W7E6QH@teuwn@ybtvdOleC-l"; //live
                //return "qurdJyQEYxv[aXNx$U@396ntlq1=tIrk"; //qa
            }
        }
        internal static int app_id
        {
            get
            {
                try
                {
                    return global::Framework.Caspar.Api.Config.DMM[(string)global::Framework.Caspar.Api.Config.Deploy].AppID;
                }
                catch (Exception)
                {

                }

                return 669116; //live
                //return 339; //qa
            }
        }

        internal static string AuthURL
        {
            get
            {
                try
                {
                    return global::Framework.Caspar.Api.Config.DMM[(string)global::Framework.Caspar.Api.Config.Deploy].AuthURL;
                }
                catch (Exception)
                {

                }

                return "https://netgame-api.dmm.com/api"; //live
                //return "https://sbx-netgameclient-api.dmm.com/api"; //qa
            }
        }

        public static string GetAuthorization(string url, long uid, long timestamp)
        {

            string auth = string.Empty;
            auth += $"oauth_consumer_key=\"{consumer_key}\",";
            auth += $"oauth_nonce=\"{uid}\",";
            auth += $"oauth_signature_method=\"HMAC-SHA1\",";
            auth += $"oauth_timestamp=\"{timestamp}\",";
            auth += $"oauth_version=\"1.0\",";
            auth += $"oauth_signature=\"{GetSignature(url, uid.ToString(), timestamp)}\"";


            return auth;
        }

        public static string GetSignature(string url, string uid, long timestamp)
        {
            var hash = GetBaseString(url, uid, timestamp);
            Regex reg = new Regex(@"%[a-f0-9]{2}");
            var key = $"{reg.Replace($"{HttpUtility.UrlEncode(consumer_secret, Encoding.UTF8)}", m => m.Value.ToUpperInvariant())}&";
            using (var hmacsha256 = new HMACSHA1($"{key}".ToBytes()))
            {
                var ret = hmacsha256.ComputeHash(hash.ToBytes());
                var tt = ret.ToBase64String();
                return tt;
            }
        }

        public static string GetBaseString(string url, string uid, long timestamp)
        {
            SortedList<string, string> keys = new SortedList<string, string>();

            keys.Add("oauth_consumer_key", consumer_key);
            keys.Add("oauth_nonce", $"{uid}");
            keys.Add("oauth_timestamp", $"{timestamp}");
            keys.Add("oauth_signature_method", "HMAC-SHA1");
            keys.Add("oauth_version", "1.0");

            //매개 변수는 키와 값을 "="로 연결 한 후, 키를 알파벳 오름차순으로 정렬 한 것을 "&"로 연결합니다.
            string @params = string.Empty;

            foreach (var e in keys)
            {
                if (@params.IsNullOrEmpty() == false)
                {
                    @params += "&";
                }
                @params += $"{HttpUtility.UrlEncode(e.Key, Encoding.UTF8)}={HttpUtility.UrlEncode(e.Value, Encoding.UTF8)}";
            }

            Regex reg = new Regex(@"%[a-f0-9]{2}");
            string upper = reg.Replace($"POST&{HttpUtility.UrlEncode(url, Encoding.UTF8)}&{HttpUtility.UrlEncode(@params, Encoding.UTF8)}", m => m.Value.ToUpperInvariant());
            return upper;
        }

        public static class Auth
        {
            public static async Task<string> UpdateToken(string viewer_id, string onetime_token)
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    var url = AuthURL + "/Auth/updateToken";
                    using (var request = new HttpRequestMessage(new HttpMethod("POST"), url))
                    {

                        var header = $"OAuth {(GetAuthorization(url, global::Framework.Caspar.Api.UniqueKey, DateTime.UtcNow.ToUnixTime()))}";
                        request.Headers.TryAddWithoutValidation("Authorization", header);
                        var form = new MultipartFormDataContent();
                        form.Add(new StringContent(app_id.ToString()), "app_id");
                        form.Add(new StringContent(viewer_id), "viewer_id");
                        form.Add(new StringContent(onetime_token), "onetime_token");
                        request.Content = form;
                        var response = await httpClient.SendAsync(request);
                        var status = response.EnsureSuccessStatusCode().StatusCode;
                        if (status != HttpStatusCode.OK)
                        {
                            return string.Empty;
                        }
                        var resultString = await response.Content.ReadAsStringAsync();

                        dynamic result = JObject.Parse(resultString);

                        if (result.result_code == 0)
                        {

                            var r = (string)result.onetime_token;
                            if (0 < r.Length)
                                return r;
                            return string.Empty;
                        }

                        return string.Empty;
                    }
                }

            }

            public static async Task<bool> CheckLogin(string viewer_id, string onetime_token)
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    var url = AuthURL + "/Auth/checkLogin";
                    using (var request = new HttpRequestMessage(new HttpMethod("POST"), url))
                    {

                        var header = $"OAuth {(GetAuthorization(url, global::Framework.Caspar.Api.UniqueKey, DateTime.UtcNow.ToUnixTime()))}";

                        Logger.Info($"DMM {header}");

                        request.Headers.TryAddWithoutValidation("Authorization", header);
                        var form = new MultipartFormDataContent();
                        form.Add(new StringContent(app_id.ToString()), "app_id");
                        form.Add(new StringContent(viewer_id), "viewer_id");
                        form.Add(new StringContent(onetime_token), "onetime_token");
                        request.Content = form;
                        var response = await httpClient.SendAsync(request);
                        var status = response.EnsureSuccessStatusCode().StatusCode;
                        if (status != HttpStatusCode.OK)
                        {
                            return false;
                        }
                        var resultString = await response.Content.ReadAsStringAsync();

                        dynamic result = JObject.Parse(resultString);

                        if (result.result_code == 0)
                        {
                            return true;
                        }

                        return false;
                    }
                }
            }

            public static async Task<string> Payment(string viewer_id, string onetime_token, string itemId, string itemName, int unitPrice, int quantity)
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    var url = AuthURL + "/Payment";
                    using (var request = new HttpRequestMessage(new HttpMethod("POST"), url))
                    {

                        var header = $"OAuth {(GetAuthorization(url, global::Framework.Caspar.Api.UniqueKey, DateTime.UtcNow.ToUnixTime()))}";
                        request.Headers.TryAddWithoutValidation("Authorization", header);
                        var form = new MultipartFormDataContent();
                        form.Add(new StringContent(app_id.ToString()), "app_id");
                        form.Add(new StringContent(viewer_id), "viewer_id");
                        form.Add(new StringContent(onetime_token), "onetime_token");
                        form.Add(new StringContent(itemId), "itemId");
                        form.Add(new StringContent(itemName), "itemName");
                        form.Add(new StringContent(unitPrice.ToString()), "unitPrice");
                        form.Add(new StringContent(quantity.ToString()), "quantity");
                        form.Add(new StringContent("https://fortressv2.retiad.com/DMM/PaymentCallback"), "callbackurl");
                        form.Add(new StringContent("https://fortressv2.retiad.com/DMM/PaymentFinish"), "finishurl");


                        request.Content = form;
                        var response = await httpClient.SendAsync(request);
                        var status = response.EnsureSuccessStatusCode().StatusCode;
                        if (status != HttpStatusCode.OK)
                        {
                            return "";
                        }
                        var resultString = await response.Content.ReadAsStringAsync();

                        dynamic result = JObject.Parse(resultString);

                        if (result.result_code == 0)
                        {
                            return result.payment_id;
                        }

                        return "";
                    }

                }

            }

            public static async Task<int> GetPoint(string viewer_id, string onetime_token)
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    var url = AuthURL + "/Auth/getPoint";
                    using (var request = new HttpRequestMessage(new HttpMethod("POST"), url))
                    {

                        Logger.Info($"{url},{app_id},{viewer_id},{onetime_token}");

                        //test code
                        //onetime_token = "28460fa359476bb18da7f10f2a98c103";

                        var header = $"OAuth {(GetAuthorization(url, global::Framework.Caspar.Api.UniqueKey, DateTime.UtcNow.ToUnixTime()))}";
                        request.Headers.TryAddWithoutValidation("Authorization", header);
                        var form = new MultipartFormDataContent();
                        form.Add(new StringContent(app_id.ToString()), "app_id");
                        form.Add(new StringContent(viewer_id), "viewer_id");
                        form.Add(new StringContent(onetime_token), "onetime_token");
                        request.Content = form;
                        var response = await httpClient.SendAsync(request);
                        var status = response.EnsureSuccessStatusCode().StatusCode;
                        if (status != HttpStatusCode.OK)
                        {
                            return 0;
                        }
                        var resultString = await response.Content.ReadAsStringAsync();

                        dynamic result = JObject.Parse(resultString);

                        if (result.result_code == 0)
                        {
                            return (int)result.can_use_point;
                        }

                        return 0;
                    }
                }


            }
            public static async Task<string> GetProfile(string viewer_id, string onetime_token)
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    var url = AuthURL + "/Auth/getProfile";
                    using (var request = new HttpRequestMessage(new HttpMethod("POST"), url))
                    {

                        var header = $"OAuth {(GetAuthorization(url, global::Framework.Caspar.Api.UniqueKey, DateTime.UtcNow.ToUnixTime()))}";
                        request.Headers.TryAddWithoutValidation("Authorization", header);
                        var form = new MultipartFormDataContent();
                        form.Add(new StringContent(app_id.ToString()), "app_id");
                        form.Add(new StringContent(viewer_id), "viewer_id");
                        form.Add(new StringContent(onetime_token), "onetime_token");
                        request.Content = form;
                        var response = await httpClient.SendAsync(request);
                        var status = response.EnsureSuccessStatusCode().StatusCode;
                        if (status != HttpStatusCode.OK)
                        {
                            return "error1";
                        }
                        var resultString = await response.Content.ReadAsStringAsync();

                        try
                        {

                            dynamic result = JObject.Parse(resultString);

                            if (result.result_code == 0)
                            {
                                return (string)result.nickname;
                            }

                            return "error2";

                        }
                        catch (Exception)
                        {
                            return "Exception";
                            //Logger.Error(e);
                        }

                    }
                }
            }
        }
    }
}
