using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Framework.Caspar
{
    public static partial class Api
    {
        public class Localization
        {
            //(country, type)
            public static Dictionary<(string, string), Dictionary<string, string>> metadatas = new Dictionary<(string, string), Dictionary<string, string>>();

            public static void StartUp()
            {
                try
                {
                    foreach (var e in Directory.GetFiles("Resources/Localization", "*.csv", SearchOption.AllDirectories))
                    {
                        var country = Path.GetDirectoryName(e);
                        country = Path.GetFileName(country);
                        var type = Path.GetFileNameWithoutExtension(e);
                        country = country.Split('-')[1];

                        metadatas.Remove((country, type));

                        var rows = new Dictionary<string, string>();
                        metadatas.Add((country, type), rows);

                        using (var file = File.OpenText(e))
                        {
                            file.ReadLine();

                            while (true)
                            {
                                try
                                {
                                    var row = file.ReadLine();
                                    if (row.IsNullOrEmpty() == true)
                                    {
                                        break;
                                    }

                                    var tokens = row.Split(',');

                                    if (tokens.Length > 2)
                                    {
                                        for (int i = 2; i < tokens.Length; ++i)
                                        {
                                            tokens[1] += $",{tokens[i]}";
                                        }
                                    }

                                    if (tokens.Length > 1)
                                    {
                                        rows.Add(tokens[0].Trim('\"'), tokens[1].Trim('\"'));
                                    }

                                }
                                catch
                                {
                                    break;
                                }
                            }

                        }

                    }
                }
                catch (Exception)
                {

                }
                
            }

            public static string Localize((string, string) code, string key)
            {
                if (metadatas.TryGetValue(code, out var table))
                {
                    if (table.TryGetValue(key, out string value))
                    {
                        return value;
                    }
                    else
                    {
                        Logger.Error($"Can not find Localize {code}:{key}");
                        return "";
                    }
                }
                else
                {
                    Logger.Error($"Can not find Localize {code}:{key}");
                    return "";
                }
            }

        }
    }
    
}
