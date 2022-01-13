using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Framework.Caspar
{
    public static partial class Extension
    {
        public static void Swap<T>(this IList<T> list, int i, int j)
        {
            var temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }

        // RFC2822(https://regexr.com/2rhq7)
        private static System.Text.RegularExpressions.Regex emailRegex = new System.Text.RegularExpressions.Regex(@"^[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$");
        public static bool IsEmailAddress(this string value)
        {
            return emailRegex.IsMatch(value);
        }

        public static string Localize(this string value, (string, string) code)
        {
            return Api.Localization.Localize(code, value);
        }

        public static string Intern(this string value)
        {
            return string.Intern(value);
        }
        public static void Shuffle<T>(this IList<T> list)
        {
            for (var i = 0; i < list.Count; i++)
                list.Swap(i, global::Framework.Caspar.Dice.Throw(i, list.Count));
        }

        public static long ToByte(this string value)
        {
            byte parsed = 0;
            if (byte.TryParse(value, out parsed) == true) { return parsed; }
            return 0;
        }

        public static long ToSByte(this string value)
        {
            sbyte parsed = 0;
            if (sbyte.TryParse(value, out parsed) == true) { return parsed; }
            return 0;
        }

        public static short ToInt16(this string value)
        {
            short parsed = 0;
            if (short.TryParse(value, out parsed) == true) { return parsed; }
            return 0;
        }

        public static string ToCoupon(this string value)
        {
            if (value.Length != 20) { return $"{value[0..4]}-{value[4..]}"; }
            return $"{value[0..4]}-{value[4..8]}-{value[8..12]}-{value[12..16]}-{value[16..]}";
        }

        public static ushort ToUInt16(this string value)
        {
            ushort parsed = 0;
            if (ushort.TryParse(value, out parsed) == true) { return parsed; }
            return 0;
        }

        public static int ToInt32(this string value)
        {
            int parsed = 0;
            if (int.TryParse(value, out parsed) == true) { return parsed; }
            return 0;
        }

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

        public static string FromCouponPrefixCode(this int code)
        {
            string prefix = string.Empty;
            foreach (var e in BitConverter.GetBytes(code).Reverse())
            {
                prefix += Convert.ToChar(e);
            }
            return prefix;
        }

        public static int ToInt32(this object value)
        {
            return (int)value;
        }
        public static int ToInt32(this char value)
        {
            int parsed = 0;
            if (int.TryParse(new string(value, 1), out parsed) == true) { return parsed; }
            return 0;
        }
        public static short ToInt16(this object value)
        {
            return (short)value;
        }

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

        public static double ToDouble(this object value)
        {
            return (double)value;
        }

        public static long ToInt64(this object value)
        {
            return Convert.ToInt64(value);
        }

        public static int Code<T>(this T msg) where T : class
        {
            return global::Framework.Caspar.Id<T>.Value;
        }

        public static T ToProtobuf<T>(this string value) where T : global::Google.Protobuf.IMessage, new()
        {
            return global::Google.Protobuf.JsonParser.Default.Parse<T>(value);
        }

        public static string ToBase64UrlEncode(this string value)
        {
            return Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(value);
        }

        public static string ToBase64UrlEncode(this byte[] value)
        {
            return Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(value);
        }

        public static string ToHex(this byte[] array)
        {
            StringBuilder s = new StringBuilder(array.Length * 2);
            foreach (var e in array)
                s.Append(e.ToString("X2"));
            return s.ToString();
        }

        

        public static string FromBase64UrlDecode(this string value)
        {
            return Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Decode(value);
        }

        public static byte[] FromBase64UrlDecodeBytes(this string value)
        {
            return Microsoft.IdentityModel.Tokens.Base64UrlEncoder.DecodeBytes(value);
        }

        public static string ToBase64String(this byte[] value)
        {
            return Convert.ToBase64String(value);
        }

        public static string ToBase64String(this string value)
        {
            return value.ToBytes().ToBase64String();
        }

        public static byte[] FromBase64String(this string value)
        {
            return Convert.FromBase64String(value);
        }

        public static byte[] ToBytes(this string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }

        public static string ToString(this byte[] value)
        {
            return Encoding.UTF8.GetString(value);
        }


        public static string ToJson(this global::Google.Protobuf.IMessage value)
        {
            return global::Google.Protobuf.JsonFormatter.Default.Format(value);
        }

        public static global::Google.Protobuf.CodedInputStream ToCodedInputStream(this global::Google.Protobuf.IMessage msg)
        {
            var buf = new byte[msg.CalculateSize()];
            using (var os = new global::Google.Protobuf.CodedOutputStream(buf))
            {
                msg.WriteTo(os);
            }

            return new global::Google.Protobuf.CodedInputStream(buf);

        }

        public static DateTime ToKST(this DateTime value)
        {
            return value.ConvertTimeFromUtc("Korea Standard Time");
        }

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

        public static string ToUInt32IpAddress(this long value, bool @public = false)
        {
            if (@public == true)
            {
                return global::Framework.Caspar.Api.UInt32ToIPAddress((uint)value);
            }
            else
            {
                return global::Framework.Caspar.Api.UInt32ToIPAddress((uint)(value >> 32));
            }
        }

        public static string ToBase64String(this MemoryStream stream)
        {
            return stream.ToArray().ToBase64String();
        }


        public static T FromBase64String<T>(this T msg, string base64) where T : global::Google.Protobuf.IMessage<T>, new()
        {
            MemoryStream stream = new MemoryStream(base64.FromBase64String());
            msg.MergeFrom(stream);
            return msg;
        }

        public static MemoryStream Compress(this Stream stream)
        {
            return global::Framework.Caspar.Api.Compress(stream);
        }

        public static MemoryStream Decompress(this Stream stream)
        {
            return global::Framework.Caspar.Api.Decompress(stream);
        }

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

        public static void MergeFrom<T>(this T msg, Stream stream) where T : global::Google.Protobuf.IMessage<T>
        {
            msg.MergeFrom(new global::Google.Protobuf.CodedInputStream(stream));
        }

        public static void MergeFrom<T>(this T msg, global::Google.Protobuf.WellKnownTypes.Any any) where T : global::Google.Protobuf.IMessage<T>, new()
        {
            var unpacked = any.Unpack<T>();
            msg.MergeFrom(unpacked);
        }

        public static int Id<T>(this T msg) where T : global::Google.Protobuf.IMessage<T>
        {
            return global::Framework.Caspar.Id<T>.Value;
        }

        

        public static bool IsNullOrEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }

        public static global::Google.Protobuf.CodedOutputStream ToCodedOutputStream(this global::Google.Protobuf.IMessage msg)
        {
            return new global::Google.Protobuf.CodedOutputStream(new byte[msg.CalculateSize()]);
        }

        public static DateTime ConvertTimeFromUtc(this DateTime UtcNow, string TimeZone)
        {
            var timezone = TimeZoneConverter.TZConvert.GetTimeZoneInfo(TimeZone);
            DateTime cstTime = TimeZoneInfo.ConvertTimeFromUtc(UtcNow, timezone);
            return cstTime;
        }


        public static DateTime ConvertTimeFromUtc(this DateTime UtcNow)
        {
            string TimeZone = "";
            if (global::Framework.Caspar.Api.Config.ServerStandardTime == null)
            {
                TimeZone = "Korea Standard Time";
            }
            else
            {
                TimeZone = global::Framework.Caspar.Api.Config.ServerStandardTime;
            }

            var timezone = TimeZoneConverter.TZConvert.GetTimeZoneInfo(TimeZone);
            DateTime cstTime = TimeZoneInfo.ConvertTimeFromUtc(UtcNow, timezone);
            return cstTime;
        }

        public static DateTime ConvertTimeToUtc(this DateTime SST)
        {
            string TimeZone = "";
            if (global::Framework.Caspar.Api.Config.ServerStandardTime == null)
            {
                TimeZone = "Korea Standard Time";
            }
            else
            {
                TimeZone = global::Framework.Caspar.Api.Config.ServerStandardTime;
            }

            var timezone = TimeZoneConverter.TZConvert.GetTimeZoneInfo(TimeZone);
            DateTime cstTime = TimeZoneInfo.ConvertTimeToUtc(SST, timezone);
            return cstTime;
        }

        public static DateTime FromUnixTime(this long unixTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTime);
        }
        public static long ToUnixTime(this DateTime date)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64((date - epoch).TotalSeconds);
        }

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

        public static uint ToUInt32(this object value)
        {
            return (uint)value;
        }

        public static uint ToUInt32(this string value)
        {
            uint parsed = 0;
            if (uint.TryParse(value, out parsed) == true) { return parsed; }
            return 0;
        }

        public static long ToInt64(this string value)
        {
            long parsed = 0;
            if (long.TryParse(value, out parsed) == true) { return parsed; }
            return 0;
        }

        public static ulong ToUInt64(this string value)
        {
            ulong parsed = 0;
            if (ulong.TryParse(value, out parsed) == true) { return parsed; }
            return 0;
        }


        public static bool ToBoolean(this string value)
        {
            bool parsed = true;
            if (bool.TryParse(value, out parsed) == true) { return parsed; }
            return true;
        }

        public static float ToFloat(this string value)
        {
            float parsed = 0;
            if (float.TryParse(value, out parsed) == true) { return parsed; }
            return 0;
        }

        public static double ToDouble(this string value)
        {
            double parsed = 0;
            if (double.TryParse(value, out parsed) == true) { return parsed; }
            return 0;
        }

        public static DateTime LastWeek(this DateTime value)
        {
            return Api.DateHelper.GetFirstDateTimeOfWeek(value, Api.DateHelper.FirstDayOfWeek).AddDays(-7);
        }

        public static DateTime CurrentWeek(this DateTime value)
        {
            return Api.DateHelper.GetFirstDateTimeOfWeek(value, Api.DateHelper.FirstDayOfWeek);
        }

        public static DateTime NextWeek(this DateTime value, int count = 1)
        {
            return Api.DateHelper.GetFirstDateTimeOfWeek(value, Api.DateHelper.FirstDayOfWeek).AddDays(7 * count);
        }

        public static DateTime LastMonth(this DateTime value)
        {
            return Api.DateHelper.GetFirstDateTimeOfMonth(value).AddMonths(-1);
        }

        public static DateTime CurrentMonth(this DateTime value)
        {
            return Api.DateHelper.GetFirstDateTimeOfMonth(value);
        }

        public static DateTime NextMonth(this DateTime value, int count = 1)
        {
            return Api.DateHelper.GetFirstDateTimeOfMonth(value).AddMonths(count);
        }

        public static DateTime NextDay(this DateTime value, int count = 1)
        {
            return new DateTime(value.Year, value.Month, value.Day).AddDays(count);
        }

        public static string ToDynamoDB(this DateTime value)
        {
            return value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }
    }

}
