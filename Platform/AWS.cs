using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Framework.Caspar.Container;
using Org.BouncyCastle.Ocsp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Framework.Caspar.Api;

namespace Framework.Caspar.Platform
{
    public static class AWS
    {
        public class S3
        {
            public string KeyId { get; set; }
            public string SecretAccessKey { get; set; }
            public RegionEndpoint Endpoint { get; set; }
            public string Domain { get; set; }

            private static ConcurrentDictionary<string, S3> s3s = new();

            public static void Add(string name, S3 s3)
            {

                s3.S3Client = new AmazonS3Client(s3.KeyId, s3.SecretAccessKey, s3.Endpoint);
                s3s.AddOrUpdate(name, s3);
            }

            public static S3 Get(string name)
            {
                return s3s.Get(name);
            }

            public IAmazonS3 S3Client { get; set; }
            //{ get { return new AmazonS3Client(global::Framework.Caspar.CDN.Amazon.KeyId, global::Framework.Caspar.CDN.Amazon.SecretAccessKey, RegionEndpoint.APNortheast2); } }

            public async Task Upload(string key, Stream stream)
            {
                await S3Client.UploadObjectFromStreamAsync(Domain, key, stream, new Dictionary<string, object>());
                return;
            }


            public async Task<Stream> Download(string key)
            {
                using var obj = await S3Client.GetObjectAsync(Domain, key);
                var stream = new MemoryStream();
                obj.ResponseStream.CopyTo(stream);
                stream.Seek(0, SeekOrigin.Begin);
                return stream;
            }

            public async Task<Stream> Download(string domain, string key)
            {
                using var obj = await S3Client.GetObjectAsync(domain, key);
                var stream = new MemoryStream();
                obj.ResponseStream.CopyTo(stream);
                stream.Seek(0, SeekOrigin.Begin);
                return stream;
            }
            public async Task Move(string source, string destination)
            {
                await Copy(source, destination);
                await S3Client.DeleteAsync(Domain, source, new Dictionary<string, object>());
            }

            public async Task Copy(string source, string destination)
            {
                CopyObjectRequest request = new CopyObjectRequest();
                request.SourceBucket = Domain;
                request.DestinationBucket = Domain;
                request.SourceKey = source;
                request.DestinationKey = destination;
                request.CannedACL = S3CannedACL.PublicRead;
                await S3Client.CopyObjectAsync(request);
            }
        }


        public class SQS
        {

            public string KeyId { get; set; } = "AKIAS3EN46735AXZGIW2";
            public string SecretAccessKey { get; set; } = "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";
            public string URL { get; set; } = "https://sqs.ap-northeast-2.amazonaws.com/338862796095";

            public RegionEndpoint Endpoint { get; set; }

            ///HALStatisticsResult.fifo
            //QAStatisticsResult.fifo
            //public static IAmazonSQS SQSClient { get { return new AmazonSQSClient(KeyId, SecretAccessKey, RegionEndpoint.APNortheast2); } }

            public SQS()
            {

            }
            private static ConcurrentDictionary<string, SQS> container = new();
            public static void Add(string name, SQS sqs)
            {
                sqs.SQSClient = new AmazonSQSClient(sqs.KeyId, sqs.SecretAccessKey, sqs.Endpoint);
                container.AddOrUpdate(name, sqs);
            }

            public static SQS Get(string name)
            {
                return container.Get(name);
            }
            public IAmazonSQS SQSClient { get; set; }
            public async Task<bool> Enqueue(SendMessageRequest request)
            {
                request.QueueUrl = $"{URL}/{request.QueueUrl}";

                var response = await SQSClient.SendMessageAsync(request);

                if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
                {
                    return false;
                }
                return true;

            }

            public async Task<List<Message>> Dequeue(ReceiveMessageRequest request, bool delete = true)
            {
                var name = request.QueueUrl;
                request.QueueUrl = $"{URL}/{name}";

                try
                {
                    var response = await SQSClient.ReceiveMessageAsync(request);

                    if (delete == true)
                    {
                        await Delete(name, response.Messages);
                    }

                    return response.Messages;

                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }

                return null;

            }

            public async Task<List<Message>> Peek()
            {
                var request = new ReceiveMessageRequest();
                request.QueueUrl = URL;
                request.VisibilityTimeout = 300;
                request.MaxNumberOfMessages = 10;
                var response = await SQSClient.ReceiveMessageAsync(request);
                return response.Messages;
            }

            public async Task Delete(string queue, List<Message> messages)
            {
                if (messages == null || messages.Count == 0) { return; }
                var delete = new DeleteMessageBatchRequest();
                delete.QueueUrl = $"{URL}/{queue}";
                delete.Entries = new List<DeleteMessageBatchRequestEntry>();
                {
                    try
                    {
                        foreach (var e in messages)
                        {
                            //if ((from entry in delete.Entries where entry.Id == e.MessageId select entry).FirstOrDefault() != null)
                            //{
                            //    continue;
                            //}

                            try
                            {
                                var ret = await SQSClient.DeleteMessageAsync($"{URL}/{queue}", e.ReceiptHandle);
                            }
                            catch
                            {
                                Logger.Error(e);
                            }


                            //delete.Entries.Add(new DeleteMessageBatchRequestEntry()
                            //{
                            //    Id = e.MessageId,
                            //    ReceiptHandle = e.ReceiptHandle,
                            //});
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                    }
                }
                //if (delete.Entries.Count > 0)
                //{
                //    var resposne = await SQSClient.DeleteMessageBatchAsync(delete);
                //    if (resposne.Successful.Count != delete.Entries.Count)
                //    {

                //    }
                //}
            }

            public async Task Delete(string queue, Message message)
            {
                var delete = new DeleteMessageBatchRequest();
                delete.QueueUrl = $"{URL}/{queue}";
                delete.Entries = new List<DeleteMessageBatchRequestEntry>();
                {
                    try
                    {
                        delete.Entries.Add(new DeleteMessageBatchRequestEntry()
                        {
                            Id = message.MessageId,
                            ReceiptHandle = message.ReceiptHandle,
                        });

                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                    }
                }
                if (delete.Entries.Count > 0)
                {
                    var response = await SQSClient.DeleteMessageBatchAsync(delete);
                    if (response.Successful.Count != delete.Entries.Count)
                    {

                    }
                }
            }
        }

        public class Lambda
        {
            public static async Task<bool> Deploy(string function, string path, string profile)
            {
                try
                {
                    File.Delete($"{function}.zip");
                    ZipFile.CreateFromDirectory(path, $"{function}.zip");

                    string process = "aws";

                    if (Environment.OSVersion.Platform == PlatformID.Win32S ||
                                Environment.OSVersion.Platform == PlatformID.Win32Windows ||
                                Environment.OSVersion.Platform == PlatformID.Win32NT ||
                                Environment.OSVersion.Platform == PlatformID.WinCE)
                    {
                        process = "C:/Program Files/Amazon/AWSCLIV2/aws.exe";
                    }

                    string args = $"lambda update-function-code --function-name {function} --zip-file {"fileb://"}{Path.Combine(Directory.GetCurrentDirectory(), function)}.zip --profile {profile}";

                    var ps = Process.Start(new ProcessStartInfo()
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        FileName = process,
                        Arguments = args,
                        RedirectStandardOutput = true,
                    });


                    ps.WaitForExit(15000);
                    Process.GetProcessById(ps.Id).Kill();

                }
                catch (System.Exception)
                {

                    throw;
                }
                finally
                {
                    File.Delete($"{function}.zip");
                }

                await Task.CompletedTask;
                return true;
            }
        }
    }
}
