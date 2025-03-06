using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Caspar.Protocol
{
    public class Udp
    {
        protected UdpClient socket = null;
        public async Task Bind()
        {
            socket = new UdpClient(4081);

            var ret = await socket.ReceiveAsync();

            //ret.Buffer


        }
    }
}
