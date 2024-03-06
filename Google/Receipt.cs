using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Framework.Caspar.Google
{
    public class Receipt
    {

        public static void Initialize(string MY_BASE64_PUBLIC_KEY)
        {
            cryptoServiceProviderXml = PEMKeyLoader.CryptoServiceProviderFromPublicKeyInfo(MY_BASE64_PUBLIC_KEY).ToXmlString(false);
            if(null == cryptoServiceProviderXml) throw new Exception("Failed : Framework.Caspar.Google.Initialize");
        }

        public static string cryptoServiceProviderXml = null;

        public class Result
        {
            public string orderId = String.Empty;
            public string packageName = String.Empty;
            public string productId = String.Empty;
            public string purchaseTime = String.Empty;
            public string purchaseState = String.Empty;
            public string developerPayload = String.Empty;
            public string purchaseToken = String.Empty;
        }

        public static Result GetPurchaseResult(string message)
        {
            return JsonConvert.DeserializeObject<Framework.Caspar.Google.Receipt.Result>(message);
        }

        public static Result GetPurchaseResult(byte[] message)
        {
            var data = Encoding.UTF8.GetString(message);
            return GetPurchaseResult(data);
        }

        public static bool Verify(string message, string base64Signature)
        {

            try
            {
                RSACryptoServiceProvider provider = new RSACryptoServiceProvider();
                provider.FromXmlString(cryptoServiceProviderXml);

                byte[] signature = Convert.FromBase64String(base64Signature);
                var sha = SHA1.Create();
                byte[] data = Encoding.UTF8.GetBytes(message);

                bool result = provider.VerifyData(data, sha, signature);
                return result;
            }
            catch (Exception)
            {
                return false;
            }
        }


        public static Result VerifyFromUnreal(string validation)
        {
            dynamic info = JsonConvert.DeserializeObject(validation);

            try
            {
                RSACryptoServiceProvider provider = new RSACryptoServiceProvider();
                provider.FromXmlString(cryptoServiceProviderXml);

                byte[] signature = Convert.FromBase64String((string)info.signature);
                var sha = SHA1.Create();
                byte[] data = ((string)info.receiptData).FromBase64ToBytes();
                if (provider.VerifyData(data, sha, signature) == false)
                {
                    return null;
                }

                var result = Encoding.UTF8.GetString(data);
                return JsonConvert.DeserializeObject<Result>(result);
            }
            catch (Exception)
            {
                return null;
            }
        }


    }
}
