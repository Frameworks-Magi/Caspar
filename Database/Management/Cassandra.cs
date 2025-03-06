using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

//using Apache.Cassandra;
//using Thrift.Transport;
//using Thrift.Protocol;

//namespace Caspar.Database.Management.NoSql
//{
//  public sealed class Cassandra : Driver
//  {
//    public delegate int Procedure(global::Apache.Cassandra.Cassandra.Client cass, MemoryStream input, out MemoryStream output);

//    private Dictionary<string, Procedure> procedures = new Dictionary<string, Procedure>();

//    string ip = "127.0.0.1";
//    ushort port = 9160;

//    Cassandra(string ip, ushort port) {

//      this.ip = ip;
//      this.port = port;

//    }
//    public void Add(string name, Procedure procedure) {
//      procedures.Add(name, procedure);
//    }

//    protected override void Poll() {


//      TTransport transport = new TBufferedTransport(new TSocket(ip, 9160));
//      TProtocol Protocol = new TBinaryProtocol(transport);
//      Apache.Cassandra.Cassandra.Client client = new Apache.Cassandra.Cassandra.Client(Protocol);

//      while (IsOk()) {

//        if (!transport.IsOpen) {

//          try {
//            transport.Open();
//          } catch (Exception) {
//            global::System.Threading.Thread.Sleep(1000);
//            continue;
//          }

//          Tuple<string, MemoryStream, AsyncCallback> tuple;
//          while (Pop(out tuple) == true) {

//            Procedure procedure;
//            if (procedures.TryGetValue(tuple.Item1, out procedure) == true) {

//              int ret = 0;
//              MemoryStream output = null;
//              try {
//                  ret = procedure(client, tuple.Item2, out output);
//              } catch (Exception) {
//                ret = -1;
//              }

//              try {
//                tuple.Item3(ret, output);
//              } catch (Exception) {
//              }

//            }

//          }

//        }

//      }

//    }

//  }
//}
