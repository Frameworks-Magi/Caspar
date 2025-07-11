using Caspar;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static Caspar.Api;

namespace Caspar.Platform
{
    public class Steam
    {

        //public const string WebApiBaseUrl = "https://api.steampowered.com/";
        public const string WebApiPartnerUrl = "https://partner.steam-api.com/";

        // public static string AppId => Caspar.Api.Config.Steam[(string)Config.Deploy].AppId;
        // public static string MicroTxn => Caspar.Api.Config.Steam[(string)Config.Deploy].MicroTxn;
        // //public static string MicroTxn => Caspar.Api.Config.Steam[(string)"PD"].MicroTxn;
        // public static string ApiKey => Caspar.Api.Config.Steam[(string)Config.Deploy].ApiKey;

        public static string AppId => global::Caspar.Api.Config.Steam[(string)Config.Deploy].AppId;
        public static string MicroTxn => global::Caspar.Api.Config.Steam[(string)Config.Deploy].MicroTxn;
        //public static string MicroTxn => Caspar.Api.Config.Steam[(string)"PD"].MicroTxn;
        public static string ApiKey => global::Caspar.Api.Config.Steam[(string)Config.Deploy].ApiKey;
        /// <summary>
        /// 스팀 서버에 유저 인증 요청.
        /// </summary>
        /// <param token="clientKey">클라이언트로 부터 전송된 티켓(토큰) 값</param>
        /// <returns>OK or Error Message</returns>
        /// 
        public static class SteamUserAuth
        {
            public class Auth
            {
                [JsonProperty(PropertyName = "result")]
                public string Result { get; set; }

                [JsonProperty(PropertyName = "steamid")]
                public ulong SteamId { get; set; }

                [JsonProperty(PropertyName = "ownersteamid")]
                public string OwnerSteamId { get; set; }

                [JsonProperty(PropertyName = "vacbanned")]
                public ushort VACBanned { get; set; }

                [JsonProperty(PropertyName = "publisherbanned")]
                public ushort PublisherBanned { get; set; }
            }
            public class AppOwnership
            {
                [JsonProperty(PropertyName = "ownsapp")]
                public ushort Ownsapp { get; set; }

                [JsonProperty(PropertyName = "permanent")]
                public ushort Permanent { get; set; }

                [JsonProperty(PropertyName = "timestamp")]
                public string Timestamp { get; set; }

                [JsonProperty(PropertyName = "ownersteamid")]
                public string Ownersteamid { get; set; }

                [JsonProperty(PropertyName = "sitelicense")]
                public ushort Sitelicense { get; set; }

                [JsonProperty(PropertyName = "result")]
                public string Result { get; set; }
            }


            public static async Task<Auth> AuthenticateUserTicket(string token)
            {
                var result = new { response = new { @params = new Auth(), error = new { errorcode = "", errordesc = "" } } };

                int @try = 0;
                string responseContent = string.Empty;


                while (@try < 4)
                {
                    string appId = AppId;
                    if ((@try % 2) == 1)
                    {
                        appId = Config.Steam[(string)"PD"].AppId;
                    }

                    ++@try;

                    using (HttpClient httpClient = new HttpClient())
                    {
                        try
                        {
                            var response = await httpClient.GetAsync($"{WebApiPartnerUrl}ISteamUserAuth/AuthenticateUserTicket/v1/?key={ApiKey}&appid={appId}&ticket={token}");

                            var status = response.EnsureSuccessStatusCode().StatusCode;
                            if (status != HttpStatusCode.OK)
                            {
                                Logger.Error($"[Steam] response.EnsureSuccessStatusCode().StatusCode = {status}");
                                continue;
                            }
                            //if (response.EnsureSuccessStatusCode().StatusCode == HttpStatusCode.OK)

                            responseContent = await response.Content.ReadAsStringAsync();

                            try
                            {
                                result = Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(responseContent, result);
                                if (result.response.error != null)
                                {

                                    if (result.response.error.errordesc == "Ticket for other app")
                                    {
                                        continue;
                                    }
                                    Logger.Info($"[Steam] AuthenticateUserTicket result.response.error != null, {responseContent}");
                                    return null;
                                }
                            }
                            catch (Exception e)
                            {
                                Logger.Error($"[Steam] AuthenticateUserTicket Retry {@try}\n {e}");
                                Logger.Verbose($"[Steam] {responseContent}");
                                continue;
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Error($"[Steam] AuthenticateUserTicket Retry {@try}\n {e}");
                            Logger.Verbose($"[Steam] {responseContent}");
                            continue;
                        }
                    }
                    try
                    {
                        Logger.Info($"[Steam] Login Success {responseContent}");
                    }
                    catch (Exception e)
                    {
                        Logger.Info($"{e}");
                    }

                    return result.response.@params;
                }

                return null;

            }

            //isLog : true/false 값에 따라 에러 메세지 출력/미출력
            public static async Task<bool> CheckAppOwnership(string steamID, string appID, bool isLog)
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    try
                    {
                        ///start AppOwnership
                        var UserOwnership = new { appownership = new AppOwnership() };
                        var response = await httpClient.GetAsync($"{WebApiPartnerUrl}ISteamUser/CheckAppOwnership/v2/?key={ApiKey}&steamid={steamID}&appid={appID}");
                        response.EnsureSuccessStatusCode();

                        var responseContent = await response.Content.ReadAsStringAsync();
                        UserOwnership = Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(responseContent, UserOwnership);
                        if (UserOwnership.appownership.Ownsapp == 0)
                        {
                            if (isLog == true)
                            {
                                Logger.Info($"CheckAppOwnership error responseContent : {responseContent}");
                            }

                            return false;
                        }
                        ///end AppOwnership
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"StackTrace : {e.StackTrace}\n Message : {e?.InnerException.ToString()}\n steamID : {steamID}, appID : {appID}");
                        return false;
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// 스팀 서버에 유저 정보 요청.
        /// </summary>
        /// <returns>true or false</returns>
        public static class SteamUser
        {
            public class Summmary
            {
                [JsonProperty(PropertyName = "steamid")]
                public ulong SteamId { get; set; }

                [JsonProperty(PropertyName = "communityvisibilitystate")]
                public int ProfileVisibility { get; set; }

                [JsonProperty(PropertyName = "profilestate")]
                public uint ProfileState { get; set; }

                [JsonProperty(PropertyName = "personaname")]
                public string Nickname { get; set; }

                [JsonProperty(PropertyName = "lastlogoff")]
                public string LastLoggedOffDate { get; set; }

                [JsonProperty(PropertyName = "profileurl")]
                public string ProfileUrl { get; set; }

                [JsonProperty(PropertyName = "avatar")]
                public string AvatarUrl { get; set; }

                [JsonProperty(PropertyName = "avatarmedium")]
                public string AvatarMediumUrl { get; set; }

                [JsonProperty(PropertyName = "avatarfull")]
                public string AvatarFullUrl { get; set; }

                [JsonProperty(PropertyName = "personastate")]
                public int UserStatus { get; set; }

                [JsonProperty(PropertyName = "primaryclanid")]
                public string PrimaryGroupId { get; set; }

                [JsonProperty(PropertyName = "timecreated")]
                public string AccountCreatedDate { get; set; }

                [JsonProperty(PropertyName = "personastateflags")]
                public int personastateflags { get; set; }
            }
            public static async Task<Summmary> GetPlayerSummaries(string steamId)
            {
                string responseContent = string.Empty;
                try
                {
                    var summary = new { response = new { players = new List<Summmary>() } };

                    using (HttpClient httpClient = new HttpClient())
                    {

                        var response = await httpClient.GetAsync(WebApiPartnerUrl + "ISteamUser/GetPlayerSummaries/v2/?key=" + ApiKey + "&steamids=" + steamId);
                        response.EnsureSuccessStatusCode();

                        responseContent = await response.Content.ReadAsStringAsync();
                        summary = Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(responseContent, summary);
                        if (summary.response.players.Count() == 0)
                        {
                            Logger.Info($"GetPlayerSummaries error responseContent : {responseContent}");
                            return null;
                        }
                    }

                    return summary.response.players[0];
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    Logger.Error($"[Steam] Summery Fail {responseContent}, SteamId : {steamId}");
                    return null;
                }

            }

            public class FriendList
            {
                public string steamid { get; set; }
                public string relationship { get; set; }
                public int friend_since { get; set; }
            }

            public static async Task<List<FriendList>> GetFriendList(string steamid)
            {
                var friendList = new { friendslist = new { friends = new List<FriendList>() } };

                for (int i = 0; i < 4; ++i)
                {
                    using (HttpClient httpClient = new HttpClient())
                    {
                        try
                        {
                            var response = await httpClient.GetAsync(WebApiPartnerUrl + "ISteamUser/GetFriendList/v1/?key=" + ApiKey + "&steamid=" + steamid);
                            response.EnsureSuccessStatusCode();

                            string responseContent = await response.Content.ReadAsStringAsync();
                            friendList = Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(responseContent, friendList);
                            if (friendList.friendslist.friends.Count() == 0)
                            {
                                Logger.Error($"GetFriendList error responseContent : {responseContent}");
                                continue;
                            }
                            return friendList.friendslist.friends;
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e);
                            continue;
                        }
                    }
                }

                return null;
            }

        }

        /// <summary>
        /// 스팀 결제.
        /// </summary>
        /// 
        public static class SteamMicroTxn
        {
            public class UserInfo
            {
                public string state { get; set; }
                public string country { get; set; }
                public string currency { get; set; }
                public string status { get; set; }
            }

            /// <summary>
            /// 유저의 상태 정보를 가져온다
            /// </summary>
            /// <param steamId="clientKey">클라이언트 스팀ID 값</param>
            /// <param ip="clientIp">클라이언트 IP 값</param>
            /// <returns> UserInfo or null</returns>
            /// 


            public static async Task<UserInfo> GetUserInfo(string steamId, string ip)
            {
                if (steamId.IsNullOrEmpty() == true)
                {
                    return null;
                }

                if (ip.IsNullOrEmpty() == true || ip == "127.0.0.1")
                {
                    ip = global::Caspar.Api.PublicIp;
                }

                if (ip.StartsWith("172") || ip.StartsWith("192"))
                {
                    ip = global::Caspar.Api.PublicIp;
                }

                var userInfo = new { response = new { result = "", @params = new UserInfo(), @error = new { errorcode = "", errordesc = "" } } };
                //var userInfo = new { response = new { result ="" , @params = new { state ="", country = "", currency = ""} } };

                using (HttpClient httpClient = new HttpClient())
                {
                    try
                    {
                        var response = await httpClient.GetAsync($"{WebApiPartnerUrl}{MicroTxn}/GetUserInfo/v2/?key={ApiKey}&steamids={steamId}&ipaddress={ip}");
                        response.EnsureSuccessStatusCode();

                        string responseContent = await response.Content.ReadAsStringAsync();
                        userInfo = Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(responseContent, userInfo);
                        if (userInfo.response.result == "Failure")
                        {
                            return null;
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"StackTrace : {e.StackTrace}\n Message : {e?.InnerException.ToString()}\n steamID : {steamId}, ip : {ip}");
                        return null;
                    }
                }

                return userInfo.response.@params;
            }

            /// <summary>
            /// 스팀 서버에 결제 영수증 양식을 만들어 전달
            /// </summary>
            /// <param steamId="clientKey">클라이언트 스팀ID 값</param>
            /// <param ip="clientIp">클라이언트 IP 값</param>
            /// <param Basis.Metadata.LobbyShop="lobbyShop">로비 상점 데이터</param>
            /// <returns> orderId or 0</returns>
            /// 
            public static async Task<long> InitTxn(string steamId, string product, string count, string name, string amount, string language, string ip)
            {
                if (ip.IsNullOrEmpty() || ip == "127.0.0.1")
                {
                    ip = global::Caspar.Api.PublicIp;
                }
                if (ip.StartsWith("172") || ip.StartsWith("192"))
                {
                    ip = global::Caspar.Api.PublicIp;
                }


                long orderId = global::Caspar.Api.UniqueKey;
                UserInfo userInfo = await GetUserInfo(steamId, ip);
                if (userInfo == null)
                {
                    Logger.Info($"InitTxn userInfo == null. steamId : {steamId}, ip : {ip}");
                    return 0;
                }

                //https://partner.steamgames.com/doc/store/pricing/currencies


                //int amount = cashShop.GetCash(Schema.Protobuf.Enums.EPlatformType.Steam, userInfo.currency);

                //amount *= 100;

                //if (amount == 0)
                //{
                //    return 0;
                //}

                ////https://partner.steamgames.com/doc/store/localization#supported_languages
                //language = Repository.GetMetadata<Basis.Metadata.LanguageCode>(userInfo.country.ToUpper())?.Language;
                //language ??= "en";

                using (HttpClient httpClient = new HttpClient())
                {
                    try
                    {
                        ///https://partner.steamgames.com/doc/webapi/ISteamMicroTxn#InitTxn 참고
                        string initTxnURL = $"{WebApiPartnerUrl}{MicroTxn}/InitTxn/v3/";

                        var content = new FormUrlEncodedContent(
                            new Dictionary<string, string> {
                            {"key", ApiKey},
                            {"orderid", orderId.ToString()},
                            {"steamid", steamId},
                            {"appid", AppId},
                            //itemcount - 스팀 장바구니에 있는 아이템 개수(itemid[0], itemid[1].. 등록된 아이템의 총 갯수)
                            {"itemcount", "1" },
                            {"language", language},
                            {"currency", userInfo.currency.ToString().ToUpper()},
                            //usersession, ipaddress : 웹 결제시 필요한 정보
                            {"usersession", "client"},
                            {"ipaddress", ip},
                            //itemid : 판매할 ItemId, qty : 판매할 아이템 개수
                            //추후 한번에 2개의 유료 아이템을 사고 싶다면 {itemid[0],qty[0],amount[0],description[1]}, {itemid[1],qty[1],amount[1],description[1]} 이런식으로..
                            {"itemid[0]", product},
                            {"qty[0]", count },
                            //amount : 판매할 금액
                            {"amount[0]", amount.ToString()},
                            //판매할 이름
                            {"description[0]", name}
                                //{"category[0]", ""},
                                //{"associated_bundle[0]", ""},
                                //{"billingtype[0]", ""},
                                //{"startdate[0]", ""},
                                //{"enddate[0]", ""},
                                //{"period[0]", ""},
                                //{"frequency[0]", ""},
                                //{"recurringamt[0]", ""},
                                //{"bundlecount", ""},
                                //{"bundleid[0]", ""},
                                //{"bundle_qty[0]", ""},
                                //{"bundle_desc[0]",""},
                                //{"bundle_category[0]", ""},
                            });

                        HttpResponseMessage response = await httpClient.PostAsync(initTxnURL, content);
                        response.EnsureSuccessStatusCode();

                        var initTxn = new { response = new { result = "", @params = new { orderid = "", transid = "" }, @error = new { errorcode = "", errordesc = "" } } };

                        string responseContent = await response.Content.ReadAsStringAsync();
                        initTxn = Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(responseContent, initTxn);
                        if (initTxn.response.result == "Failure")
                        {
                            Logger.Info($"InitTxn({steamId}, {product}, errorcode = {initTxn.response.error.errorcode} errordesc = {initTxn.response.error.errordesc}");
                            Logger.Info($"{responseContent}");
                            return 0;
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"StackTrace : {e.StackTrace}\n Message : {e?.InnerException.ToString()}\n steamID : {steamId}, ip : {ip}");
                        Logger.Error($"InitTxn Exception error Info : language - {language}, currency{userInfo.currency.ToString().ToUpper()}, itemid[0] - {product}, qty[0] - {count}, amount[0] - {amount}, description[0] - {name}");
                        return 0;
                    }
                }

                return orderId;
            }

            /// <summary>
            /// 스팀 서버에 영수증 처리 완료 요청
            /// </summary>
            /// <param orderId="orderItemId">영수증 Id</param>
            /// <returns> true or false</returns>
            /// 
            public static async Task<bool> FinalizeTxn(long orderId)
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    try
                    {
                        string finalizeTxnURL = $"{WebApiPartnerUrl}{MicroTxn}/FinalizeTxn/v2/";
                        var content = new FormUrlEncodedContent(
                            new Dictionary<string, string> {
                            {"key", ApiKey},
                            {"orderid", orderId.ToString()},
                            {"appid", AppId}
                                });

                        HttpResponseMessage response = await httpClient.PostAsync(finalizeTxnURL, content);
                        response.EnsureSuccessStatusCode();

                        var finalizeTxn = new { response = new { result = "", @params = new { orderid = "", transid = "" }, @error = new { errorcode = "", errordesc = "" } } };

                        string responseContent = await response.Content.ReadAsStringAsync();
                        finalizeTxn = Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(responseContent, finalizeTxn);
                        if (finalizeTxn.response.result == "Failure")
                        {
                            Logger.Info($"FinalizeTxn({orderId} errorcode = {finalizeTxn.response.error.errorcode} errordesc = {finalizeTxn.response.error.errordesc}");
                            return false;
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"StackTrace : {e.StackTrace}\n Message : {e?.InnerException.ToString()}\n orderId : {orderId}");
                        return false;
                    }
                }

                return true;
            }

            public class QueryTxnInfo
            {
                public string orderid { get; set; } = "";
                public string transid { get; set; } = "";
                public string steamid { get; set; } = "";
                public string status { get; set; } = "";
                public string currency { get; set; } = "";
                public string time { get; set; } = "";
                public string country { get; set; } = "";
                public string usstate { get; set; } = "";

                public List<QueryTxnItem> items { get; set; } = new List<QueryTxnItem>();
                public class QueryTxnItem
                {
                    public int itemid { get; set; }
                    public int qty { get; set; }
                    public float amount { get; set; }
                    public int vat { get; set; }
                    public string itemstatus { get; set; }
                }

                public string responseContent { get; set; } = "";
            }

            /// <summary>
            /// 영수증Id를 기반으로 구매 상태 요청
            /// </summary>
            /// <param orderId="orderItemId">영수증 Id</param>
            /// <returns> QueryTxnInfo or null</returns>
            /// 
            public static async Task<QueryTxnInfo> QueryTxn(long orderId)
            {
                var queryTxn = new { response = new { result = "", @params = new QueryTxnInfo(), @error = new { errorcode = "", errordesc = "" } } };

                //var queryTxn = new { response = new { result = "",
                //                    @params = new { orderid = "", transid = "", steamid = "",  status = "", currency = "", time = "", country = "", usstate = "",
                //                                    @items = new List<QueryTxnItem>() },

                //                                    @error = new { errorcode = "", errordesc = "" } } };

                using (HttpClient httpClient = new HttpClient())
                {
                    try
                    {
                        var response = await httpClient.GetAsync($"{WebApiPartnerUrl}{MicroTxn}/QueryTxn/v2/?key={ApiKey}&appid={AppId}&orderid={orderId}");
                        response.EnsureSuccessStatusCode();

                        string responseContent = await response.Content.ReadAsStringAsync();
                        queryTxn = Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(responseContent, queryTxn);
                        if (queryTxn.response.result == "Failure")
                        {
                            Logger.Info($"QueryTxn({orderId}) errorcode = {queryTxn.response.@error.errorcode} errordesc = {queryTxn.response.@error.errordesc}");
                            return null;
                        }
                        queryTxn.response.@params.responseContent = responseContent;
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"StackTrace : {e.StackTrace}\n Message : {e?.InnerException.ToString()}\n orderId : {orderId}");
                        return null;
                    }
                }

                return queryTxn.response.@params;
            }

            public class ReportInfo
            {
                public int count { get; set; } = 0;

                public List<QueryReportOrder> orders { get; set; } = new List<QueryReportOrder>();
                public class QueryReportOrder
                {
                    public string orderid { get; set; } = "";
                    public string transid { get; set; } = "";
                    public string steamid { get; set; } = "";
                    public string status { get; set; } = "";
                    public string currency { get; set; } = "";
                    public string time { get; set; } = "";
                    public string country { get; set; } = "";
                    public string usstate { get; set; } = "";
                    public string timecreated { get; set; } = "";

                    public List<QueryTxnItem> items { get; set; } = new List<QueryTxnItem>();
                    public class QueryTxnItem
                    {
                        public int itemid { get; set; }
                        public int qty { get; set; }
                        public int amount { get; set; }
                        public int vat { get; set; }
                        public string itemstatus { get; set; }
                    }
                }
            }

            /// <summary>
            /// 거래 내역 요청
            /// </summary>
            /// <param type="GAMESALES">Report type (One of: "GAMESALES", "STEAMSTORESALES", "SETTLEMENT")</param>
            /// <param time="2019-07-17T07:33:26Z">거래 시작 시간</param>
            /// <param maxresults=1000>결과 개수(설정 안할 경우 기본 1000개)</param>
            /// <returns> true or false</returns>
            /// 
            public static async Task<ReportInfo> GetReport(DateTime time, int maxresults = 0, string type = "")
            {
                var queryReport = new { response = new { result = "", @params = new ReportInfo(), @error = new { errorcode = "", errordesc = "" } } };

                using (HttpClient httpClient = new HttpClient())
                {
                    try
                    {
                        string reportUrl = $"{WebApiPartnerUrl}{MicroTxn}/GetReport/v4/?key={ApiKey}&appid={AppId}";
                        if (maxresults != 0)
                        {
                            reportUrl += $"&maxresults={maxresults}";
                        }
                        if (type != "")
                        {
                            reportUrl += $"&type={type}";
                        }
                        reportUrl += $"&time={time.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ssZ")}";

                        var response = await httpClient.GetAsync(reportUrl);
                        response.EnsureSuccessStatusCode();

                        string responseContent = await response.Content.ReadAsStringAsync();
                        queryReport = Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(responseContent, queryReport);
                        if (queryReport.response.result == "Failure")
                        {
                            Logger.Info($"GetReport({time}, {type}, {maxresults} errorcode = {queryReport.response.error.errorcode} errordesc = {queryReport.response.error.errordesc}");
                            return null;
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"StackTrace : {e.StackTrace}\n Message : {e?.InnerException.ToString()}\n maxresults : {maxresults}, type : {type}");
                        return null;
                    }
                }

                return queryReport.response.@params;
            }

            /// <summary>
            /// 영수증Id를 기반으로 환불 요청
            /// </summary>
            /// <param orderId="orderItemId">영수증 Id</param>
            /// <returns> true or false</returns>
            /// 
            public static async Task<bool> RefundTxn(string orderId)
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    try
                    {
                        string refundTxnURL = $"{WebApiPartnerUrl}{MicroTxn}/RefundTxn/v2/";
                        var content = new FormUrlEncodedContent(
                            new Dictionary<string, string> {
                            {"key", ApiKey},
                            {"orderid", orderId.ToString()},
                            {"appid", AppId}
                                });

                        HttpResponseMessage response = await httpClient.PostAsync(refundTxnURL, content);
                        response.EnsureSuccessStatusCode();

                        var refundTxn = new { response = new { result = "", @params = new { orderid = "", transid = "" }, @error = new { errorcode = "", errordesc = "" } } };

                        string responseContent = await response.Content.ReadAsStringAsync();
                        refundTxn = Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(responseContent, refundTxn);
                        if (refundTxn.response.result == "Failure")
                        {
                            Logger.Info($"RefundTxn({orderId} errorcode = {refundTxn.response.error.errorcode} errordesc = {refundTxn.response.error.errordesc}");
                            return false;
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"StackTrace : {e.StackTrace}\n Message : {e?.InnerException.ToString()}\n orderId : {orderId}");
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// 스팀 VAC.
        /// </summary>
        /// 
        public static class CheatReportingService
        {
            //유저가 의심되는 유저를 신고
            //TODO : enum으로 만들어서 사용 할것 ex) Schema.Protobuf.Enums.EHackType, Schema.Protobuf.Enums.ESeverityType
            public static async Task<bool> ReportPlayerCheating(string steamId, string steamidreporter, int hackType, bool heuristic, bool detection, bool playerreport, bool noreportid, string gamemode, int severity)
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    try
                    {
                        string reportPlayerCheatingURL = $"{WebApiPartnerUrl}ICheatReportingService/ReportPlayerCheating/v1/";
                        var content = new FormUrlEncodedContent(
                            new Dictionary<string, string> {
                            {"key", ApiKey},
                            {"steamid", steamId},
                            {"appid", AppId}
                            //{"steamidreporter", steamidreporter},
                            //{"appdata", hackType.ToString()},
                            //{"heuristic", heuristic.ToString()}, //신고한 유저가 직접 경험해서 신고한 유형인가?
                            //{"detection", detection.ToString()}, 
                            //{"playerreport", playerreport.ToString()},
                            //{"noreportid", noreportid.ToString()},
                            //{"gamemode", gamemode.ToString()},
                            //{"suspicionstarttime", DateTime.UtcNow.ToString()},
                            //{"severity", severity.ToString()}
                            });

                        HttpResponseMessage response = await httpClient.PostAsync(reportPlayerCheatingURL, content);
                        response.EnsureSuccessStatusCode();

                        var reportPlayerCheating = new { response = new { steamid = "", reportid = "", banstarttime = "", suspicionlevel = "" } };

                        string responseContent = await response.Content.ReadAsStringAsync();
                        reportPlayerCheating = Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(responseContent, reportPlayerCheating);
                        //TODO : 실패에 대한 결과 값이 없다??
                        //if (refundTxn.response.result == "Failure")
                        //{
                        //    return false;
                        //}
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"StackTrace : {e.StackTrace}\n Message : {e?.InnerException.ToString()}\n steamId : {steamId}, hackType : {hackType}, heuristic : {heuristic}, detection : {detection}, playerreport : {playerreport}, noreportid : {noreportid}, gamemode : {gamemode}, severity : {severity}");
                        return false;
                    }
                }

                return true;
            }

            //유저 밴 요청
            public static async Task<string> RequestPlayerGameBan(string steamId, long reportid, string cheatdescription, int duration, bool delayban, int flags)
            {
                string steamid = "";

                using (HttpClient httpClient = new HttpClient())
                {
                    try
                    {
                        string requestPlayerGameBanURL = $"{WebApiPartnerUrl}ICheatReportingService/RequestPlayerGameBan/v1/";
                        var content = new FormUrlEncodedContent(
                            new Dictionary<string, string>{
                            {"key", ApiKey},
                            {"steamid", steamId},
                            {"appid", AppId},
                            {"reportid", reportid.ToString()},
                            {"cheatdescription", cheatdescription},
                            {"duration", duration.ToString()},
                            {"delayban", delayban.ToString()},
                            {"flags", flags.ToString()}
                            });

                        HttpResponseMessage response = await httpClient.PostAsync(requestPlayerGameBanURL, content);
                        response.EnsureSuccessStatusCode();

                        var requestPlayerGameBan = new { response = new { steamid = "" } };

                        string responseContent = await response.Content.ReadAsStringAsync();
                        requestPlayerGameBan = Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(responseContent, requestPlayerGameBan);
                        if (requestPlayerGameBan.response.steamid == null)
                        {
                            Logger.Info($"RequestPlayerGameBan({steamId}, {reportid}, {cheatdescription}, {duration}, {delayban}, {flags})");
                            return steamid;
                        }

                        steamid = requestPlayerGameBan.response.steamid;
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"StackTrace : {e.StackTrace}\n Message : {e?.InnerException.ToString()}\n steamId : {steamId}, reportid : {reportid}, cheatdescription : {cheatdescription}, duration : {duration}, delayban : {delayban}, flags : {flags}");
                        return steamid;
                    }
                }

                return steamid;
            }

            //유저 밴 해제
            public static async Task<string> RemovePlayerGameBan(string steamId)
            {
                string steamid = "";

                using (HttpClient httpClient = new HttpClient())
                {
                    try
                    {
                        string removePlayerGameBanURL = $"{WebApiPartnerUrl}ICheatReportingService/RemovePlayerGameBan/v1/";
                        var content = new FormUrlEncodedContent(
                            new Dictionary<string, string>{
                            {"key", ApiKey},
                            {"steamid", steamId},
                            {"appid", AppId}
                            });

                        HttpResponseMessage response = await httpClient.PostAsync(removePlayerGameBanURL, content);
                        response.EnsureSuccessStatusCode();

                        var removePlayerGameBan = new { response = new { steamid = "" } };

                        string responseContent = await response.Content.ReadAsStringAsync();
                        removePlayerGameBan = Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(responseContent, removePlayerGameBan);
                        if (removePlayerGameBan.response.steamid == null)
                        {
                            Logger.Info($"RemovePlayerGameBan({steamId}) error.");
                            return steamid;
                        }

                        steamid = removePlayerGameBan.response.steamid;
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"StackTrace : {e.StackTrace}\n Message : {e?.InnerException.ToString()}\n steamId : {steamId}");
                        return steamid;
                    }
                }

                return steamid;
            }

            public class ReportInfo
            {
                public string reportid { get; set; } = "";
                public string steamid { get; set; } = "";
                public string steamidreporter { get; set; } = "";
                public string appdata { get; set; } = "";
                public bool playerreport { get; set; } = false;
                public bool heuristic { get; set; } = false;
                public bool detection { get; set; } = false;
                public int timereport { get; set; } = 0;
                public int gamemode { get; set; } = 0;
                public string matchid { get; set; } = "";
                public string cheating_type { get; set; } = "";
            }

            //유저 치트 목록 가져오기
            public static async Task<List<ReportInfo>> GetCheatingReports(int timeend, int timebegin, long reportidmin, bool includereports, bool includebans, string steamid)
            {
                var queryGetCheatingReports = new { response = new { results = new List<ReportInfo>(), @error = new { errorcode = "", errordesc = "" } } };

                using (HttpClient httpClient = new HttpClient())
                {
                    try
                    {
                        string URL = $"{WebApiPartnerUrl}ICheatReportingService/GetCheatingReports/v1/?key={ApiKey}&appid={AppId}&timeend={timeend}&timebegin={timebegin}&reportidmin={reportidmin}";

                        //필요하면 주석 풀고 사용 할것.
                        //URL += $"& includereports = { includereports}";
                        //URL += $"&includebans = { includebans}";
                        if (steamid != "")
                        {
                            URL += $"&steamid = {steamid}";
                        }

                        var response = await httpClient.GetAsync(URL);
                        response.EnsureSuccessStatusCode();

                        string responseContent = await response.Content.ReadAsStringAsync();
                        queryGetCheatingReports = Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(responseContent, queryGetCheatingReports);
                        if (queryGetCheatingReports.response.results.Count == 0)
                        {
                            return null;
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"StackTrace : {e.StackTrace}\n Message : {e?.InnerException.ToString()}\n steamid : {steamid}, timeend : {timeend}, timebegin : {timebegin}, reportidmin : {reportidmin}");
                        return null;
                    }
                }

                return queryGetCheatingReports.response.results;
            }

            //VAC 보고
            public static async Task<bool> ReportCheatData(string steamid, string pathandfilename, string webcheaturl, long time_now, long time_started, long time_stopped, string cheatname, int game_process_id, int cheat_process_id, long cheat_param_1, long cheat_param_2)
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    try
                    {
                        string reportCheatDataURL = $"{WebApiPartnerUrl}ICheatReportingService/ReportCheatData/v1/";
                        var content = new FormUrlEncodedContent(
                            new Dictionary<string, string>{
                            {"key", ApiKey},
                            {"steamid", steamid},
                            {"appid", AppId},
                            {"pathandfilename", pathandfilename},
                            {"webcheaturl", webcheaturl},
                            {"time_now", time_now.ToString()},
                            {"time_started", time_started.ToString()},
                            {"time_stopped", time_stopped.ToString()},
                            {"cheatname", cheatname},
                            {"game_process_id", game_process_id.ToString()},
                            {"cheat_process_id", cheat_process_id.ToString()},
                            {"cheat_param_1", cheat_param_1.ToString()},
                            {"cheat_param_2", cheat_param_2.ToString()}
                            });

                        HttpResponseMessage response = await httpClient.PostAsync(reportCheatDataURL, content);
                        response.EnsureSuccessStatusCode();

                        var reportCheatData = new { response = new { success = false, result_message = "" } };

                        string responseContent = await response.Content.ReadAsStringAsync();
                        reportCheatData = Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(responseContent, reportCheatData);
                        if (reportCheatData.response.success == false)
                        {
                            //TODO : 여기에 뭘 해야 하나?
                            return false;
                        }
                    }
                    catch (Exception e)
                    {
                        //TODO : 추가로 아규먼트 출력 해줄것(기능 테스트가 안되 임시로 남김)
                        Logger.Error($"StackTrace : {e.StackTrace}\n Message : {e?.InnerException.ToString()}\n steamid : {steamid}, pathandfilename : {pathandfilename}");
                        return false;
                    }
                }

                return true;
            }

            //사용자의 VAC 금지 상태를 확인하고 사용자의 VAC 세션 상태를 확인
            public static async Task<bool> RequestVacStatusForUser(string steamId)
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    try
                    {
                        string requestVacStatusForUserURL = $"{WebApiPartnerUrl}ICheatReportingService/RequestVacStatusForUser/v1/";
                        var content = new FormUrlEncodedContent(
                            new Dictionary<string, string>{
                            {"key", ApiKey},
                            {"steamid", steamId},
                            {"appid", AppId}
                            });

                        HttpResponseMessage response = await httpClient.PostAsync(requestVacStatusForUserURL, content);
                        response.EnsureSuccessStatusCode();

                        var requestVacStatusForUser = new { response = new { success = true, session_verified = true } };

                        string responseContent = await response.Content.ReadAsStringAsync();
                        requestVacStatusForUser = Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(responseContent, requestVacStatusForUser);
                        if (requestVacStatusForUser.response.success == false)
                        {
                            //TODO : 여기에 뭘 해야 하나?
                            return false;
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"StackTrace : {e.StackTrace}\n Message : {e?.InnerException.ToString()}\n steamid : {steamId}");
                        return false;
                    }
                }

                return true;
            }
        }
    }
}