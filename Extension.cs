using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caspar
{
    public static partial class Extension
    {
        public static string GetIp(this System.Net.EndPoint value)
        {
            try
            {
                return (value as System.Net.IPEndPoint).Address.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }
        public static long ToInt64Address(this System.Net.EndPoint value)
        {
            var ip = (value as System.Net.IPEndPoint).Address.ToString();
            var port = (value as System.Net.IPEndPoint).Port;
            return global::Caspar.Api.AddressToInt64(ip, (ushort)port);
        }
    }

}
