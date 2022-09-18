using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Framework.Caspar.Api;

namespace Framework.Caspar.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class Delegatable : Attribute
    {
        public bool Singleton { get; private set; } = false;

        public ushort Port { get; private set; } = 0;

        public string RemoteType { get; private set; } = string.Empty;

        public Delegatable(string remoteType, ushort port, bool singleton = false)
        {
            Singleton = singleton;
            RemoteType = remoteType;
            Port = port;
        }
        public Delegatable(ushort port)
        {
            Singleton = false;
            Port = port;
        }

        static public void StartUp()
        {


            var caspar = typeof(Framework.Caspar.Api);
            var assembly = System.Reflection.Assembly.GetAssembly(caspar);

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

                        var delegatable = attribute as global::Framework.Caspar.Attributes.Delegatable;
                        if (delegatable == null)
                        {
                            continue;
                        }

                        if (delegatable.RemoteType == string.Empty)
                        {

                            // registration delegator and listen;
                            // registration and health


                            // listen
                            {
                                var type = assembly.GetType($"Framework.Caspar.Protocol.Delegator`1[[{c.FullName}, {c.Assembly.FullName}]]");
                                var method = type.GetMethod("Listen", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, new Type[] { typeof(ushort) }, null);
                                method.Invoke(null, new object[] { delegatable.Port });

                            }


                            if (Framework.Caspar.Api.StandAlone == true) { continue; }
                            {
                                var type = assembly.GetType($"Framework.Caspar.Protocol.Delegator`1+Listener[[{c.FullName}, {c.Assembly.FullName}]]");
                                var listener = Activator.CreateInstance(type);
                                var method = listener.GetType().GetMethod("Run", new Type[] { });
                                method.Invoke(listener, new object[] { });

                            }


                            // var connector = new Framework.Caspar.Api.Connector();
                            // connector.Self = false;
                            // connector.Run();

                        }
                        else
                        {
                            if (Framework.Caspar.Api.StandAlone == true)
                            {
                                var type = assembly.GetType($"Framework.Caspar.Protocol.Delegator`1[[{c.FullName}, {c.Assembly.FullName}]]");
                                var remoteType = c.Assembly.GetType(delegatable.RemoteType);
                                var method = type.GetMethod("Create", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, new Type[] { typeof(long), typeof(bool) }, null);
                                var delegator = method.Invoke(null, new object[] { (long)0, remoteType != null });
                                (delegator as Framework.Caspar.Protocol.IDelegator).UID = Framework.Caspar.Api.Idx;
                                (delegator as Framework.Caspar.Protocol.IDelegator).Connect("127.0.0.1", delegatable.Port);
                            }
                            else
                            {
                                var type = assembly.GetType($"Framework.Caspar.Protocol.Delegator`1+Connector[[{c.FullName}, {c.Assembly.FullName}]]");
                                var connector = Activator.CreateInstance(type);

                                connector.GetType().GetProperty("Port").SetValue(connector, delegatable.Port);

                                var self = c.Assembly.GetType(delegatable.RemoteType) != null;
                                connector.GetType().GetProperty("Self").SetValue(connector, self);
                                connector.GetType().GetProperty("RemoteType").SetValue(connector, delegatable.RemoteType);

                                var method = connector.GetType().GetMethod("Run", new Type[] { });
                                method.Invoke(connector, new object[] { });
                            }
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
