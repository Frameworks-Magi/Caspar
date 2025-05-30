using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Caspar
{
    public static partial class Extension
    {
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static void Swap<T>(this IList<T> list, int i, int j)
        {
            var temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }

        // RFC2822(https://regexr.com/2rhq7)
        private static System.Text.RegularExpressions.Regex emailRegex = new System.Text.RegularExpressions.Regex(@"^[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$");
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static bool IsEmailAddress(this string value)
        {
            return emailRegex.IsMatch(value);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static string Localize(this string value, (string, string) code)
        {
            return Api.Localization.Localize(code, value);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static string Intern(this string value)
        {
            return string.Intern(value);
        }
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static void Shuffle<T>(this IList<T> list)
        {
            for (var i = 0; i < list.Count; i++)
                list.Swap(i, global::Caspar.Dice.Roll(i, list.Count));
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static long ToByte(this string value)
        {
            byte parsed = 0;
            if (byte.TryParse(value, out parsed) == true) { return parsed; }
            return 0;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static long ToSByte(this string value)
        {
            sbyte parsed = 0;
            if (sbyte.TryParse(value, out parsed) == true) { return parsed; }
            return 0;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static short ToInt16(this string value)
        {
            short parsed = 0;
            if (short.TryParse(value, out parsed) == true) { return parsed; }
            return 0;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static string ToCoupon(this string value)
        {
            if (value.Length != 20) { return $"{value[0..4]}-{value[4..]}"; }
            return $"{value[0..4]}-{value[4..8]}-{value[8..12]}-{value[12..16]}-{value[16..]}";
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static ushort ToUInt16(this string value)
        {
            ushort parsed = 0;
            if (ushort.TryParse(value, out parsed) == true) { return parsed; }
            return 0;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static int ToInt32(this string value)
        {
            int parsed = 0;
            if (int.TryParse(value, out parsed) == true) { return parsed; }
            return 0;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static BigInteger ToWei(this decimal value, Nethereum.Util.UnitConversion.EthUnit fromUnit = Nethereum.Util.UnitConversion.EthUnit.Ether)
        {
            return Nethereum.Util.UnitConversion.Convert.ToWei(value, fromUnit);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static BigInteger ToWei(this string value, Nethereum.Util.UnitConversion.EthUnit fromUnit = Nethereum.Util.UnitConversion.EthUnit.Ether)
        {
            return Nethereum.Util.UnitConversion.Convert.ToWei(value, fromUnit);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static BigInteger ToInt256(this string value)
        {
            return BigInteger.Parse(value);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static int ToCouponPrefixCode(this string value)
        {
            int parsed = 0;
            foreach (var e in value)
            {
                parsed = parsed << 8;
                parsed |= e;
            }

            return parsed;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static string FromCouponPrefixCode(this int code)
        {
            string prefix = string.Empty;
            foreach (var e in BitConverter.GetBytes(code).Reverse())
            {
                prefix += Convert.ToChar(e);
            }
            return prefix;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static int ToInt32(this object value)
        {
            return (int)value;
        }
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static int ToInt32(this char value)
        {
            int parsed = 0;
            if (int.TryParse(new string(value, 1), out parsed) == true) { return parsed; }
            return 0;
        }
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static short ToInt16(this object value)
        {
            return (short)value;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static T ToEnum<T>(this object value)
        {
            try
            {
                return (T)Enum.Parse(typeof(T), value as string, true);
            }
            catch
            {
                return default(T);
            }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static double ToDouble(this object value)
        {
            return (double)value;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static long ToInt64(this object value)
        {
            return Convert.ToInt64(value);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static decimal ToDecimal(this object value)
        {
            return Convert.ToDecimal(value);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static int Code<T>(this T msg) where T : class
        {
            return global::Caspar.Id<T>.Value;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static T ToProtobuf<T>(this string value) where T : global::Google.Protobuf.IMessage, new()
        {
            return global::Google.Protobuf.JsonParser.Default.Parse<T>(value);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static string ToBase64UrlEncode(this string value)
        {
            return Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(value);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static string ToBase64UrlEncode(this byte[] value)
        {
            return Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(value);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static string ToHex(this byte[] array)
        {
            StringBuilder s = new StringBuilder(array.Length * 2);
            foreach (var e in array)
                s.Append(e.ToString("X2"));
            return s.ToString();
        }



        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static string FromBase64UrlDecode(this string value)
        {
            return Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Decode(value);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static byte[] FromBase64UrlDecodeBytes(this string value)
        {
            return Microsoft.IdentityModel.Tokens.Base64UrlEncoder.DecodeBytes(value);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static string ToBase64String(this byte[] value)
        {
            return Convert.ToBase64String(value);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static string ToBase64String(this string value)
        {
            return value.ToBytes().ToBase64String();
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static byte[] FromBase64ToBytes(this string value)
        {
            return Convert.FromBase64String(value);
        }
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static string FromBase64ToString(this string value)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static byte[] ToBytes(this string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static string ToString(this byte[] value)
        {
            return Encoding.UTF8.GetString(value);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static string ToJson(this global::Google.Protobuf.IMessage value)
        {
            return Caspar.Api.JsonFormatter.Format(value);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static string ToJson(this object value)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(value);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static global::Google.Protobuf.CodedInputStream ToCodedInputStream(this global::Google.Protobuf.IMessage msg)
        {
            var buf = new byte[msg.CalculateSize()];
            using (var os = new global::Google.Protobuf.CodedOutputStream(buf))
            {
                msg.WriteTo(os);
            }

            return new global::Google.Protobuf.CodedInputStream(buf);

        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static DateTime ToKST(this DateTime value)
        {
            return value.ConvertTimeFromUtc("Korea Standard Time");
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static void Serialize(this global::Google.Protobuf.IMessage msg, Stream to, bool leaveOpen)
        {
            lock (msg)
            {
                using (var co = new global::Google.Protobuf.CodedOutputStream(to, true))
                {
                    msg.WriteTo(co);
                }
            }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static string ToBase64String(this global::Google.Protobuf.IMessage msg)
        {
            int size = msg.CalculateSize();
            MemoryStream stream = new MemoryStream(size);
            using (var co = new global::Google.Protobuf.CodedOutputStream(stream, true))
            {
                msg.WriteTo(co);
            }
            stream.Seek(0, SeekOrigin.Begin);
            return stream.ToBase64String();
        }

        public static Nethereum.Signer.EthereumMessageSigner signer { get; } = new();
        public static string EncodeUTF8AndEcRecover(this string msg, string signature)
        {
            try
            {
                return signer.EncodeUTF8AndEcRecover(msg, signature);
            }
            catch
            {
                return "0x0000000000000000000000000000000000000000";
            }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static string EncodeUTF8AndSign(this string msg, string privateKey)
        {
            try
            {
                return signer.EncodeUTF8AndSign(msg, new Nethereum.Signer.EthECKey(privateKey));
            }
            catch
            {
                return "";
            }
        }
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static string EncodeUTF8AndSign(this string msg, Nethereum.Signer.EthECKey key)
        {
            try
            {
                return signer.EncodeUTF8AndSign(msg, key);
            }
            catch
            {
                return "";
            }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static string ToUInt32IpAddress(this long value, bool @public = false)
        {
            if (@public == true)
            {
                return global::Caspar.Api.UInt32ToIPAddress((uint)value);
            }
            else
            {
                return global::Caspar.Api.UInt32ToIPAddress((uint)(value >> 32));
            }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static string ToBase64String(this MemoryStream stream)
        {
            return stream.ToArray().ToBase64String();
        }


        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static T FromBase64String<T>(this T msg, string base64) where T : global::Google.Protobuf.IMessage<T>, new()
        {
            MemoryStream stream = new MemoryStream(base64.FromBase64ToBytes());
            msg.MergeFrom(stream);
            return msg;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static MemoryStream Compress(this Stream stream)
        {
            return global::Caspar.Api.Compress(stream);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static MemoryStream Decompress(this Stream stream)
        {
            return global::Caspar.Api.Decompress(stream);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static MemoryStream ToMemoryStream(this global::Google.Protobuf.IMessage msg)
        {
            int size = msg.CalculateSize();
            MemoryStream stream = new MemoryStream(size);
            using (var co = new global::Google.Protobuf.CodedOutputStream(stream, true))
            {
                msg.WriteTo(co);
            }
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        //public static void MergeFrom<T>(this T msg, MemoryStream stream) where T : global::Google.Protobuf.IMessage<T>
        //{
        //    msg.MergeFrom(new global::Google.Protobuf.CodedInputStream(stream.GetBuffer(), 0, (int)stream.Length));
        //}

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static void MergeFrom<T>(this T msg, Stream stream) where T : global::Google.Protobuf.IMessage<T>
        {
            msg.MergeFrom(new global::Google.Protobuf.CodedInputStream(stream));
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static void MergeFrom<T>(this T msg, global::Google.Protobuf.WellKnownTypes.Any any) where T : global::Google.Protobuf.IMessage<T>, new()
        {
            var unpacked = any.Unpack<T>();
            msg.MergeFrom(unpacked);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static int Id<T>(this T msg) where T : global::Google.Protobuf.IMessage<T>
        {
            return global::Caspar.Id<T>.Value;
        }



        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static bool IsNullOrEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static bool IsNullOrDeadAddress(this string str)
        {
            if (str == "0x0000000000000000000000000000000000000000" || str == "0x000000000000000000000000000000000000dEaD" || str.ToLower() == "0x000000000000000000000000000000000000dead")
            {
                return true;
            }
            return false;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static bool IsNullAddress(this string str)
        {
            if (str == "0x0000000000000000000000000000000000000000")
            {
                return true;
            }
            return false;
        }
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static bool IsDeadAddress(this string str)
        {
            if (str == "0x000000000000000000000000000000000000dEaD" || str.ToLower() == "0x000000000000000000000000000000000000dead")
            {
                return true;
            }
            return false;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static global::Google.Protobuf.CodedOutputStream ToCodedOutputStream(this global::Google.Protobuf.IMessage msg)
        {
            return new global::Google.Protobuf.CodedOutputStream(new byte[msg.CalculateSize()]);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static DateTime ConvertTimeFromUtc(this DateTime UtcNow, string TimeZone)
        {
            var timezone = TimeZoneConverter.TZConvert.GetTimeZoneInfo(TimeZone);
            DateTime cstTime = TimeZoneInfo.ConvertTimeFromUtc(UtcNow, timezone);
            return cstTime;
        }


        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static DateTime ConvertTimeFromUtc(this DateTime UtcNow)
        {
            string TimeZone = "";
            if (global::Caspar.Api.Config.ServerStandardTime == null)
            {
                TimeZone = "Korea Standard Time";
            }
            else
            {
                TimeZone = global::Caspar.Api.Config.ServerStandardTime;
            }

            var timezone = TimeZoneConverter.TZConvert.GetTimeZoneInfo(TimeZone);
            DateTime cstTime = TimeZoneInfo.ConvertTimeFromUtc(UtcNow, timezone);
            return cstTime;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static DateTime ConvertTimeToUtc(this DateTime SST)
        {
            string TimeZone = "";
            if (global::Caspar.Api.Config.ServerStandardTime == null)
            {
                TimeZone = "Korea Standard Time";
            }
            else
            {
                TimeZone = global::Caspar.Api.Config.ServerStandardTime;
            }

            var timezone = TimeZoneConverter.TZConvert.GetTimeZoneInfo(TimeZone);
            DateTime cstTime = TimeZoneInfo.ConvertTimeToUtc(SST, timezone);
            return cstTime;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static DateTime FromUnixTime(this long unixTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTime);
        }
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static long ToUnixTime(this DateTime date)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64((date - epoch).TotalSeconds);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static long ToUnixTime(this object value)
        {
            try
            {
                if (value is DateTime)
                {
                    return ((DateTime)value).ToUnixTime();
                }
                if (value is global::MySql.Data.Types.MySqlDateTime)
                {
                    return ((global::MySql.Data.Types.MySqlDateTime)value).GetDateTime().ToUnixTime();
                }
                if (value is MySqlConnector.MySqlDateTime)
                {
                    return ((MySqlConnector.MySqlDateTime)value).GetDateTime().ToUnixTime();
                }
            }
            catch
            {
            }
            return 0;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static DateTime ToDateTime(this object value)
        {
            try
            {
                if (value is DateTime)
                {
                    return (DateTime)value;
                }

                if (value is global::MySql.Data.Types.MySqlDateTime)
                {
                    return ((global::MySql.Data.Types.MySqlDateTime)value).GetDateTime();
                }
                if (value is MySqlConnector.MySqlDateTime)
                {
                    return ((MySqlConnector.MySqlDateTime)value).GetDateTime();
                }

                if (value is string)
                {
                    return DateTime.Parse((string)value);
                }

                if (value is long)
                {
                    return new DateTime((long)value);
                }
            }
            catch
            {
                return new DateTime(0);
            }
            return new DateTime(0);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static uint ToUInt32(this object value)
        {
            return (uint)value;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static uint ToUInt32(this string value)
        {
            uint parsed = 0;
            if (uint.TryParse(value, out parsed) == true) { return parsed; }
            return 0;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static long ToInt64(this string value)
        {
            long parsed = 0;
            if (long.TryParse(value, out parsed) == true) { return parsed; }
            return 0;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static ulong ToUInt64(this string value)
        {
            ulong parsed = 0;
            if (ulong.TryParse(value, out parsed) == true) { return parsed; }
            return 0;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static bool ToBoolean(this string value)
        {
            bool parsed = true;
            if (bool.TryParse(value, out parsed) == true) { return parsed; }
            return false;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static float ToFloat(this string value)
        {
            float parsed = 0;
            if (float.TryParse(value, out parsed) == true) { return parsed; }
            return 0;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static double ToDouble(this string value)
        {
            double parsed = 0;
            if (double.TryParse(value, out parsed) == true) { return parsed; }
            return 0;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static DateTime LastWeek(this DateTime value)
        {
            return Api.DateHelper.GetFirstDateTimeOfWeek(value, Api.DateHelper.FirstDayOfWeek).AddDays(-7);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static DateTime CurrentWeek(this DateTime value)
        {
            return Api.DateHelper.GetFirstDateTimeOfWeek(value, Api.DateHelper.FirstDayOfWeek);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static DateTime NextWeek(this DateTime value, int count = 1)
        {
            return Api.DateHelper.GetFirstDateTimeOfWeek(value, Api.DateHelper.FirstDayOfWeek).AddDays(7 * count);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static DateTime LastMonth(this DateTime value)
        {
            return Api.DateHelper.GetFirstDateTimeOfMonth(value).AddMonths(-1);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static DateTime CurrentMonth(this DateTime value)
        {
            return Api.DateHelper.GetFirstDateTimeOfMonth(value);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static DateTime NextMonth(this DateTime value, int count = 1)
        {
            return Api.DateHelper.GetFirstDateTimeOfMonth(value).AddMonths(count);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static DateTime NextDay(this DateTime value, int count = 1)
        {
            return new DateTime(value.Year, value.Month, value.Day).AddDays(count);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static string ToDynamoDB(this DateTime value)
        {
            return value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static string ToISODateTime(this DateTime value)
        {
            return value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }
    }

}
