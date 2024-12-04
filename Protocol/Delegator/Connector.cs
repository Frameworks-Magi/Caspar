using System;
using System.Collections.Generic;
using System.Text;
using dbms = Framework.Caspar.Database.Management;
using Framework.Caspar.Database;
using Framework.Caspar;
using System.Threading.Tasks;
using static Framework.Caspar.Extensions.Database;
using Newtonsoft.Json.Linq;

namespace Framework.Caspar.Protocol
{
    public partial class Delegator<D>
    {
        public class Connector : Framework.Caspar.Scheduler
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
                        uint privateIp = Framework.Caspar.Api.IPAddressToUInt32(PrivateIp);
                        uint publicIp = Framework.Caspar.Api.IPAddressToUInt32(PublicIp);
                        return (long)publicIp << 32 | privateIp;
                    }
                }

                public string GetConnectionString()
                {

                    if (CloudPlatform.ToString() == (string)Framework.Caspar.Api.Config.CloudPlatform)
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

                    if (Framework.Caspar.Api.StandAlone == true)
                    {
                        return;
                    }

                    using var session = new Framework.Caspar.Database.Session();

                    JObject obj = global::Framework.Caspar.Api.Config.Databases.MySql;
                    dynamic db = obj.First;
                    DB = db.Name;

                    var connection = await session.GetConnection(DB);
                    var command = connection.CreateCommand();

                    // 자신을 등록하고.
                    command.Parameters.Clear();


                    //서버들을 받아온다.
                    command.Parameters.Clear();
                    command.CommandText = $"SELECT * FROM `caspar`.`delegator` WHERE provider = '{Framework.Caspar.Api.Config.Provider}' AND type = '{RemoteType}';";
                    session.ResultSet = (await command.ExecuteReaderAsync()).ToResultSet();


                    List<Server> servers = new List<Server>();


                    Framework.Caspar.Database.ResultSet resultSet = session.ResultSet;

                    foreach (var row in resultSet[0])
                    {
                        Server server = new Server();

                        server.Provider = row[0].ToString();
                        server.Publish = row[1].ToString();
                        server.Region = row[2].ToString();

                        server.Type = row[3].ToString();
                        server.CloudPlatform = row[4].ToString();

                        // state
                        if (row[5].ToInt32() != 1) { server.Health = false; }

                        server.PublicIp = row[6].ToString();
                        server.PrivateIp = row[7].ToString();
                        server.HeartBeat = row[8].ToDateTime();

                        if (server.HeartBeat < DateTime.UtcNow) { server.Health = false; }

                        server.Latitude = row[9].ToDouble();
                        server.Longitude = row[10].ToDouble();
                        servers.Add(server);
                    }

                    OnRun(servers);

                }
                catch (Exception e)
                {
                    Framework.Caspar.Api.Logger.Error(e);
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
                        delegator.UID = Framework.Caspar.Api.Idx;
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
