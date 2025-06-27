using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Caspar.Attributes
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

        private static IEnumerable<Type> GetLoadableTypes(Assembly[] assemblies)
        {
            var allTypes = new List<Type>();

            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes();
                    allTypes.AddRange(types);
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // 로드 가능한 타입만 추가
                    // var loadableTypes = ex.Types.Where(t => t != null);
                    // allTypes.AddRange(loadableTypes);

                    // // 로드 실패한 타입들에 대한 정보 로깅 (선택사항)
                    // Console.WriteLine($"Assembly: {assembly.FullName}");
                    // foreach (var loaderException in ex.LoaderExceptions)
                    // {
                    //     Console.WriteLine($"Loader Exception: {loaderException?.Message}");
                    // }
                }
                catch (Exception ex)
                {
                    // 다른 예외는 로깅만 하고 계속 진행
                    //Console.WriteLine($"Failed to load types from assembly {assembly.FullName}: {ex.Message}");
                }
            }

            return allTypes;
        }
        static public void StartUp()
        {

            var caspar = typeof(Caspar.Api);
            var assembly = System.Reflection.Assembly.GetAssembly(caspar);

            // // 안전한 타입 로드 메서드
            var types = GetLoadableTypes(AppDomain.CurrentDomain.GetAssemblies());
            var classes = types.Where(t => t.IsClass && t.GetCustomAttributes(typeof(Delegatable), false).Length > 0);

            // var classes = (from asm in AppDomain.CurrentDomain.GetAssemblies()
            //                from type in asm.GetTypes()
            //                where type.IsClass
            //                select type);


            void listen(global::Caspar.Attributes.Delegatable delegatable, Type c)
            {
                {
                    var type = assembly.GetType($"Caspar.Protocol.Delegator`1[[{c.FullName}, {c.Assembly.FullName}]]");
                    var method = type.GetMethod("Listen", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, new Type[] { typeof(ushort) }, null);
                    method.Invoke(null, new object[] { delegatable.Port });

                }


                if (Caspar.Api.StandAlone == true) { return; }
                {
                    var type = assembly.GetType($"Caspar.Protocol.Delegator`1+Listener[[{c.FullName}, {c.Assembly.FullName}]]");
                    var listener = Activator.CreateInstance(type);
                    var method = listener.GetType().GetMethod("Run", new Type[] { });
                    method.Invoke(listener, new object[] { });

                }
            }


            foreach (var c in classes)
            {

                try
                {
                    foreach (var attribute in c.GetCustomAttributes(false))
                    {

                        var delegatable = attribute as global::Caspar.Attributes.Delegatable;
                        if (delegatable == null)
                        {
                            continue;
                        }

                        if (delegatable.RemoteType == string.Empty)
                        {
                            listen(delegatable, c);
                        }
                        else
                        {
                            if (c.FullName == delegatable.RemoteType)
                            {
                                listen(delegatable, c);
                            }

                            if (Caspar.Api.StandAlone == true)
                            {
                                var type = assembly.GetType($"Caspar.Protocol.Delegator`1[[{c.FullName}, {c.Assembly.FullName}]]");
                                var remoteType = c.Assembly.GetType(delegatable.RemoteType);
                                var method = type.GetMethod("Create", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, new Type[] { typeof(long), typeof(bool) }, null);
                                var delegator = method.Invoke(null, new object[] { (long)Caspar.Api.Idx, remoteType != null });
                                (delegator as Caspar.Protocol.IDelegator).UID = Caspar.Api.Idx;
                                (delegator as Caspar.Protocol.IDelegator).Connect("127.0.0.1", delegatable.Port);
                            }
                            else
                            {
                                var type = assembly.GetType($"Caspar.Protocol.Delegator`1+Connector[[{c.FullName}, {c.Assembly.FullName}]]");
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
