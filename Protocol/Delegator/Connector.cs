using System;
using System.Collections.Generic;
using System.Text;
using dbms = Caspar.Database.Management;
using Caspar.Database;
using Caspar;
using System.Threading.Tasks;
using static Caspar.Extensions.Database;
using Newtonsoft.Json.Linq;

namespace Caspar.Protocol
{
    public partial class Delegator<D>
    {
        public class Connector : Caspar.Scheduler
        {
            public class Server
            {
                public string Type { get; set; }
                public string CloudPlatform { get; set; }



                public string PublicIp { get; set; }
                public string PrivateIp { get; set; }
                public DateTime HeartBeat { get; set; }
                public string Provider { get; set; } = string.Empty;
                public string Publish { get; set; } = string.Empty;
                public string Region { get; set; } = string.Empty;
                public double Latitude { get; set; }
                public double Longitude { get; set; }

                public bool Health { get; set; } = true;

                public long UID
                {
                    get
                    {
                        uint privateIp = Caspar.Api.IPAddressToUInt32(PrivateIp);
                        uint publicIp = Caspar.Api.IPAddressToUInt32(PublicIp);
                        return (long)publicIp << 32 | privateIp;
                    }
                }

                public string GetConnectionString()
                {

                    if (CloudPlatform.ToString() == (string)Caspar.Api.Config.CloudPlatform)
                    {
                        return PrivateIp;
                    }

                    return PublicIp;
                }
            }


            public bool Self { get; set; }

            public ushort Port { get; set; }
            public string RemoteType { get; set; }

            public static string DB { get; set; } = "Game";

            public async Task Execute()
            {
                try
                {

                    if (Caspar.Api.StandAlone == true)
                    {
                        return;
                    }

                    using var session = new Caspar.Database.Session();

                    JObject obj = global::Caspar.Api.Config.Databases.MySql;
                    dynamic db = obj.First;
                    DB = db.Name;

                    var connection = await session.GetConnection(DB);
                    var command = connection.CreateCommand();

                    // 자신을 등록하고.
                    command.Parameters.Clear();


                    //서버들을 받아온다.
                    command.Parameters.Clear();
                    command.CommandText = $"SELECT * FROM `caspar`.`delegator` WHERE provider = '{Caspar.Api.Config.Provider}' AND type = '{RemoteType}';";
                    session.ResultSet = (await command.ExecuteReaderAsync()).ToResultSet();


                    List<Server> servers = new List<Server>();


                    Caspar.Database.ResultSet resultSet = session.ResultSet;

                    foreach (dynamic row in resultSet[0])
                    {
                        Server server = new Server();

                        server.Provider = row.provider;
                        server.Publish = row.publish;
                        server.Region = row.region;

                        server.Type = row.type;
                        server.CloudPlatform = row.platform;

                        // state
                        if (row.state != 1) { server.Health = false; }

                        server.PublicIp = row.public_ip;
                        server.PrivateIp = row.private_ip;
                        server.HeartBeat = (DateTime)row.heartbeat;

                        if (server.HeartBeat < DateTime.UtcNow) { server.Health = false; }

                        server.Latitude = row.latitude;
                        server.Longitude = row.longitude;
                        servers.Add(server);
                    }

                    OnRun(servers);

                }
                catch (Exception e)
                {
                    Caspar.Api.Logger.Error(e);
                }
                finally
                {
                    Resume();
                }


            }
            protected override void OnSchedule()
            {
                Pause();
                _ = Execute();
            }

            public virtual void OnRun(List<Server> servers)
            {

                foreach (var item in servers)
                {
                    if (item.Health == true)
                    {
                        var delegator = Delegator<D>.Create(item.UID, Self);
                        if (delegator.IsClosed() == false) { continue; }
                        delegator.UID = Caspar.Api.Idx;
                        var ip = item.GetConnectionString();
                        delegator.Connect(ip, Port);
                    }
                    else
                    {
                        var delegator = Delegator<D>.Get(item.UID);
                        if (delegator == null) { continue; }
                        delegator.Close();
                    }
                }
            }
            public void Run()
            {
                _ = PostMessage(async () =>
                {

                    try
                    {
                        await Execute();
                    }
                    catch
                    {

                    }
                    finally
                    {
                        Run(10000);
                    }

                });
            }
        }

    }
}
