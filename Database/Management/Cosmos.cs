using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Framework.Caspar.Database.NoSql
{
    public class Cosmos : IConnection
    {

        public ThreadLocal<CosmosClient> Connection = new ThreadLocal<CosmosClient>();

        public string EndPoint { get; set; } = "";
        public string Name { get; internal set; }

        public void Initialize()
        {

            if (Connection.Value == null)
            {
                Connection.Value = new CosmosClient(EndPoint);
            }

        }

        public IConnection Create()
        {
            return this;
        }


        public CosmosClient GetCosmosClient()
        {
            if (Connection.Value == null)
            {
                Connection.Value = new CosmosClient(EndPoint);
            }

            return Connection.Value;
        }

        public void BeginTransaction() { }
        public void Commit() { }
        public async Task CommitAsync() { await Task.CompletedTask; }
        public void Rollback() { }
        public async Task RollbackAsync() { await Task.CompletedTask; }
        public async Task<IConnection> Open(CancellationToken token = default, bool transaction = true)
        {
            await System.Threading.Tasks.Task.CompletedTask;
            return null;
        }
        public void Close() { }
        public void CopyFrom(IConnection value) { }

        public void Dispose()
        {

        }

        //protected string Database { get; private set; }
        //protected string Container { get; private set; }
        //protected string PartitionKey { get; private set; }
        //public virtual bool Initialize(T data)
        //{
        //    Data = data;
        //    return true;
        //}
        //public async virtual Task<bool> Initialize(string id)
        //{
        //    CosmosClient cosmosClient = new CosmosClient(
        //        ,
        //        new CosmosClientOptions()
        //        {
        //            ApplicationRegion = Regions.KoreaCentral,
        //            ConnectionMode = ConnectionMode.Direct,
        //        });


        //    var container = cosmosClient.GetContainer(Database, Container);
        //    QueryDefinition sqlQuery = new QueryDefinition("select * from root r where r.id = '1'");


        //    var itr = container.GetItemQueryIterator<T>(sqlQuery);
        //    while (itr.HasMoreResults)
        //    {
        //        foreach (var e in await itr.ReadNextAsync())
        //        {
        //            Data = e;
        //        }
        //    }
        //    return true;
        //}
        //public async virtual Task<bool> Commit()
        //{
        //    CosmosClient cosmosClient = new CosmosClient(
        //       "AccountEndpoint=https://retiad.documents.azure.com:443/;AccountKey=BpLGtSePPsWPlgTMsFx4r6kXzdPEC05Cy4KV5rfZgRKcPIZWK0UYqvJTRa3P91l5AoljVesJ1EeJeFHdppO1hg==;",
        //       new CosmosClientOptions()
        //       {
        //           ApplicationRegion = Regions.KoreaCentral,
        //           ConnectionMode = ConnectionMode.Direct,
        //       });


        //    var container = cosmosClient.GetContainer(Database, Container);

        //    string etag = ((dynamic)Data)._etag;
        //    Data = await container.UpsertItemAsync(Data, requestOptions: new ItemRequestOptions() { IfMatchEtag = etag });
        //    return true;
        //}
        //public async virtual Task<bool> Rollback()
        //{
        //    CosmosClient cosmosClient = new CosmosClient(
        //        "AccountEndpoint=https://retiad.documents.azure.com:443/;AccountKey=BpLGtSePPsWPlgTMsFx4r6kXzdPEC05Cy4KV5rfZgRKcPIZWK0UYqvJTRa3P91l5AoljVesJ1EeJeFHdppO1hg==;",
        //        new CosmosClientOptions()
        //        {
        //            ApplicationRegion = Regions.KoreaCentral,
        //            ConnectionMode = ConnectionMode.Direct,
        //        });


        //    var container = cosmosClient.GetContainer(Database, Container);
        //    QueryDefinition sqlQuery = new QueryDefinition("select * from root r where r.id = '1'");


        //    var itr = container.GetItemQueryIterator<T>(sqlQuery);
        //    while (itr.HasMoreResults)
        //    {
        //        foreach (var e in await itr.ReadNextAsync())
        //        {
        //            Data = e;
        //        }
        //    }
        //    return true;
        //}

        //protected T Origin;
        //public T Data;
    }
}
