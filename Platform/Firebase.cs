using FirebaseAdmin;
using System.IO;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using FirebaseAdmin.Auth;
using System.Threading.Tasks;
using System;

namespace Framework.Caspar.Platform.Firebase
{
    public static class Auth
    {
        public static FirebaseAdmin.FirebaseApp App { get; set; }

        public static async Task<FirebaseAdmin.Auth.UserRecord> Verify(string idToken)
        {
            try
            {
                var ret = await FirebaseAuth.GetAuth(App).VerifyIdTokenAsync(idToken);
                var user = await FirebaseAuth.GetAuth(App).GetUserAsync(ret.Uid);

                return user;
            }
            catch (Exception e)
            {
                //  Logger.Debug(e);
            }
            return null;
        }

        public static async Task<FirebaseAdmin.Auth.UserRecord> Get(string uid)
        {
            try
            {
                var user = await FirebaseAuth.GetAuth(App).GetUserAsync(uid);
                return user;
            }
            catch (Exception e)
            {
                //  Logger.Debug(e);
            }
            return null;
        }


        public static FirebaseAuth Instance { get { return FirebaseAuth.GetAuth(App); } }
    }
}
