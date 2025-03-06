using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NSec.Cryptography;


namespace Caspar.Platform
{
    public static partial class AWS
    {
        public static partial class SignatureV4
        {
            public const string DateStringFormat = "yyyyMMdd";
            public const string TERMINATOR = "aws4_request";
            public const string HMACSHA256 = "HMACSHA256";

            public static string AWSAccessKey { get; set; } = "AKIAUJQCP42YN5AYMJ3D";
            public static string AWSSecretKey { get; set; } = "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";
            public static string Region { get; set; } = "ap-northeast-2";

            public static string S3UploadCredentials(string region, string bucket, string filename)
            {

                var json = new
                {
                    endpoint_url = $"https://s3.{region}.amazonaws.com/{bucket}",
                    @params = s3Params(region, bucket, filename)

                };
                return JsonConvert.SerializeObject(json);
            }

            internal static string s3Params(string region, string bucket, string filename)
            {
                var credential = amzCredential(region);
                var policy = s3UploadPolicy(bucket, filename, credential);
                var policyBase64 = policy.ToBase64String();
                var signature = s3UploadSignature(AWSSecretKey, policyBase64, credential, region);

                var json = new JObject();
                json.Add("key", $"{filename}");
                json.Add("acl", "private");
                json.Add("success_action_status", "201");
                json.Add("policy", policyBase64);
                json.Add("x-amz-algorithm", "AWS4-HMAC-SHA256");
                json.Add("x-amz-credential", credential);
                json.Add("x-amz-date", dateString() + "T000000Z");
                json.Add("x-amz-signature", signature);

                return JsonConvert.SerializeObject(json);
            }

            internal static string s3UploadPolicy(string bucket, string filename, string credential)
            {
                string expires = DateTime.UtcNow.AddDays(1).ToISODateTime();

                JObject json = new JObject();
                json.Add("expiration", expires);

                var length = new JArray();
                length.Add("content-length-range");
                length.Add(0);
                length.Add(10485760);

                var start_with = new JArray();
                start_with.Add("starts-with");
                start_with.Add("image/jpg,image/jpeg,image/png,image/gif");
                start_with.Add("image/");

                json.Add("conditions", new JArray(
                    new JObject(new JProperty("bucket", bucket)),
                    new JObject(new JProperty("key", $"{filename}")),
                    new JObject(new JProperty("acl", "private")),
                    new JObject(new JProperty("success_action_status", "201")),
                    start_with,
                    length,
                    new JObject(new JProperty("x-amz-algorithm", "AWS4-HMAC-SHA256")),
                    new JObject(new JProperty("x-amz-credential", credential)),
                    new JObject(new JProperty("x-amz-date", dateString() + "T000000Z"))
                ));
                return JsonConvert.SerializeObject(json);
            }


            internal static string dateString()
            {
                return DateTime.UtcNow.ToString(DateStringFormat, CultureInfo.InvariantCulture);
            }
            internal static string amzCredential(string region, string service = "s3")
            {
                var dateStamp = DateTime.UtcNow.ToString(DateStringFormat, CultureInfo.InvariantCulture);
                var scope = string.Format("{0}/{1}/{2}/{3}/{4}",
                                          AWSAccessKey,
                                          dateStamp,
                                          region,
                                          service,
                                          TERMINATOR);

                return scope;
            }

            internal static byte[] hmac(string key, string data)
            {
                return hmac(Encoding.UTF8.GetBytes(key), data);
            }
            internal static byte[] hmac(byte[] key, string data)
            {
                var kha = KeyedHashAlgorithm.Create(HMACSHA256);
                kha.Key = key;
                return kha.ComputeHash(Encoding.UTF8.GetBytes(data));
            }

            internal static string ToHexString(byte[] data, bool lowercase)
            {
                var sb = new StringBuilder();
                for (var i = 0; i < data.Length; i++)
                {
                    sb.Append(data[i].ToString(lowercase ? "x2" : "X2"));
                }
                return sb.ToString();
            }
            internal static string s3UploadSignature(string secretKey, string policyBase64, string credential, string region)
            {
                var key = ("AWS4" + secretKey).ToCharArray();
                var dateKey = hmac(Encoding.UTF8.GetBytes(key), dateString());
                var dateRegionKey = hmac(dateKey, region);
                var dateRegionServiceKey = hmac(dateRegionKey, "s3");
                var signingKey = hmac(dateRegionServiceKey, "aws4_request");

                var signature = hmac(signingKey, policyBase64);
                return ToHexString(signature, true);
            }

        }
    }
}