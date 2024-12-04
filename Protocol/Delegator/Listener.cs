using System;
using System.Collections.Generic;
using System.Text;
using dbms = Framework.Caspar.Database.Management;
using Framework.Caspar.Database;
using Framework.Caspar;
using System.Threading.Tasks;
using static Framework.Caspar.Extensions.Database;
using Newtonsoft.Json.Linq;
using static Framework.Caspar.Api;

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
                    JObject obj = global::Framework.Caspar.Api.Config.Databases.MySql;
                    dynamic db = obj.First;
                    DB = db.Name;

                    using var session = new Framework.Caspar.Database.Session();
                    var connection = await session.GetConnection(DB);
                    var command = connection.CreateCommand();
                    command.Parameters.Clear();
                    command.CommandText = string.Empty;

                    command.CommandText += $"INSERT INTO caspar.delegator (provider, publish, region, type, platform, state, public_ip, private_ip, heartbeat, latitude, longitude) ";
                    command.CommandText += $"VALUES (@provider, @Publish, @region, @type, @platform, @state, @public_ip, @private_ip, @heartbeat, @latitude, @longitude) ";
                    command.CommandText += $"ON DUPLICATE KEY ";
                    command.CommandText += $"UPDATE platform = @platform, heartbeat = @heartbeat, latitude = @latitude, longitude = @longitude;";

                    command.Parameters.AddWithValue("@provider", (string)Framework.Caspar.Api.Config.Provider);
                    command.Parameters.AddWithValue("@publish", (string)Framework.Caspar.Api.Config.Publish);
                    command.Parameters.AddWithValue("@region", (string)Framework.Caspar.Api.Config.Region);
                    command.Parameters.AddWithValue("@type", Type);
                    command.Parameters.AddWithValue("@platform", Framework.Caspar.Api.Config.CloudPlatform.ToString());
                    command.Parameters.AddWithValue("@state", 1);
                    command.Parameters.AddWithValue("@public_ip", Framework.Caspar.Api.PublicIp);
                    command.Parameters.AddWithValue("@private_ip", Framework.Caspar.Api.PrivateIp);
                    command.Parameters.AddWithValue("@heartbeat", DateTime.UtcNow.AddMinutes(1));
                    command.Parameters.AddWithValue("@latitude", 0.0);
                    command.Parameters.AddWithValue("@longitude", 0.0);

                    await command.ExecuteNonQueryAsync();
                    session.Commit();
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
