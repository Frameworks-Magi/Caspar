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
using static Framework.Caspar.Api;


namespace Framework.Caspar.Google
{
    public static class Api
    {
        public static string ClientId { get; set; } = "605618539331-533mbccnrrdpcuqbatpjlut9djr9866q.apps.googleusercontent.com";
        public static string ClientSecret { get; set; } = "Qx4XjXVCicJTLxVhrOJkDs76";

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


// /*
// using UnityEngine;
// using System.Collections.Generic;
// using Network.Lobby;
// using TMPro;
// using UnityEngine.UI;
// using System.Threading.Tasks;
// using System.Net;
// using System;
// using System.Collections;
// using System.Linq;
// using System.Net.Http;
// using System.Net.Http.Headers;
// using System.Threading;

// public class TitleMode : MonoBehaviour
// {
//     [Serializable]
//     public class Token
//     {
//         [SerializeField] public string access_token;
//     }

//     [Serializable]
//     public class OAuth
//     {
//         [SerializeField] public Token google;
//         [SerializeField] public Token keycloak;
//     }

//     public GameObject titlePrefab;

//     public Slider loadingSlider;
//     public TextMeshProUGUI loadingText;
//     public TextMeshProUGUI loadingPercent;

//     public GameObject loginList;

//     private bool _isInit = false;
//     private Animation _titleAnimation;

//     public enum ConnectStatus
//     {
//         None,
//         WaitSocialLogin,
//         Connect,
//         Logging,
//         Logined,
//         Failed,
//     }

//     [SerializeField] private ConnectStatus _connectStatus;
//     public GameObject startText;

//     private float _animationTime;

//     public void Init()
//     {
//         if (!_isInit)
//         {
//             _titleAnimation = titlePrefab.GetComponent<Animation>();
//             _titleAnimation.Play("Title_Start");
//             _isInit = true;
//         }

//         _animationTime = _titleAnimation.clip.length;
//         loadingSlider.gameObject.SetActive(false);
//         StartCoroutine(Co_StartText());
//         titlePrefab.SetActive(true);
//         _titleAnimation.Play();
//     }

//     public enum LogInType
//     {
//         None,
//         Guest,
// #if UNITY_IOS||UNITY_ANDROID
//         FaceBook,
//         Google,
// #endif
//     };

//     LogInType SuccessLoginType = LogInType.None;

//     public LogInType GetLoginType()
//     {
//         if (SuccessLoginType != LogInType.None)
//         {
//             return SuccessLoginType;
//         }

//         return LogInType.None;
//     }

//     /*
// #if UNITY_IOS||UNITY_ANDROID
//     if(FB.IsLoggedIn)
//     {
//         return LogInType.FaceBook;
//     }
//     #endif

//     return LogInType.None;
// }

// #if UNITY_IOS || UNITY_ANDROID
// private GoogleSignInConfiguration configuration;

// private void InitLogin()
// { 
//       configuration = new GoogleSignInConfiguration {
//             WebClientId = webClientId,
//             RequestIdToken = true
//     };

//     if (!FB.IsInitialized)
//         FB.Init(InitCallBack);
//     else
//         FB.ActivateApp();
// }

// private void InitCallBack()
// {
//     //초기화가 됐다면
//     if (FB.IsInitialized)
//         FB.ActivateApp();
// }

// public void OnLoginFacebook()
// { 
//         titlePrefab.transform.Find("center_anchor/LoginLst").gameObject.SetActive(false);
//     var perms = new List<string>() { "public_profile", "email" };
//     FB.LogInWithReadPermissions(perms, OnFacebookConnect);
// }
// private void OnFacebookConnect(ILoginResult result)
// {
//     if (result.Cancelled)
//     {
//         Debug.Log("User cancelled login");
//     }
//     else
//     {
//         //토큰을 가져온다.
//         authCode = AccessToken.CurrentAccessToken.TokenString;
//     }
// } 
// public void OnLoginGoogle()
// { 
//     titlePrefab.transform.Find("center_anchor/LoginLst").gameObject.SetActive(false);


//   GoogleSignIn.Configuration = configuration;
//   GoogleSignIn.Configuration.UseGameSignIn = false;
//   GoogleSignIn.Configuration.RequestIdToken = true;

//   GoogleSignIn.DefaultInstance.SignIn().ContinueWith(
//     OnAuthenticationFinished);

// } 

// internal void OnAuthenticationFinished(Task<GoogleSignInUser> task) {
//   if (task.IsFaulted) {
//     using (IEnumerator<System.Exception> enumerator =
//             task.Exception.InnerExceptions.GetEnumerator()) {
//       if (enumerator.MoveNext()) {
//         GoogleSignIn.SignInException error =
//                 (GoogleSignIn.SignInException)enumerator.Current;
//         Debug.LogError("Got Error: " + error.Status + " " + error.Message);
//       } else {
//         Debug.LogError("Got Unexpected Exception?!?" + task.Exception);
//       }
//     }
//             titlePrefab.transform.Find("center_anchor/LoginLst").gameObject.SetActive(true);
//   } else if(task.IsCanceled) {
//     Debug.LogError("Canceled");
//             titlePrefab.transform.Find("center_anchor/LoginLst").gameObject.SetActive(true);
//   } else  {

//     authCode = task.Result.IdToken;
//     SuccessLoginType = LogInType.Google;
//     Debug.LogError("Welcome: " + task.Result.DisplayName + "!");
//   }
// }
// #endif
// */

// public async Task OnLogin()
// {
//     titlePrefab.transform.Find("center_anchor/LoginLst").gameObject.SetActive(false);
//     await Task.CompletedTask;

//     // 아래부터 연결이 되었다면 임시 세팅
//     SOTable.initalcharacters initTable = Resources.Load("table/initalcharacters") as SOTable.initalcharacters;

//     if (initTable == null)
//     {
//         Debug.LogError("InitTable is null");
//         return;
//     }

//     var characters = initTable.GetInitalCharacterAll();

//     NetPlayerInfo playerInfo = new NetPlayerInfo()
//     {
//         uid = 0,
//         name = "fortressArena",
//         gem = 0,
//         cring = 111222333,
//         level = 1,
//         exp = 0,
//         charactersCount = characters.Count,
//         characterDecksCount = 0,
//         itemDecksCount = 0,
//         country = 410,
//         portrait_idx = 1,
//         emoticon_idx = 1,
//         game_tutorial_step = 1,
//         beginnerRewardStep = 1,
//         energy = 100,
//         maxEnergy = 300,
//         userPresetIndex = 0,
//         lastStageIdx = 10203,
//         units = new List<UnitInfo>(),
//         items = new List<ItemInfo>(),
//         rentalResetLimitSec = 0,
//         rankScore = 90
//     };

// #if UNITY_EDITOR || TEST_FLAG
//         foreach (var character in characters.Values)
//         {
//             var unitInfo = DataManager.MakeUnitInfo(character.cardIndex, character.idx + 90000);
//             playerInfo.units.Add(unitInfo);
//         }
//         int itemGrade = 0;
//         foreach (var item in DataManager.Instance.itemTable.GetItemAll().Values)
//         {
//             var info = new ItemInfo(item.idx, item.idx, itemGrade, item.idx);
//             playerInfo.items.Add(info);
//             itemGrade = (itemGrade + 1) % 4;
//         }
// #endif

//     // 프리셋
//     playerInfo.userPresetCount = 3;
//     playerInfo.userPreset = new long[3, 3];
//     playerInfo.userPresetName = new string[3];

//     for (int i = 0; i < 3; i++)
//     {
//         for (int j = 0; j < 3; j++)
//         {
//             playerInfo.userPreset[i, j] = -1;
//         }

//         playerInfo.userPresetName[i] = "Preset " + (i + 1);
//     }

// #if UNITY_EDITOR || TEST_FLAG
//         //기본 세팅(임시)
//         for (int i = 0; i < playerInfo.units.Count; i++)
//         {
//             if (i >= 9)
//                 break;

//             playerInfo.userPreset[i / 3, i % 3] = 90000 + i;
//         }
//         playerInfo.itemPreset = new long[4];
//         for (int i = 0; i < 4; i++)
//             playerInfo.itemPreset[i] = -1;
// #endif

//     gamemanager.Instance.playerInfo = playerInfo;

//     //SDH:TEST
//     if (gamemanager.Instance.useServer == false)
//     {
//         SetConnectStatus(ConnectStatus.Logined);
//         return;
//     }
//     gamemanager.Instance.LobbyURI = "https://api.ftv2.io:5777/";
//     //gamemanager.Instance.LobbyURI = "http://localhost:5777/";
// #if UNITY_EDITOR
//         _loadingStep = true;
//         HttpListener httpListener = new HttpListener();
//         httpListener.Prefixes.Add("http://*:5858/");
//         //http://ipfs.atomrigs.io:18081/auth/realms/master/protocol/openid-connect/auth?client_id=game-server&redirect_uri=http%3A%2F%2Fipfs.atomrigs.io%3A18081%2F&state=e11e2b79-9886-4e84-9075-4544b620d238&response_mode=fragment&response_type=code&scope=openid&nonce=d784e7ed-cec7-4aa3-a5d7-cccdcb03772a&code_challenge=8Y7XtDpD4L1qlt8hMdy4K4wis0pta_rioONuHFEtA0Q&code_challenge_method=S256
//         httpListener.Start();
//         httpListener.BeginGetContext(async (IAsyncResult result) =>
//         {
//             Debug.Log("callback");

//             string code;
//             string state;
//             HttpListener httpListener;
//             HttpListenerContext httpContext;
//             HttpListenerRequest httpRequest;
//             HttpListenerResponse httpResponse;
//             string responseString;

//             httpListener = (HttpListener) result.AsyncState;
//             httpContext = httpListener.EndGetContext(result);
//             httpRequest = httpContext.Request;

//             //var body = new StreamReader(httpContext.Request.InputStream).ReadToEnd();

//             //var oauth = JsonUtility.FromJson<OAuth>(body);

//             //Debug.Log(oauth);


//             //gamemanager.Instance.Bearer = oauth.keycloak.access_token;
//             code = httpRequest.QueryString.Get("code");


//             /*
//              * state = httpRequest.QueryString.Get("state");
//              */

//             httpResponse = httpContext.Response;
//             responseString = "<html><body><b>DONE!</b><br>(You can close this tab/window now)</body></html>";
//             byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);

//             httpResponse.ContentLength64 = buffer.Length;
//             System.IO.Stream output = httpResponse.OutputStream;
//             output.Write(buffer, 0, buffer.Length);
//             output.Close();

//             using (var httpClient = new HttpClient())
//             {
//                 httpClient.Timeout = Timeout.InfiniteTimeSpan;
//                 var google = new {access_token = ""};
//                 using (var request = new HttpRequestMessage(new HttpMethod("POST"),
//                     "https://accounts.google.com/o/oauth2/token"))
//                 {
//                     var form = new Dictionary<string, string>();
//                     form.Add("code", code);
//                     form.Add("client_id", "414714303384-murbj6hggt7ic57eduirh4o8jkdmqjac.apps.googleusercontent.com");
//                     form.Add("client_secret", "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");
//                     //form.Add("client_id", "1041608374642-kq627tthqcb3vflnd0u6j0c28brcurf6.apps.googleusercontent.com");
//                     //form.Add("client_secret", "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");
//                     form.Add("redirect_uri", "http://localhost:5858/");
//                     form.Add("grant_type", "authorization_code");

//                     request.Content = new FormUrlEncodedContent(form);
//                     request.Content.Headers.ContentType =
//                         MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");

//                     try
//                     {
//                         var response = await httpClient.SendAsync(request);
//                         var status = response.StatusCode;

//                         if (status != HttpStatusCode.OK)
//                         {
//                             return;
//                         }

//                         var content = await response.Content.ReadAsStringAsync();
//                         google = Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(content, new {access_token = ""});
//                     }
//                     catch (Exception e)
//                     {
//                         return;
//                     }
//                 }

//                 using (var request = new HttpRequestMessage(new HttpMethod("GET"),
//                     $"{gamemanager.Instance.LobbyURI}Authentication/KeyCloak/"))
//                 {
//                     string accessToken = google.access_token;
//                     NetworkManager.Instance.SetGoogle(accessToken);
//                     request.Headers.Add("Authorization", accessToken);
//                     try
//                     {
//                         var response = await httpClient.SendAsync(request);
//                         var status = response.StatusCode;

//                         if (status != HttpStatusCode.OK)
//                         {
//                             return;
//                         }

//                         var content = await response.Content.ReadAsStringAsync();
//                         Console.WriteLine(content);
//                         gamemanager.Instance.Bearer = content;
//                     }
//                     catch (Exception e)
//                     {
//                         return;
//                     }
//                 }
//             }
//             NetworkManager.Instance.StartRefreshSchedule();

//             // server login.
//             var msg = new Schema.Protobuf.Authentication.Login();
//             var ret = await msg.Request();
//             _loadingStep = false;
//             gamemanager.Instance.playerInfo.uid = ret.UID;
//             gamemanager.Instance.playerInfo.name = ret.Name;
//             gamemanager.Instance.playerInfo.isFirstLogin = ret.IsFirstLogin;

//             var totalExp = ret.TotalExp;
//             gamemanager.Instance.playerInfo.level = DataManager.GetLevel(ref totalExp);
//             gamemanager.Instance.playerInfo.exp = (int) totalExp;

//             gamemanager.Instance.playerInfo.energy = ret.Energy.Value;
//             gamemanager.Instance.playerInfo.maxEnergy = DataManager.Instance.configTable.energy.EnergyMax;
//             gamemanager.Instance.playerInfo.energyChargeTime = ret.Energy.Timestamp;

//             // Wallet
//             var wallet = new Schema.Protobuf.Lobby.Wallet();
//             wallet = await wallet.Request();
//             gamemanager.Instance.playerInfo.cring = wallet.Cring;

//             // Rental Select
//             var rentList = new Schema.Protobuf.Lobby.RentList();
//             rentList = await rentList.Request();
//             gamemanager.Instance.playerInfo.rentalResetLimitSec = rentList.Timestamp;
//             gamemanager.Instance.playerInfo.rentalResetLimitCount = rentList.Limit;

//             // Characters
//             var ownerCharacters = new Schema.Protobuf.Lobby.Characters();
//             ownerCharacters = await ownerCharacters.Request();
//             Debug.Log(ownerCharacters);

//             foreach (var c in ownerCharacters.Values)
//             {
//                 var info = DataManager.MakeUnitInfo(c.Id, c.TokenId);
//                 gamemanager.Instance.playerInfo.units.Add(info);
//             }

//             gamemanager.Instance.playerInfo.maxEnergy += ownerCharacters.Values.Count * DataManager.Instance.configTable.energyBonus.TankBonus;

//             // Preset Setting
//             for (int i = 0; i < gamemanager.Instance.playerInfo.units.Count; i++)
//             {
//                 if (i >= 9)
//                     break;

//                 gamemanager.Instance.playerInfo.userPreset[i / 3, i % 3] = gamemanager.Instance.playerInfo.units[i].uid;
//             }

//             // Clear Stage
//             var stage = new Schema.Protobuf.Lobby.StageList();
//             stage = await stage.Request();
//             var lastStageIndex = stage.ClearStages.Keys.Prepend(int.MinValue).Max();
//             gamemanager.Instance.playerInfo.lastStageIdx = lastStageIndex;

//             await Task.CompletedTask;

//             SetConnectStatus(ConnectStatus.Logined);
//             httpListener.Close();
//         }, httpListener);
//         //SetConnectStatus(ConnectStatus.Logined);
//         Application.OpenURL("https://accounts.google.com/o/oauth2/auth?client_id=414714303384-murbj6hggt7ic57eduirh4o8jkdmqjac.apps.googleusercontent.com&redirect_uri=http%3A%2F%2Flocalhost%3A5858%2F&scope=openid%20profile%20email&response_type=code&access_type=offline&state=CfDJ8PepAM8mzwNKqoz5aAyynxP");
//         //Application.OpenURL("https://accounts.google.com/o/oauth2/auth?client_id=1041608374642-kq627tthqcb3vflnd0u6j0c28brcurf6.apps.googleusercontent.com&redirect_uri=https%3A%2F%2Fapi.ftv2.io%3A5777%2FAuthentication%2FGoogle&scope=openid%20profile%20email&response_type=code&access_type=offline&state=CfDJ8PepAM8mzwNKqoz5aAyynxP");
// #endif
//     // 일단 접속 되게
//     // SetConnectStatus(ConnectStatus.Logined);

// #if UNITY_ANDROID && !UNITY_EDITOR
//         _loadingStep = true;
//         await gamemanager.Instance.googleSignIn.OnSignIn();

//         await NetworkManager.Instance.TitleLogin();

//         if (_failLoginStep)
//             return;

//         var msg = new Schema.Protobuf.Authentication.Login();
//         var loggedIn = await msg.Request();
//         _loadingStep = false;

//         gamemanager.Instance.playerInfo.uid = loggedIn.UID;
//         gamemanager.Instance.playerInfo.name = loggedIn.Name;
//         gamemanager.Instance.playerInfo.isFirstLogin = loggedIn.IsFirstLogin;

//         var totalExp = loggedIn.TotalExp;
//         gamemanager.Instance.playerInfo.level = DataManager.GetLevel(ref totalExp);
//         gamemanager.Instance.playerInfo.exp = (int) totalExp;

//         gamemanager.Instance.playerInfo.energy = loggedIn.Energy.Value;
//         gamemanager.Instance.playerInfo.maxEnergy = DataManager.Instance.configTable.energy.EnergyMax;
//         gamemanager.Instance.playerInfo.energyChargeTime = loggedIn.Energy.Timestamp;

//         var wallet = new Schema.Protobuf.Lobby.Wallet();
//         wallet = await wallet.Request();
//         gamemanager.Instance.playerInfo.cring = wallet.Cring;

//         var rentList = new Schema.Protobuf.Lobby.RentList();
//         rentList = await rentList.Request();
//         gamemanager.Instance.playerInfo.rentalResetLimitSec = rentList.Timestamp;
//         gamemanager.Instance.playerInfo.rentalResetLimitCount = rentList.Limit;

//         await Task.CompletedTask;
//         var ownerCharacters = new Schema.Protobuf.Lobby.Characters();
//         ownerCharacters = await ownerCharacters.Request();

//         foreach (var c in ownerCharacters.Values)
//         {
//             var info = DataManager.MakeUnitInfo(c.Id, c.TokenId);
//             gamemanager.Instance.playerInfo.units.Add(info);
//         }
//         gamemanager.Instance.playerInfo.maxEnergy += ownerCharacters.Values.Count * DataManager.Instance.configTable.energyBonus.TankBonus;

//         // 기본 세팅
//         for (int i = 0; i < gamemanager.Instance.playerInfo.units.Count; i++)
//         {
//             if (i >= 9)
//                 break;

//             gamemanager.Instance.playerInfo.userPreset[i / 3, i % 3] = gamemanager.Instance.playerInfo.units[i].uid;
//         }

//         if (string.IsNullOrEmpty(gamemanager.Instance.Bearer) == true)
//         {
//             // 로그인 실패
//             CommonManager.Instance.OneButtonPopUp("Error", "Login Failed");
//         }
//         else
//             SetConnectStatus(ConnectStatus.Logined);
// #endif
// }

// public void Loading()
// {
//     gamemanager.Instance.SetUICamera(Color.white);

//     _connectStatus = ConnectStatus.WaitSocialLogin;

//     // TOCO CJK - 상태변경
//     //Tcp.Instance.Connect();
//     titlePrefab.transform.Find("center_anchor/lb_message").GetComponent<TMPro.TextMeshProUGUI>().text =
//         DataManager.Instance.GetString(105010047);
//     titlePrefab.transform.Find("Openning/StartGame").GetComponent<TMPro.TextMeshProUGUI>().text =
//         DataManager.Instance.GetString(105020004);

//     //현재는 페이스북을 지원안한다
//     titlePrefab.transform.Find("center_anchor/LoginLst/Facebook").gameObject.SetActive(false);
//     titlePrefab.transform.Find("center_anchor/LoginLst").gameObject.SetActive(false);

// #if BGM
//         gamemanager.Instance.soundManager.Play("ui_title", SoundManager.Sound.Bgm);
// #endif
// }

// private float _loadingTime = 0f;
// private bool _loadingStep = true;
// private float t1 = 0.5f;
// private int count = 0;

// public void OnUpdate(float deltaTime)
// {
//     if (_connectStatus == ConnectStatus.WaitSocialLogin)
//     {
//         var type = GetLoginType();

//         if (type != LogInType.None)
//         {
//             loginList.SetActive(false);
//             titlePrefab.transform.Find("center_anchor/LoginLst").gameObject.SetActive(false);
//             if (Tcp.Instance.socket == null)
//             {
//                 Tcp.Instance.Connect();
//             }
//         }
//     }

//     if (string.IsNullOrEmpty(writeStr) == false)
//         titlePrefab.transform.Find("center_anchor/lb_message").GetComponent<TMPro.TextMeshProUGUI>().text = writeStr;

//     if (_titleAnimation.isPlaying == false)
//     {
//         _titleAnimation.Play("Title_Loop");
//     }

//     if (_connectStatus == ConnectStatus.Logined && loadingSlider.value >= 1.0f)
//         gamemanager.Instance.TitleOnEndOutro();

//     if (_loadingStep == false)
//     {
//         _loadingTime += deltaTime * 2f;
//         loadingSlider.value = _loadingTime / _animationTime;
//         loadingPercent.text = $"{loadingSlider.value:P0}";
//     }

//     t1 -= deltaTime;
//     if (t1 < 0)
//     {
//         loadingText.text += ".";
//         count++;
//         t1 = 0.5f;
//     }

//     if (count > 3)
//     {
//         loadingText.text = "Loading";
//         count = 0;
//     }

//     if (_failLoginStep)
//     {
//         _isLoginClicked = false;
//         startText.SetActive(true);
//         loadingSlider.gameObject.SetActive(false);

//         _failLoginStep = false;
//     }
// }

// public void Release()
// {
//     titlePrefab.SetActive(false);
// }

// string writeStr;
// private void SetConnectStatus(ConnectStatus status)
// {
//     if (_connectStatus == status)
//         return;

//     switch (status)
//     {
//         case ConnectStatus.Connect:
//             writeStr = DataManager.Instance.GetString(105020002);
//             break;
//         case ConnectStatus.Logging:
//             writeStr = DataManager.Instance.GetString(105020001);
//             break;
//         case ConnectStatus.Logined:
//             writeStr = DataManager.Instance.GetString(105020003);
//             break;
//         case ConnectStatus.Failed:
//             writeStr = DataManager.Instance.GetString(105020005);
//             break;
//     }

//     _connectStatus = status;
// }

// public void OnConnect(bool success)
// {
//     SetConnectStatus(success ? ConnectStatus.Connect : ConnectStatus.Failed);
// }

// private bool _isLoginClicked = false;
// private bool _failLoginStep = false;
// public void OnClickStartButton()
// {
//     if (_connectStatus == ConnectStatus.Connect && _isLoginClicked == false)
//     {
//         _isLoginClicked = true;

//         _loadingStep = false;
//         loadingSlider.value = 0f;
//         loadingPercent.text = $"{loadingSlider.value:P0}";

//         startText.SetActive(false);
//         loadingSlider.gameObject.SetActive(true);

//         _ = OnLogin();
//     }
// }

// public void OnLoginFailed()
// {
//     _failLoginStep = true;
// }

// private RectTransform _startText = null;
// private IEnumerator Co_StartText()
// {
//     if (_startText == null)
//         _startText = startText.GetComponent<RectTransform>();

//     var t = 0f;
//     var turn = false;

//     while (true)
//     {
//         if (turn)
//             _startText.localScale = Vector3.Lerp(_startText.localScale, Vector3.one * 1.2f, t);
//         else
//             _startText.localScale = Vector3.Lerp(_startText.localScale, Vector3.one, t);

//         t += Time.deltaTime;

//         if (t >= 0.3f)
//         {
//             t = 0f;
//             turn = !turn;
//         }

//         yield return null;
//     }

//     yield return null;
// }
// }