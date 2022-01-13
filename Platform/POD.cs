using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static Framework.Caspar.Api;

namespace Framework.Caspar.Platform
{
    public static class POD
    {
        internal static string AppId { get; set; } = "PGHTA";
        //internal static string ClientSecret { get; set; } = "0937d74a2ee1409ea9e293ddcc4f92e0";  // QA
        internal static string ClientSecret
        {
            get
            {
                try
                {
                    var r = global::Framework.Caspar.Api.Config.POD[(string)global::Framework.Caspar.Api.Config.Deploy].ClientSecret;
                    if (r != null)
                    {
                        return r;
                    }

                }
                catch (Exception)
                {

                }


                //if (Framework.Caspar.Api.Config.POD != null)
                //    return Framework.Caspar.Api.Config.POD.ClientSecret;

                //return "0937d74a2ee1409ea9e293ddcc4f92e0"; // qa
                return "ef133fd17ed044be8a5a9b49376b3981"; //live
            }
        }
        internal static string URI
        {
            get
            {
                try
                {
                    var r = global::Framework.Caspar.Api.Config.POD[(string)global::Framework.Caspar.Api.Config.Deploy].Login;
                    if (r != null)
                    {
                        return r;
                    }

                }
                catch (Exception e)
                {

                }
                //  
                //sandbox https://pgpod-sandbox-podgameapi-8080.gcld-line.com
                //live https://pgpod-release-podgameapi-8080.gcld-line.com

                return "pgpod-release-podgameapi-8080.gcld-line.com";
            }
        }

        internal static string GROWTHY
        {
            get
            {
                try
                {
                    var r = global::Framework.Caspar.Api.Config.POD[(string)global::Framework.Caspar.Api.Config.Deploy].Growthy;
                    if (r != null)
                    {
                        return r;
                    }

                }
                catch (Exception)
                {

                }


                //if (Framework.Caspar.Api.Config.POD != null)
                //return Framework.Caspar.Api.Config.POD.Growthy;
                //return "growthy-sandbox-growthywebcollector-80.gcld-line.com";
                return "growthy-release-growthywebcollector-80.gcld-line.com";
            }
        }

        //internal static string GROWTHY { get; set; } = "growthy-sandbox-growthywebcollector-80.gcld-line.com";

        internal static string AchievementURI
        {
            get
            {
                try
                {
                    var r = global::Framework.Caspar.Api.Config.POD[(string)global::Framework.Caspar.Api.Config.Deploy].AchievementURI;
                    if (r != null)
                    {
                        return r;
                    }
                }
                catch (Exception)
                {

                }

                //return "http://lg-sandbox-achievement-8080.gcld-line.com"; //sandbox
                return "https://lg-release-achievement-8080.gcld-line.com"; //live
            }
        }
        public static class Token
        {
            public static async Task<string> Exchange(string token)
            {
                using (HttpClient httpClient = new HttpClient())
                {

                    var addr = $"https://{URI}/api/v1/auth/exchange";

                    //Logger.Error($"Exchange addr : {addr}");

                    using (var request = new HttpRequestMessage(new HttpMethod("GET"), addr))
                    {
                        try
                        {
                            request.Headers.TryAddWithoutValidation("X-Linegame-AppId", AppId);
                            request.Headers.TryAddWithoutValidation("X-Linegame-InstantToken", token);

                            var response = await httpClient.SendAsync(request);
                            var status = response.EnsureSuccessStatusCode().StatusCode;
                            if (status != HttpStatusCode.OK)
                            {
                                return string.Empty;
                            }
                            var content = await response.Content.ReadAsStringAsync();
                            var json = new { userToken = "", expireTime = "" };
                            json = JsonConvert.DeserializeAnonymousType(content, json);
                            return json.userToken;

                        }
                        catch (Exception e)
                        {
                            Logger.Error($"Exchange error : {e}");
                        }

                        return String.Empty;
                    }
                }
            }

            public static async Task<string> Exchange(string token, string deploy)
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    var uri = global::Framework.Caspar.Api.Config.POD[deploy].Login;
                    var addr = $"https://{uri}/api/v1/auth/exchange";

                    //Logger.Error($"Exchange addr : {addr}");

                    using (var request = new HttpRequestMessage(new HttpMethod("GET"), addr))
                    {
                        try
                        {
                            request.Headers.TryAddWithoutValidation("X-Linegame-AppId", AppId);
                            request.Headers.TryAddWithoutValidation("X-Linegame-InstantToken", token);

                            var response = await httpClient.SendAsync(request);
                            var status = response.EnsureSuccessStatusCode().StatusCode;
                            if (status != HttpStatusCode.OK)
                            {
                                return string.Empty;
                            }
                            var content = await response.Content.ReadAsStringAsync();
                            var json = new { userToken = "", expireTime = "" };
                            json = JsonConvert.DeserializeAnonymousType(content, json);
                            return json.userToken;

                        }
                        catch (Exception e)
                        {
                            Logger.Error($"Exchange error : {e}");
                        }

                        return String.Empty;
                    }
                }
            }


            public static async Task<string> Verify(string token)
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    using (var request = new HttpRequestMessage(new HttpMethod("GET"), $"https://{URI}/api/v1/auth/verify"))
                    {
                        request.Headers.TryAddWithoutValidation("X-Linegame-AppId", AppId);
                        request.Headers.TryAddWithoutValidation("X-Linegame-UserToken", token);

                        var response = await httpClient.SendAsync(request);
                        var status = response.EnsureSuccessStatusCode().StatusCode;
                        if (status != HttpStatusCode.OK)
                        {
                            return string.Empty;
                        }
                        var content = await response.Content.ReadAsStringAsync();
                        var json = new { userKey = "" };
                        json = JsonConvert.DeserializeAnonymousType(content, json);
                        return json.userKey;
                    }
                }
            }
        }

        public static class User
        {
            public class Profile
            {
                public string displayName { get; set; }
                public string pictureUrl { get; set; }
                public string country { get; set; }
            }
            public static async Task<Profile> GetProfile(string token)
            {
                using (var httpClient = new HttpClient())
                {
                    using (var request = new HttpRequestMessage(new HttpMethod("GET"), $"https://{URI}/api/v1/user/profile"))
                    {
                        request.Headers.TryAddWithoutValidation("X-Linegame-AppId", AppId);
                        request.Headers.TryAddWithoutValidation("X-Linegame-UserToken", token);

                        var response = await httpClient.SendAsync(request);
                        var status = response.EnsureSuccessStatusCode().StatusCode;
                        if (status != HttpStatusCode.OK)
                        {
                            return null;
                        }
                        var content = await response.Content.ReadAsStringAsync();
                        var profile = JsonConvert.DeserializeObject<Profile>(content);
                        return profile;
                    }
                }
            }
        }

        public static class Billing
        {
            public class Balance
            {
                public int total { get; set; }
                public int paid { get; set; }
                public int bonus { get; set; }
                public int cpFree { get; set; }
                public int externalFree { get; set; }
            }

            public class UseCoin
            {
                public string paySequenceNo { get; set; }
                public Balance use { get; set; }
                public Balance balance { get; set; }
            }
            public static async Task<Balance> GetBalance(string token)
            {
                using (var httpClient = new HttpClient())
                {
                    using (var request = new HttpRequestMessage(new HttpMethod("GET"), $"https://{URI}/api/v1/coin/balance"))
                    {
                        request.Headers.TryAddWithoutValidation("X-Linegame-AppId", AppId);
                        request.Headers.TryAddWithoutValidation("X-Linegame-UserToken", token);

                        var response = await httpClient.SendAsync(request);
                        var status = response.EnsureSuccessStatusCode().StatusCode;
                        if (status != HttpStatusCode.OK)
                        {
                            return null;
                        }
                        var content = await response.Content.ReadAsStringAsync();
                        var profile = JsonConvert.DeserializeObject<Balance>(content);
                        return profile;
                    }
                }
            }

            public static async Task<UseCoin> Use(string token, long id, string name, int price)
            {
                using (var httpClient = new HttpClient())
                {
                    using (var request = new HttpRequestMessage(new HttpMethod("POST"), $"https://{URI}/api/v1/coin/use"))
                    {
                        request.Headers.TryAddWithoutValidation("X-Linegame-AppId", AppId);
                        request.Headers.TryAddWithoutValidation("X-Linegame-UserToken", token);
                        request.Content = new StringContent($"{{\n \"transactionId\":\"{(global::Framework.Caspar.Api.UniqueKey)}\",\n \"productId\":\"{id}\",\n \"useAmount\": {price},\n \"productName\":\"{name}\"\n}}");
                        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                        var response = await httpClient.SendAsync(request);
                        var status = response.EnsureSuccessStatusCode().StatusCode;
                        if (status != HttpStatusCode.OK)
                        {
                            return null;
                        }
                        var content = await response.Content.ReadAsStringAsync();
                        var ret = JsonConvert.DeserializeObject<UseCoin>(content);
                        return ret;
                    }
                }
            }
        }


        //http://lg-sandbox-achievement-8080.gcld-line.com/achievement/v3.0/actions/LGSDKTEST_level_up/increment

        //https://sign.game.line.me/auth/login?redirectUrl=https://gdc.game.line.me/games/supports/1654017767/technical/1/11327
        public static class Achievement
        {

            public class Action
            {
                public int goalPoint;
                public string completeState;
                public string id;
                public int point;
            }

            public class achievement
            {
                public string openState { get; set; }
                public string completeState { get; set; }
                public int resultCode { get; set; }
                public string id { get; set; }

                public List<Action> Actions = new List<Action>();
            }


            public class IncrementResult
            {
                public List<achievement> achievements { get; set; } = new List<achievement>();
            }


            public static string GetLineStickerReward(string country, int mode) => (country.ToUpper(), mode) switch
            {
                ("TW", 1) => "TW_Sticker_PGHTA_SOLO",
                ("TW", 3) => "TW_Sticker_PGHTA_TRIO",
                ("TW", 8) => "TW_Sticker_PGHTA_SOLO",
                ("TH", 1) => "TH_Sticker_PGHTA_SOLO",
                ("TH", 3) => "TH_Sticker_PGHTA_TRIO",
                ("TH", 8) => "TW_Sticker_PGHTA_SOLO",
                (_, _) => string.Empty
            };

            public static async Task<bool> Increment(string userKey, string action)
            {
                using (var httpClient = new HttpClient())
                {
                    using (var request = new HttpRequestMessage(new HttpMethod("POST"), $"{AchievementURI}/achievement/v3.0/actions/{action}/increment"))
                    {
                        request.Headers.TryAddWithoutValidation("X-Linegame-AppId", AppId);
                        request.Headers.TryAddWithoutValidation("X-Linegame-AppSecret", ClientSecret);
                        request.Content = new StringContent($"{{\n \"userKey\":\"{userKey}\",\n \"point\":1\n}}");
                        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                        try
                        {
                            var response = await httpClient.SendAsync(request);

                            var status = response.EnsureSuccessStatusCode().StatusCode;
                            if (status != HttpStatusCode.OK)
                            {
                                return false;
                            }
                            var content = await response.Content.ReadAsStringAsync();
                            var ret = JsonConvert.DeserializeObject<IncrementResult>(content);

                            if (ret == null)
                            {
                                Logger.Error("Increment ret == null");
                                return false;
                            }

                            foreach (var item in ret.achievements)
                            {
                                switch (item.id)
                                {
                                    case "TH_Sticker_PGHTA":
                                    case "TW_Sticker_PGHTA":
                                        if (item.completeState == "COMPLETE")
                                        {

                                            Logger.Info($"Increment true : {userKey} : {action}" + content);
                                            return true;
                                        }

                                        break;
                                }
                            }

                            return false;

                        }
                        catch (Exception e)
                        {

                            Logger.Error(e);
                        }
                        return false;



                    }
                }
            }

        }

        public static class Growthy
        {
            public static async Task HeartBeat(string country, string os_code, string os_version, string version, string userKey)
            {
                using (var httpClient = new HttpClient())
                {
                    try
                    {
                        var uri = $"https://{GROWTHY}/v3/basic/heartbeat";
                        using (var request = new HttpRequestMessage(new HttpMethod("POST"), $"{uri}"))
                        {
                            request.Headers.TryAddWithoutValidation("X-Growthy-App-ID", AppId);

                            var json = new
                            {
                                sdk_type = "POD",
                                country_code = country,
                                os_code = os_code,
                                os_version = os_version,
                                service_version = version,
                                idp_type = "0",
                                idp_id = userKey,
                                client_log_datetime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz")
                            };

                            Logger.Verbose(uri);
                            Logger.Verbose(JsonConvert.SerializeObject(json));
                            request.Content = new StringContent(JsonConvert.SerializeObject(json));
                            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                            Logger.Info(request.Content);
                            var response = await httpClient.SendAsync(request);
                            var status = response.EnsureSuccessStatusCode().StatusCode;
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                    }
                }
            }
        }


    }
}
