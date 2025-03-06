using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Caspar.Api;

namespace Caspar.Database.NoSql
{
    public class DynamoDB : IConnection
    {

        public class DateTimeUtcConverter : IPropertyConverter
        {
            public DynamoDBEntry ToEntry(object value) => ((DateTime)value).ToUniversalTime();

            public object FromEntry(DynamoDBEntry entry)
            {
                var dateTime = entry.AsDateTime();
                return dateTime.ToUniversalTime();
            }
        }

        public ThreadLocal<AmazonDynamoDBClient> Connection = new ThreadLocal<AmazonDynamoDBClient>();

        public string AwsAccessKeyId { get; set; } = "AKIAI5H23AJ26G2E2TJA";
        public string AwsSecretAccessKey { get; set; } = "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";
        public RegionEndpoint Endpoint { get; set; } = RegionEndpoint.APNortheast2;
        public string Name { get; set; }
        public void Initialize()
        {
            if (Connection.Value == null)
            {
                AmazonDynamoDBConfig clientConfig = new AmazonDynamoDBConfig();
                clientConfig.RegionEndpoint = Endpoint;
                Connection.Value = new AmazonDynamoDBClient(AwsAccessKeyId, AwsSecretAccessKey, clientConfig);
            }
        }

        public IConnection Create()
        {
            return this;
        }

        public AmazonDynamoDBClient GetClient()
        {

            if (Connection.Value == null)
            {
                AmazonDynamoDBConfig clientConfig = new AmazonDynamoDBConfig();
                clientConfig.RegionEndpoint = Endpoint;
                Connection.Value = new AmazonDynamoDBClient(AwsAccessKeyId, AwsSecretAccessKey, clientConfig);
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
    }
}
