using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Framework.Caspar.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class Initialize : Attribute
    {

        static public void StartUp()
        {

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

                        var initialize = attribute as global::Framework.Caspar.Attributes.Initialize;
                        if (initialize != null)
                        {

                            global::Framework.Caspar.Api.Logger.Info("Initialize " + c);
                            c.GetMethod("Initialize").Invoke(null, null);

                        }


                    }
                }
                catch
                {

                }
            }

        }
    }
}
