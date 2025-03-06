using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Caspar.Database.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class Query : Attribute
    {
        private Type provider;
        public Type Provider { get { return provider; } }

        public Query(Type provider = null)
        {
            this.provider = provider;

        }

        static public void StartUp()
        {

            var classes = (from asm in AppDomain.CurrentDomain.GetAssemblies()
                           from type in asm.GetTypes()
                           where type.IsClass
                           select type);


            foreach (var c in classes)
            {



                foreach (var attribute in c.GetCustomAttributes(false))
                {


                    var query = attribute as global::Caspar.Database.Attributes.Query;

                    if (query != null)
                    {

                        global::Caspar.Api.Logger.Info("Resist DB - " + query.provider + " " + c.Namespace + ":" + c.Name);

                        //Caspar.Database.Management.Provider driver = null;
                        //if (query.provider == typeof(Caspar.Database.Management.Relational.MySql)) {
                        //	driver = Caspar.Database.Api.Db(query.provider);
                        //}


                        //if (driver == null) { return; }

                        //foreach (var m in c.GetMethods()) {


                        //	var ma = m.GetCustomAttribute(typeof(Caspar.Database.Attributes.Query)) as Caspar.Database.Attributes.Query;
                        //	if (ma == null) { continue; }

                        //	string procedure = (c.Name + "." + m.Name);
                        //	var callback = (Caspar.Database.Management.Provider.ProcedureOld)Delegate.CreateDelegate(typeof(Caspar.Database.Management.Provider.ProcedureOld), m);
                        //	Caspar.Database.Management.Provider.Add(procedure, callback);

                        //}
                    }

                }
            }

        }
    }
}
