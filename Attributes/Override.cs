using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Framework.Caspar.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class Override : Attribute
    {
        static private HashSet<string> needAssemblies = new HashSet<string>();
        static private HashSet<string> ignoreAssemblies = new HashSet<string>();

        public static void StartUp()
        {

            //string path = Directory.GetCurrentDirectory();
            //path += "/Override/Config.xml";

            //FileInfo fi = new FileInfo(path);
            //if (fi == null) { return; }
            //Framework.Caspar.Api.OverrideConfigWatcher.Update(path);

        }

        internal static void AddReference(string name)
        {
            needAssemblies.Add(name.ToLower());
        }
        static internal void RemoveReference(string name)
        {
            ignoreAssemblies.Add(name.ToLower());
        }
        internal static void Clear()
        {
            needAssemblies.Clear();
            ignoreAssemblies.Clear();
        }

        internal static bool IsContain(string dll)
        {
            if (needAssemblies.Contains(dll) == true)
            {
                return true;
            }
            if (ignoreAssemblies.Contains(dll) == true)
            {
                return true;
            }

            return false;
        }

        internal static void AddReference(System.CodeDom.Compiler.CompilerParameters parameters)
        {

            foreach (var e in needAssemblies)
            {
                parameters.ReferencedAssemblies.Add(e);
            }

        }
    }
}
