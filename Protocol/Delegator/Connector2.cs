using System;
using System.Collections.Generic;
using System.Text;
using dbms = Caspar.Database.Management;
using Caspar.Database;
using Caspar;
using System.Threading.Tasks;
using static Caspar.Extensions.Database;

namespace Caspar
{
    public static partial class Api
    {
        public class Connector2 : Caspar.Scheduler
        {
            public class Server
            {
                public string Type { get; set; }
                public string CloudPlatform { get; set; }

                public System.Reflection.MethodInfo Method { get; set; }

                public bool Self { get; set; }

                public string PublicIp { get; set; }
                public string PrivateIp { get; set; }
                public DateTime HeartBeat { get; set; }
                public string Provider { get; set; } = string.Empty;
                public string Publish { get; set; } = string.Empty;
                public string Region { get; set; } = string.Empty;
                public double Latitude { get; set; }
                public double Longitude { get; set; }

                public long UID
                {
                    get
                    {
                        uint privateIp = Caspar.Api.IPAddressToUInt32(PrivateIp);
                        uint publicIp = Caspar.Api.IPAddressToUInt32(PublicIp);
                        return (long)publicIp << 32 | privateIp;
                    }
                }

                public List<Server> Bridges = new List<Server>();
                public List<Server> Gateways = new List<Server>();

                public string GetConnectionString()
                {

                    if (CloudPlatform.ToString() == (string)Caspar.Api.Config.CloudPlatform)
                    {
                        return PrivateIp;
                    }

                    return PublicIp;
                    //if (CloudPlatform == platform) { return PrivateIp; }

                    //else { return PublicIp; }

                    //if (Bridges.Count == 0 && Gateways.Count == 0)
                    //{
                    //    return PublicIp;
                    //}

                    //double distance = double.MaxValue;

                    //foreach (var e in Bridges)
                    //{
                    //    // 브릿지는 나와 같은 플랫폼을 찾는게 기본이며 실제 연결하려는 서버와 가장 가까운놈을 선택한다.
                    //    // 브릿지는 내부적으로 해당서버의 게이트웨이와 퍼블릭 연결이 되어있을 것이다.
                    //    if (e.CloudPlatform == platform && e.Publish == Publish)
                    //    {
                    //        return $"{e.PrivateIp},{PrivateIp}";
                    //    }
                    //}



                    // 브릿지가 없으므로 
                    // 최대한 나와 가까깝고 실제 연결하려는 서버와 동일한 플랫폼의 게이트웨이를 찾는다.
                    // 나와 게이트웨이는 퍼블릭. 게이트웨이와 실제서버는 프라이빗.
                    //string remoteIp = string.Empty;
                    //string gatewayIp = string.Empty;

                    //foreach (var e in Gateways)
                    //{

                    //    if (e.CloudPlatform != CloudPlatform) { continue; }
                    //    if (e.Provider == provider) { continue; }
                    //    if (e.Provider == provider)
                    //    {
                    //        if (e.CloudPlatform == platform)
                    //        {
                    //            gatewayIp = e.PrivateIp;
                    //        }
                    //        else
                    //        {
                    //            gatewayIp = e.PublicIp;
                    //        }


                    //        if (e.CloudPlatform == CloudPlatform)
                    //        {
                    //            remoteIp = PrivateIp;
                    //        }
                    //        else
                    //        {
                    //            remoteIp = PublicIp;
                    //        }

                    //        return $"{gatewayIp},{remoteIp}";
                    //    }
                    //}


                    // 나와 같은 플랫폼이지만 실제 연결하려는 서버와 최대한 가까운 서버를 찾는다. 나와는 최대한 멀리떨어졌을 것이다.
                    // 나와 게이트웨이는 프라이빗. 게이트웨이와 실제서버는 퍼블릭.
                    //remoteIp = string.Empty;
                    //gatewayIp = string.Empty;
                    //foreach (var e in Gateways)
                    //{
                    //    if (e.CloudPlatform != platform) { continue; }

                    //        if (e.CloudPlatform == platform)
                    //        {
                    //            gatewayIp = e.PrivateIp;
                    //        }
                    //        else
                    //        {
                    //            gatewayIp = e.PublicIp;
                    //        }


                    //        if (e.CloudPlatform == CloudPlatform)
                    //        {
                    //            remoteIp = PrivateIp;
                    //        }
                    //        else
                    //        {
                    //            remoteIp = PublicIp;
                    //        }

                    //        return $"{gatewayIp},{remoteIp}";

                    //}


                    // 아무것도 찾을수 없었다. 아무거나 일단 연결을 해본다.
                    //foreach (var e in Gateways)
                    //{
                    //    if (e.CloudPlatform == platform)
                    //    {
                    //        gatewayIp = e.PrivateIp;
                    //    }
                    //    else
                    //    {
                    //        gatewayIp = e.PublicIp;
                    //    }


                    //    if (e.CloudPlatform == CloudPlatform)
                    //    {
                    //        remoteIp = PrivateIp;
                    //    }
                    //    else
                    //    {
                    //        remoteIp = PublicIp;
                    //    }

                    //    return $"{gatewayIp},{remoteIp}";
                    //}

                    //return string.Empty;
                }
            }


            public string ServerType { get; set; } = string.Empty;
            public string RemoteType { get; set; }

            public bool Self { get; set; } = false;
            public bool Singleton { get; set; } = false;

            public Type DelegatableType { get; set; }


            public string DB { get; set; } = "Game";

            public async Task Execute()
            {
                try
                {

                    if (Caspar.Api.StandAlone == true)
                    {
                        List<Server> servers = new List<Server>();
                        Server server = new Server();
                        server.Provider = Caspar.Api.Config.Provider;
                        server.Publish = Caspar.Api.Config.Publish;
                        server.Region = Caspar.Api.Config.Region;

                        server.Type = RemoteType;
                        server.CloudPlatform = Caspar.Api.Config.CloudPlatform;

                        // state
                        server.PublicIp = Caspar.Api.PublicIp;
                        server.PrivateIp = Caspar.Api.PrivateIp;
                        server.HeartBeat = DateTime.UtcNow;

                        server.Latitude = 0;
                        server.Longitude = 0;
                        servers.Add(server);
                        OnRun(servers);
                        return;
                    }

                    using var session = new Caspar.Database.Session();


                    var connection = await session.CreateConnection(DB);
                    var command = connection.CreateCommand();
                    //Api.Region;
                    //Api.Country;
                    //Caspar.Api.Config.CloudPlatform;

                    // 자신을 등록하고.
                    command.Parameters.Clear();

                    //lock ()



                    if (ServerType == "StandAlone".Intern()) { return; }


                    command.Parameters.Clear();

                    if (ServerType != "None".Intern())
                    {
                        command.CommandText = string.Empty;

                        command.CommandText += $"INSERT INTO caspar.Server (Provider, Publish, Region, Type, Platform, State, PublicIp, PrivateIp, HeartBeat, Latitude, Longitude) ";
                        command.CommandText += $"VALUES (@provider, @Publish, @region, @type, @platform, @state, @publicip, @privateip, @heartbeat, @latitude, @longitude) ";
                        command.CommandText += $"ON DUPLICATE KEY ";
                        command.CommandText += $"UPDATE Platform = @platform, HeartBeat = @heartbeat, Latitude = @latitude, Longitude = @longitude;";

                        command.Parameters.AddWithValue("@provider", Caspar.Api.Config.Provider);
                        command.Parameters.AddWithValue("@publish", Caspar.Api.Config.Publish);
                        command.Parameters.AddWithValue("@region", Caspar.Api.Config.Region);
                        command.Parameters.AddWithValue("@type", ServerType.ToString());
                        command.Parameters.AddWithValue("@platform", Caspar.Api.Config.CloudPlatform.ToString());
                        command.Parameters.AddWithValue("@state", 1);
                        command.Parameters.AddWithValue("@publicip", Caspar.Api.PublicIp);
                        command.Parameters.AddWithValue("@privateip", Caspar.Api.PrivateIp);
                        command.Parameters.AddWithValue("@heartbeat", DateTime.UtcNow);
                        command.Parameters.AddWithValue("@latitude", 0.0);
                        command.Parameters.AddWithValue("@longitude", 0.0);

                        await command.ExecuteNonQueryAsync();
                    }


                    if (RemoteType == "None".Intern()) { return; }

                    //서버들을 받아온다.
                    command.Parameters.Clear();
                    command.CommandText = $"SELECT * FROM `caspar`.`Server` WHERE Provider = '{Caspar.Api.Config.Provider}' AND Type = '{RemoteType.ToString()}';";
                    // command.CommandText += $"SELECT * FROM `caspar`.`Server` WHERE Provider = '{Caspar.Api.Config.Provider}' AND Type = '{Schema.Protobuf.CSharp.Enums.EServer.Gateway.ToString()}';";
                    // command.CommandText += $"SELECT * FROM `caspar`.`Server` WHERE Provider = '{Caspar.Api.Config.Provider}' AND Type = '{Schema.Protobuf.CSharp.Enums.EServer.Bridge.ToString()}';";
                    session.ResultSet = (await command.ExecuteReaderAsync()).ToResultSet();



                    if (RemoteType == "None".Intern()) { return; }
                    {

                        List<Server> servers = new List<Server>();
                        List<Server> gateways = new List<Server>();
                        List<Server> bridges = new List<Server>();


                        Caspar.Database.ResultSet resultSet = session.ResultSet;

                        foreach (var row in resultSet[0])
                        {
                            Server server = new Server();
                            server.Gateways = gateways;
                            server.Bridges = bridges;

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
                            if (row[5].ToInt32() != 1) { continue; }



                            server.PublicIp = row[6].ToString();
                            server.PrivateIp = row[7].ToString();
                            server.HeartBeat = row[8].ToDateTime();

                            if (server.HeartBeat.AddSeconds(300) < DateTime.UtcNow) { continue; }

                            server.Latitude = row[9].ToDouble();
                            server.Longitude = row[10].ToDouble();
                            servers.Add(server);
                        }


                        foreach (var row in resultSet[1])
                        {
                            Server server = new Server();
                            //
                            server.Provider = row[0].ToString();
                            server.Publish = row[1].ToString();
                            server.Region = row[2].ToString();

                            server.Type = row[3].ToString();
                            server.CloudPlatform = row[4].ToString();

                            // state
                            if (row[5].ToInt32() != 1) { continue; }



                            server.PublicIp = row[6].ToString();
                            server.PrivateIp = row[7].ToString();
                            server.HeartBeat = row[8].ToDateTime();

                            if (server.HeartBeat.AddSeconds(300) < DateTime.UtcNow) { continue; }

                            server.Latitude = row[9].ToDouble();
                            server.Longitude = row[10].ToDouble();
                            gateways.Add(server);
                        }


                        OnRun(servers);

                    }
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
