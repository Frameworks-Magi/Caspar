using Framework.Caspar.Container;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using static Framework.Caspar.Api;

namespace Framework.Caspar
{
    public class Metadata
    {
        public static string LocalPath
        {
            set
            {
                string path = Directory.GetCurrentDirectory();
                try
                {
                    while (true)
                    {
                        if (Directory.Exists(Path.Combine(path, value)) == true)
                        {
                            localPath = Path.Combine(path, value);
                            Console.WriteLine($"Set LocalPath {path}");
                            return;
                        }
                        path = Path.Combine(path, "..");
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    return;
                }
            }
            get { return localPath; }
        }
        private static string localPath = "";
        //static System.Threading.ReaderWriterLock rwl = new();
        [Serializable]
        public class Element
        {
            public class PrimaryKey : Attribute { }
            public class SecondaryKey : Attribute { }
            virtual public void CustomLoad(XmlAttributeCollection attributes) { }
            virtual public object CustomLoad(StreamReader reader) { return null; }
        }

        static private Dictionary<global::System.Type, Metadata> tables = new Dictionary<global::System.Type, Metadata>();
        private Dictionary<object, object> metadatas = new Dictionary<object, object>();
        private Dictionary<object, long> expires = new Dictionary<object, long>();
        private ArrayList array = new ArrayList();

        static public T GetElement<T>(object key, int Type) where T : class
        {
            Metadata table = null;


            if (tables.TryGetValue(typeof(T), out table) == false)
            {
                table = new Metadata();
                tables.Add(typeof(T), table);
            }

            long expire = table.expires.Get(key);
            if (expire != default(long) && expire < DateTime.UtcNow.Ticks)
            {
                table.expires.Remove(key);
            }

            if (table.metadatas.TryGetValue(key, out object metadata) == false)
            {
                // read from redis;
            }
            return metadata as T;
        }
        static public T GetElement<T>(object key) where T : class
        {

            Metadata table = null;
            try
            {
                if (tables.TryGetValue(typeof(T), out table) == false)
                {
                    return null;
                }

                if (table.metadatas.TryGetValue(key, out object metadata) == false)
                {
                    return null;
                }
                return metadata as T;
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }


        static public ArrayList GetElements<T>() where T : class
        {

            Metadata table = null;
            if (tables.TryGetValue(typeof(T), out table) == false)
            {
                return null;
            }
            return table.array;
        }

        static public T RandomMetadata<T>() where T : Metadata.Element
        {
            var metadatas = Metadata.GetElements<T>();

            if (metadatas == null || metadatas.Count == 0) { return null; }
            int max = metadatas.Count;
            var metadata = (metadatas[global::Framework.Caspar.Dice.Throw(0, max)] as T);
            return metadata;

        }


        private static void LoadXml<T>(XmlDocument doc) where T : class, new()
        {

            Metadata table;
            if (tables.TryGetValue(typeof(T), out table) == false)
            {
                table = new Metadata();
                tables.Add(typeof(T), table);
            }

            table.Clear();

            XmlNode root = doc.DocumentElement;
            if (root == null) { return; }
            foreach (var e in root.ChildNodes)
            {

                if (e is System.Xml.XmlComment) { continue; }

                XmlElement node = e as XmlElement;
                var metadata = new T();

                var fields = metadata.GetType().GetFields(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance);

                object primaryKey = null;
                object secondaryKey = null;
                foreach (var field in fields)
                {

                    var attribute = node.Attributes[field.Name];
                    if (attribute == null) { continue; }

                    try
                    {
                        if (field.IsInitOnly == false) { continue; }
                        if (field.FieldType.IsEnum == true)
                        {

                            try
                            {
                                var et = Enum.Parse(field.FieldType, attribute.Value);
                                field.SetValue(metadata, et);
                            }
                            catch (Exception ex)
                            {
                                global::Framework.Caspar.Api.Logger.Debug(ex);
                                field.SetValue(metadata, 0);
                            }
                        }
                        else if (field.FieldType.IsPrimitive == true)
                        {
                            var convertedType = Convert.ChangeType(attribute.Value, field.FieldType);
                            field.SetValue(metadata, convertedType);
                        }
                        else if (field.FieldType == typeof(DateTime))
                        {
                            field.SetValue(metadata, DateTime.Parse(attribute.Value));

                        }
                        else
                        {

                            try
                            {
                                field.SetValue(metadata, attribute.Value);
                            }
                            catch (Exception ex)
                            {
                                global::Framework.Caspar.Api.Logger.Debug(ex);
                                field.SetValue(metadata, null);
                            }

                        }

                        var primary = field.GetCustomAttributes(typeof(Metadata.Element.PrimaryKey), false);
                        var secondary = field.GetCustomAttributes(typeof(Metadata.Element.SecondaryKey), false);

                        if (primary != null && primary.Length > 0)
                        {
                            primaryKey = field.GetValue(metadata);
                        }
                        if (secondary != null && secondary.Length > 0)
                        {
                            secondaryKey = field.GetValue(metadata);
                        }
                    }
                    catch (Exception ex)
                    {
                        global::Framework.Caspar.Api.Logger.Debug(ex);
                        //Debug.LogError(e + " " + field.Name);
                    }

                }

                try
                {
                    //metadata.CustomLoad(node.Attributes);
                }
                catch { }

                //Debug.Log("Add Table " + table.GetType() + " Key : " + metadata.GetKey());
                table.array.Add(metadata);

                object key = null;
                if (primaryKey != null && secondaryKey != null)
                {
                    key = (primaryKey, secondaryKey);
                }
                else if (primaryKey != null)
                {
                    key = primaryKey;
                }
                else if (secondaryKey != null)
                {
                    key = secondaryKey;
                }
                else
                {
                    global::Framework.Caspar.Api.Logger.Info($"Not found {nameof(T)}'s Key");
                    break;
                }
                if (table.metadatas.ContainsKey(key) == false)
                {
                    table.metadatas.Add(key, metadata);
                }

            }
        }

        public void Add(object key, Metadata.Element metadata)
        {
            array.Add(metadata);

            if (metadatas.ContainsKey(key) == false)
            {
                metadatas.Add(key, metadata);
            }
        }

        public void Clear()
        {
            metadatas.Clear();
            array.Clear();
        }

        static public void LoadXml<T>(string path) where T : class, new()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            LoadXml<T>(doc);
        }
        static public void LoadJson<T>(string path) where T : class, new()
        {
            try
            {
                using (var stream = File.OpenText(path))
                {
                    LoadJson<T>(stream);
                    return;
                }
            }
            catch (Exception ex)
            {
                global::Framework.Caspar.Api.Logger.Info(ex.Message);
            }
        }

        public delegate JArray CustomCallbackFunc(string json);

        public static CustomCallbackFunc CustomJsonParser { get; set; } = null;

        static public void LoadJson<T>(StreamReader reader) where T : class, new()
        {

            Metadata table;
            if (tables.TryGetValue(typeof(T), out table) == false)
            {
                table = new Metadata();
                tables.Add(typeof(T), table);
            }

            table.Clear();
            T[] array = null;


            var loader = typeof(T).GetMethod("CustomLoad", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

            if (loader != null)
            {
                object obj = loader.Invoke(null, new object[] { reader.ReadToEnd() });
                if (obj == null) { return; }
                array = obj as T[];
            }
            else
            {
                try
                {
                    if (CustomJsonParser != null)
                    {
                        array = CustomJsonParser(reader.ReadToEnd()).ToObject<T[]>();
                    }
                    else
                    {
                        array = Newtonsoft.Json.JsonConvert.DeserializeObject<T[]>(reader.ReadToEnd());
                    }

                    if (array == null) { return; }
                }
                catch (Exception e)
                {
                    global::Framework.Caspar.Api.Logger.Error(e);
                    return;
                }
            }



            foreach (T metadata in array)
            {
                var fields = metadata.GetType().GetFields();

                object primaryKey = null;
                object secondaryKey = null;
                foreach (var field in fields)
                {
                    var primary = field.GetCustomAttributes(typeof(Metadata.Element.PrimaryKey), false);
                    var secondary = field.GetCustomAttributes(typeof(Metadata.Element.SecondaryKey), false);

                    if (primary != null && primary.Length > 0)
                    {
                        primaryKey = field.GetValue(metadata);
                    }
                    if (secondary != null && secondary.Length > 0)
                    {
                        secondaryKey = field.GetValue(metadata);
                    }
                }

                table.array.Add(metadata);
                object key = null;
                if (primaryKey != null && secondaryKey != null)
                {
                    key = (primaryKey, secondaryKey);
                }
                else if (primaryKey != null)
                {
                    key = primaryKey;
                }
                else if (secondaryKey != null)
                {
                    key = secondaryKey;
                }

                if (key == null)
                {
                    global::Framework.Caspar.Api.Logger.Info($"Not found {nameof(T)}'s Key");
                    continue;
                }

                if (table.metadatas.ContainsKey(key) == false)
                {
                    table.metadatas.Add(key, metadata);
                }
                else
                {
                    global::Framework.Caspar.Api.Logger.Info($"Duplicate Metadata Key. Type - {typeof(T)} , Key = {key}");
                }

            }



        }

        static public void LoadCsv<T>(string path) where T : Metadata.Element, new()
        {
            try
            {
                using (var stream = File.OpenText(path))
                {
                    LoadCsv<T>(stream);
                    return;
                }
            }
            catch (Exception ex)
            {
                global::Framework.Caspar.Api.Logger.Debug(ex);
                try
                {
                    LoadCsv<T>((StreamReader)null);
                }
                catch
                {

                }
            }
        }

        static public void LoadCsv<T>(StreamReader reader) where T : Metadata.Element, new()
        {

            Metadata table;
            if (tables.TryGetValue(typeof(T), out table) == false)
            {
                table = new Metadata();
                tables.Add(typeof(T), table);
            }

            table.Clear();

            T loader = new T();

            T[] array = null;


            string[] cols = null;
            {
                string tokens = reader.ReadLine();
                cols = tokens.Split(',');
            }


            // while (true)
            // {
            //     string tokens = reader.ReadLine();
            //     tokens.Split(',');
            // }


            array = loader.CustomLoad(reader) as T[];

            if (array == null) { return; }


            foreach (T metadata in array)
            {
                var fields = metadata.GetType().GetFields();

                object primaryKey = null;
                object secondaryKey = null;
                foreach (var field in fields)
                {
                    var primary = field.GetCustomAttributes(typeof(Metadata.Element.PrimaryKey), false);
                    var secondary = field.GetCustomAttributes(typeof(Metadata.Element.SecondaryKey), false);

                    if (primary != null && primary.Length > 0)
                    {
                        primaryKey = field.GetValue(metadata);
                    }
                    if (secondary != null && secondary.Length > 0)
                    {
                        secondaryKey = field.GetValue(metadata);
                    }
                }

                table.array.Add(metadata);
                object key = null;
                if (primaryKey != null && secondaryKey != null)
                {
                    key = (primaryKey, secondaryKey);
                }
                else if (primaryKey != null)
                {
                    key = primaryKey;
                }
                else if (secondaryKey != null)
                {
                    key = secondaryKey;
                }

                if (key == null)
                {
                    global::Framework.Caspar.Api.Logger.Info($"Not found {nameof(T)}'s Key");
                    continue;
                }

                if (table.metadatas.ContainsKey(key) == false)
                {
                    table.metadatas.Add(key, metadata);
                }
                else
                {
                    global::Framework.Caspar.Api.Logger.Info($"Duplicate Metadata Key. Type - {typeof(T)} , Key = {key}");
                }

            }



        }

    }
}
