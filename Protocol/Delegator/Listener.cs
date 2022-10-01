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
        public class Listener : Framework.Caspar.Scheduler
        {
            public string Type { get; } = typeof(D).FullName;
            public string DB { get; set; } = "Game";

            public async Task Execute()
            {
                try
                {
                    using var session = new Framework.Caspar.Database.Session();
                    var connection = await session.GetConnection(DB);
                    var command = connection.CreateCommand();
                    command.Parameters.Clear();
                    command.CommandText = string.Empty;

                    command.CommandText += $"INSERT INTO caspar.Delegator (Provider, Publish, Region, Type, Platform, State, PublicIp, PrivateIp, HeartBeat, Latitude, Longitude) ";
                    command.CommandText += $"VALUES (@provider, @Publish, @region, @type, @platform, @state, @publicip, @privateip, @heartbeat, @latitude, @longitude) ";
                    command.CommandText += $"ON DUPLICATE KEY ";
                    command.CommandText += $"UPDATE Platform = @platform, HeartBeat = @heartbeat, Latitude = @latitude, Longitude = @longitude;";

                    command.Parameters.AddWithValue("@provider", Framework.Caspar.Api.Config.Provider);
                    command.Parameters.AddWithValue("@publish", Framework.Caspar.Api.Config.Publish);
                    command.Parameters.AddWithValue("@region", Framework.Caspar.Api.Config.Region);
                    command.Parameters.AddWithValue("@type", Type);
                    command.Parameters.AddWithValue("@platform", Framework.Caspar.Api.Config.CloudPlatform.ToString());
                    command.Parameters.AddWithValue("@state", 1);
                    command.Parameters.AddWithValue("@publicip", Framework.Caspar.Api.PublicIp);
                    command.Parameters.AddWithValue("@privateip", Framework.Caspar.Api.PrivateIp);
                    command.Parameters.AddWithValue("@heartbeat", DateTime.UtcNow.AddMinutes(1));
                    command.Parameters.AddWithValue("@latitude", 0.0);
                    command.Parameters.AddWithValue("@longitude", 0.0);

                    await command.ExecuteNonQueryAsync();
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
