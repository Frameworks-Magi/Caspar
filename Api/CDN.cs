using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using Microsoft.WindowsAzure.Storage;
//using Microsoft.WindowsAzure.Storage.Auth;
//using Microsoft.WindowsAzure.Storage.Blob;
using global::Amazon.CloudFront;
using static Caspar.Api;
using System.Net;
using Amazon.S3;
using Amazon;
using Aliyun.Acs.Core.Profile;
using Aliyun.Acs.Core;
using Aliyun.Acs.Cdn.Model.V20180510;

namespace Caspar
{
    public static class CDN
    {
        public static IAmazonS3 S3Client { get; set; }
        public static string Domain { get; set; } = "";
        public static async Task<Stream> Get(string path)
        {
            try
            {
                var res = await S3Client.GetObjectAsync(Domain, path);
                return res.ResponseStream;
            }
            catch
            {
                Console.WriteLine($"{Domain}, {path}");
                throw;
            }

        }

        public static string CloudFront { get; set; } = "";
        public static string CFKeyId { get; set; } = "";
        public static CloudFrontPEM PEM { get; set; } = () =>
        {
            return typeof(global::Caspar.Api).Assembly.GetManifestResourceStream("Caspar.Resources.pk-CloudFront.pem");
        };

        public delegate Stream CloudFrontPEM();
        public static async Task<Stream> Get(string path, string dest = "", IProgress<double> progress = null)
        {
            string uri = "";

            using (var stream = PEM())
            {
                using (var reader = new StreamReader(stream))
                {
                    uri = AmazonCloudFrontUrlSigner.GetCannedSignedURL(
                    AmazonCloudFrontUrlSigner.Protocol.https,
                    CloudFront,
                    reader,
                    $"{path}",
                    CFKeyId,
                    DateTime.UtcNow.AddMinutes(10));
                }
            }

            long totalBytes = 0;
            long recvedBytes = 0;
            byte[] data = new byte[0];
            var task = Task.Run(async () =>
            {

                using (WebClient client = new WebClient())
                {
                    if (progress != null)
                    {
                        client.DownloadProgressChanged += (sender, e) =>
                        {
                            totalBytes = e.TotalBytesToReceive;
                            recvedBytes = e.BytesReceived;
                        };
                    }

                    if (dest.IsNullOrEmpty())
                    {
                        try
                        {
                            data = await client.DownloadDataTaskAsync(new Uri(uri));
                        }
                        catch (Exception)
                        {

                        }
                    }
                    else
                    {
                        await client.DownloadFileTaskAsync(new Uri(uri), dest);
                    }


                }

            });

            if (progress != null)
            {
                while (task.IsCompleted == false)
                {
                    await Task.Delay(100);
                    if (recvedBytes == 0) { continue; }
                    var done = (double)recvedBytes / totalBytes;
                    progress.Report(done);
                }

                progress.Report(1);
            }
            else
            {
                await task;
            }
            return new MemoryStream(data);
        }

    }

    // public interface ICDN
    // {
    //     Task<Stream> Get(string path);
    // }


    // public class Alibaba : ICDN
    // {
    //     public async Task<Stream> Get(string path)
    //     {
    //         IClientProfile clientProfile = DefaultProfile.GetProfile("<your-region-id>", "<your-access-key-id>", "<your-access-key-secret>");
    //         DefaultAcsClient client = new DefaultAcsClient(clientProfile);

    //         try
    //         {

    //             AddCdnDomainRequest request = new AddCdnDomainRequest();
    //             request.CdnType = "web";
    //             request.DomainName = "test.com";
    //             request.Sources = "test.com";
    //             //request..SourceType = "domain";

    //             //Initiate the request
    //             AddCdnDomainResponse response = client.GetAcsResponse(request);

    //             //response.HttpResponse.Content
    //             Logger.Info("Success");
    //         }
    //         catch
    //         {

    //         }
    //         await System.Threading.Tasks.Task.CompletedTask;

    //         return null;
    //     }
    // }

}
