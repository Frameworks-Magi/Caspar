using Amazon.CloudFront;
using Caspar;
//using Microsoft.WindowsAzure.Storage;
//using Microsoft.WindowsAzure.Storage.Auth;
//using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static Caspar.Api;

namespace Caspar.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class Metadata : Attribute
    {
        public enum Type
        {
            Xml = 0,
            Json = 1,
            Csv = 2,
        }

        public Type type;
        public string Extension;
        public string Builder { get; private set; }
        public string Filename { get; private set; }

        public static async Task Reload()
        {
            await StartUp();
        }

        static public string Version { get; set; } = "0.0.0.0";
        public Metadata(Type type = Type.Xml, string filename = "", string builder = "", string extension = "")
        {
            this.type = type;
            this.Builder = builder;
            this.Filename = filename;
            this.Extension = extension;
        }

        public class Layer : global::Caspar.Layer { }
        public class Loader : global::Caspar.Layer.Frame
        {
            public Loader() : base(Singleton<Layer>.Instance) { }
            public static string Path { get; set; } = $"{(string)Caspar.Api.Config.Deploy}/Metadata";
            public async Task Load()
            {

                var Assemblies = new Queue<(string, System.Reflection.MethodInfo, System.Reflection.MethodInfo, Metadata)>();
                var Metadatas = new Queue<(string, System.Reflection.MethodInfo, System.Reflection.MethodInfo, Metadata, byte[])>();

                global::Caspar.Api.Logger.Info($"Load Metadata Version {Version}");
                var classes = (from asm in AppDomain.CurrentDomain.GetAssemblies()
                               from type in asm.GetTypes()
                               where type.IsClass
                               select type);

                foreach (var c in classes)
                {
                    try
                    {

                        foreach (var attribute in c.GetCustomAttributes(false))
                        {

                            var metadata = attribute as global::Caspar.Attributes.Metadata;
                            if (metadata != null)
                            {

                                string loader = "LoadXml";

                                if (metadata.type == Type.Json)
                                {
                                    loader = "LoadJson";
                                }
                                else if (metadata.type == Type.Csv)
                                {
                                    loader = "LoadCsv";
                                }

                                var method = typeof(Caspar.Metadata).GetMethod(loader, new System.Type[] { typeof(StreamReader) });
                                if (method.IsGenericMethod == true)
                                {
                                    method = method.MakeGenericMethod(c);

                                }

                                string filename = metadata.Filename;
                                if (string.IsNullOrEmpty(filename) == true)
                                {
                                    filename = c.Name;
                                }

                                string extension = ".xml";
                                if (metadata.type == Type.Json)
                                {
                                    extension = ".json";
                                }
                                else if (metadata.type == Type.Csv)
                                {
                                    extension = ".csv";
                                }

                                if (!string.IsNullOrEmpty(metadata.Extension))
                                {
                                    extension = "." + metadata.Extension;
                                    extension = System.IO.Path.GetExtension(extension);
                                }

                                System.Reflection.MethodInfo callback = null;
                                if (string.IsNullOrEmpty(metadata.Builder) == false)
                                {
                                    callback = c.GetMethod(metadata.Builder, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                                }

                                Assemblies.Enqueue(($"{Path}/{Version}/{filename}{extension}", method, callback, metadata));
                            }
                        }
                    }
                    catch
                    {
                    }
                }


                while (Assemblies.Count > 0)
                {
                    (string Key, System.Reflection.MethodInfo Method, System.Reflection.MethodInfo Callback, Metadata Metadata) e = Assemblies.Dequeue();
                    string uri = "";

                    if (Caspar.Metadata.LocalPath.IsNullOrEmpty() == false)
                    {
                        var filename = global::System.IO.Path.GetFileName(e.Key);
                        var fullpath = global::System.IO.Path.Combine(Caspar.Metadata.LocalPath, filename);
                        if (File.Exists(fullpath) == true)
                        {
                            try
                            {
                                using var fs = File.OpenRead(fullpath);
                                byte[] data = null;
                                using (var ms = new MemoryStream())
                                {
                                    fs.CopyTo(ms);
                                    data = ms.ToArray();
                                }

                                Metadatas.Enqueue((e.Key, e.Method, e.Callback, e.Metadata, data));
                            }
                            catch
                            {
                                Assemblies.Enqueue(e);
                            }
                            finally
                            {

                            }
                            continue;
                        }

                    }

                    var PEM = Caspar.CDN.PEM;
                    using (var stream = PEM())
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            uri = Amazon.CloudFront.AmazonCloudFrontUrlSigner.GetCannedSignedURL(
                            AmazonCloudFrontUrlSigner.Protocol.https,
                            (string)Config.AWS.CloudFront.Domain,
                            new StreamReader(stream),
                            $"{e.Key}",
                            (string)Config.AWS.CloudFront.Key,
                            DateTime.UtcNow.AddMinutes(10));
                        }
                    }

                    // HttpClient를 사용하여 메타데이터 다운로드
                    using (HttpClient httpClient = new HttpClient())
                    {
                        try
                        {
                            Logger.Info($"Download Metadata: {System.IO.Path.GetFileName(e.Key)}");
                            var response = await httpClient.GetAsync(new Uri(uri));
                            response.EnsureSuccessStatusCode();
                            var data = await response.Content.ReadAsByteArrayAsync();
                            Metadatas.Enqueue((e.Key, e.Method, e.Callback, e.Metadata, data));
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                            await Task.Delay(10);
                            Assemblies.Enqueue(e);
                        }
                    }

                }

                await Task.Delay(10);

                while (Metadatas.Count > 0)
                {
                    (string Key, System.Reflection.MethodInfo Method, System.Reflection.MethodInfo Callback, Metadata Metadata, byte[] bytes) e = Metadatas.Dequeue();
                    try
                    {
                        using (var sr = new StreamReader(new MemoryStream(e.bytes)))
                        {
                            e.Method.Invoke(null, new object[] { sr });
                            Logger.Info($"Read Metadata: {System.IO.Path.GetFileName(e.Key)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                    }

                    try
                    {
                        e.Callback?.Invoke(null, null);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                    }
                }
            }

        }



        static public async Task StartUp()
        {
            var task = Singleton<Loader>.Instance;
            await task.PostMessage(task.Load);
        }


    }
}
