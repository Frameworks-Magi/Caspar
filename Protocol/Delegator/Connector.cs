using System;
using System.Collections.Generic;
using System.Text;
using dbms = Framework.Caspar.Database.Management;
using Framework.Caspar.Database;
using Framework.Caspar;
using System.Threading.Tasks;
using static Framework.Caspar.Extensions.Database;

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

                // public List<Server> Bridges = new List<Server>();
                // public List<Server> Gateways = new List<Server>();

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
                        // List<Server> servers = new List<Server>();
                        // Server server = new Server();
                        // server.Provider = Framework.Caspar.Api.Config.Provider;
                        // server.Publish = Framework.Caspar.Api.Config.Publish;
                        // server.Region = Framework.Caspar.Api.Config.Region;

                        // server.Type = RemoteType;
                        // server.CloudPlatform = Framework.Caspar.Api.Config.CloudPlatform;

                        // // state
                        // server.PublicIp = Framework.Caspar.Api.PublicIp;
                        // server.PrivateIp = Framework.Caspar.Api.PrivateIp;
                        // server.HeartBeat = DateTime.UtcNow;

                        // server.Latitude = 0;
                        // server.Longitude = 0;
                        // servers.Add(server);
                        // OnRun(servers);
                        return;
                    }

                    using var session = new Framework.Caspar.Database.Session();


                    var connection = await session.GetConnection(DB);
                    var command = connection.CreateCommand();
                    //Api.Region;
                    //Api.Country;
                    //Framework.Caspar.Api.Config.CloudPlatform;

                    // 자신을 등록하고.
                    command.Parameters.Clear();

                    //lock ()

                    // if (ServerType == "StandAlone".Intern()) { return; }
                    // if (RemoteType == "None".Intern()) { return; }

                    //서버들을 받아온다.
                    command.Parameters.Clear();
                    command.CommandText = $"SELECT * FROM `caspar`.`Delegator` WHERE Provider = '{Framework.Caspar.Api.Config.Provider}' AND Type = '{RemoteType.ToString()}';";
                    session.ResultSet = (await command.ExecuteReaderAsync()).ToResultSet();


                    List<Server> servers = new List<Server>();
                    // List<Server> gateways = new List<Server>();
                    // List<Server> bridges = new List<Server>();


                    Framework.Caspar.Database.ResultSet resultSet = session.ResultSet;

                    foreach (var row in resultSet[0])
                    {
                        Server server = new Server();
                        // server.Gateways = gateways;
                        // server.Bridges = bridges;

                        //
                        /*
                         * CREATE TABLE `Server` (
                              `Provider` varchar(25) NOT NULL, 0
                              `Publish` varchar(25) NOT NULL, 1
                              `Region` varchar(25) NOT NULL, 2
                              `Type` varchar(25) NOT NULL, 3
                              `Platform` int(11) NOT NULL, 4
                              `State` int(11) NOT NULL, 5
                              `PublicIp` bigint(20) NOT NULL, 6
                              `PrivateIp` bigint(20) NOT NULL, 7
                              `HeartBeat` datetime(6) NOT NULL, 8
                              `Latitude` double NOT NULL, 9
                              `Longitude` double NOT NULL, 10
                              PRIMARY KEY (`Provider`,`Publish`,`Type`,`PrivateIp`,`Region`,`PublicIp`)
                            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

                         */
                        //

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

                        if (server.HeartBeat.AddSeconds(300) < DateTime.UtcNow) { server.Health = false; }

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
                        //     var delegator = Create.Invoke(null, new object[] { item.UID }) as Protocol.IDelegator;
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
