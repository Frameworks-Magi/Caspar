using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon.CloudFront;
using Amazon.S3;
using Amazon.S3.Transfer;
using Caspar;
using Caspar.Container;
using static Caspar.Api;

namespace Caspar
{
    public static partial class Api
    {
        //         public static async Task<bool> SetVersion(Caspar.INotifier notifier, Caspar.Protocol.Terminal.Message msg)
        //         {
        //             var tokens = msg.Command.Split(' ');
        //             if (tokens.Length > 1)
        //             {
        //                 Api.Version = tokens[1];
        //                 return true;
        //             }
        //             else
        //             {
        //                 Logger.Info($"need version");
        //             }
        //             await Task.CompletedTask;
        //             return true;
        //         }

        public static async Task<IList<string>> GetVersions(string path)
        {

            var S3 = Caspar.Platform.AWS.S3.Get("Caspar");
            IAmazonS3 s3Client = S3.S3Client;
            IList<string> versions = new List<string>();

            try
            {
                IList<string> temp = await s3Client.GetAllObjectKeysAsync((string)global::Caspar.Api.Config.AWS.S3.Global.Domain, $"{(string)Caspar.Api.Config.Deploy}/{path}/", null);
                temp.Sort((r, l) =>
                {
                    try
                    {
                        var rv = r.Split('/').Last().Split('.');
                        var rl = l.Split('/').Last().Split('.');

                        for (int i = 0; i < rv.Length && i < rl.Length; ++i)
                        {
                            if (rv[i].ToInt32() < rl[i].ToInt32()) { return 1; }
                            if (rv[i].ToInt32() > rl[i].ToInt32()) { return -1; }
                        }
                    }
                    catch
                    {

                    }

                    return 0;
                });

                foreach (var e in temp.Reverse())
                {
                    versions.Add(e.Split('/').Last());
                }
            }
            catch (Exception e)
            {
                Logger.Info(e);
                return new List<string>();
            }
            finally
            {
            }


            return versions;
        }

    }
}