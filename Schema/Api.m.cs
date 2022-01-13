using System;
using static Framework.Caspar.Api;
namespace Framework.Protobuf {

	public static partial class Api {
		public delegate void RuntimeBindException(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e, dynamic handler, dynamic notifier, Type mag);
		public delegate void RuntimeBinderInternalCompilerException(Microsoft.CSharp.RuntimeBinder.RuntimeBinderInternalCompilerException e, dynamic handler, Type mag);
		public static RuntimeBindException RuntimeBindExceptionCallback = (e, handler, notifier, msg) => {
			Logger.Info(string.Format("'{0}' has not handler for '{1}''{2}'", handler.GetType(), notifier.GetType(), msg));
		};
		public static RuntimeBinderInternalCompilerException RuntimeBinderInternalCompilerExceptionCallback = (e, handler, msg) => {
			Logger.Info(string.Format("RuntimeBinderInternalCompilerException from '{0}'", msg));
			Logger.Info(e);
		};
		private delegate global::Google.Protobuf.IMessage Deserializer(global::System.IO.MemoryStream stream);
		private delegate Action Binder(dynamic handler, dynamic notifier, global::System.IO.Stream stream);
		private static global::System.Collections.Generic.Dictionary<int, Deserializer> deserilizer = new System.Collections.Generic.Dictionary<int, Deserializer>();
		private static global::System.Collections.Generic.Dictionary<int, Binder> Binders = new global::System.Collections.Generic.Dictionary<int, Binder>();
		private static global::System.Collections.Generic.Dictionary<int, Type> types = new global::System.Collections.Generic.Dictionary<int, Type>();
		static public void StartUp() {
			global::Framework.Caspar.Id<global::Framework.Protobuf.Message.Encript>.Value = 0x7FF60001;  // 2146828289
			global::Framework.Caspar.Id<global::Framework.Protobuf.Message.Seed>.Value = 0x7FF60002;  // 2146828290
			global::Framework.Caspar.Id<global::Framework.Protobuf.Message.Connect>.Value = 0x7FF60003;  // 2146828291
			global::Framework.Caspar.Id<global::Framework.Protobuf.Message.ProcessInfo>.Value = 0x7FF60004;  // 2146828292
			global::Framework.Caspar.Id<global::Framework.Protobuf.Message.Patch>.Value = 0x7FF60005;  // 2146828293
			global::Framework.Caspar.Id<global::Framework.Protobuf.Message.Restart>.Value = 0x7FF60006;  // 2146828294
			global::Framework.Caspar.Id<global::Framework.Protobuf.Message.Terminate>.Value = 0x7FF60007;  // 2146828295
			global::Framework.Caspar.Id<global::Framework.Protobuf.Message.Config>.Value = 0x7FF60008;  // 2146828296
			global::Framework.Caspar.Id<global::Framework.Protobuf.Message.Notify>.Value = 0x7FF60009;  // 2146828297
			global::Framework.Caspar.Id<global::Framework.Protobuf.Message.Caspars>.Value = 0x7FF6000A;  // 2146828298

			Binders.Add(2146828289, (handler, notifier, stream) =>
			{
				var msg = global::Framework.Protobuf.Message.Encript.Parser.ParseFrom(stream);
				return () => {
					try { handler.OnMessage(notifier, msg); }
					catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e) {
						RuntimeBindExceptionCallback(e, handler, notifier, msg.GetType());
					}
					catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderInternalCompilerException e) {
						RuntimeBinderInternalCompilerExceptionCallback(e, handler, msg.GetType());
					}
					catch { throw; }
				};
			});
			deserilizer.Add(2146828289, (stream) => {
				var msg = global::Framework.Protobuf.Message.Encript.Parser.ParseFrom(stream);
				return msg;
			});
            types.Add(2146828289, typeof(global::Framework.Protobuf.Message.Encript));
			Binders.Add(2146828290, (handler, notifier, stream) =>
			{
				var msg = global::Framework.Protobuf.Message.Seed.Parser.ParseFrom(stream);
				return () => {
					try { handler.OnMessage(notifier, msg); }
					catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e) {
						RuntimeBindExceptionCallback(e, handler, notifier, msg.GetType());
					}
					catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderInternalCompilerException e) {
						RuntimeBinderInternalCompilerExceptionCallback(e, handler, msg.GetType());
					}
					catch { throw; }
				};
			});
			deserilizer.Add(2146828290, (stream) => {
				var msg = global::Framework.Protobuf.Message.Seed.Parser.ParseFrom(stream);
				return msg;
			});
            types.Add(2146828290, typeof(global::Framework.Protobuf.Message.Seed));
			Binders.Add(2146828291, (handler, notifier, stream) =>
			{
				var msg = global::Framework.Protobuf.Message.Connect.Parser.ParseFrom(stream);
				return () => {
					try { handler.OnMessage(notifier, msg); }
					catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e) {
						RuntimeBindExceptionCallback(e, handler, notifier, msg.GetType());
					}
					catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderInternalCompilerException e) {
						RuntimeBinderInternalCompilerExceptionCallback(e, handler, msg.GetType());
					}
					catch { throw; }
				};
			});
			deserilizer.Add(2146828291, (stream) => {
				var msg = global::Framework.Protobuf.Message.Connect.Parser.ParseFrom(stream);
				return msg;
			});
            types.Add(2146828291, typeof(global::Framework.Protobuf.Message.Connect));
			Binders.Add(2146828292, (handler, notifier, stream) =>
			{
				var msg = global::Framework.Protobuf.Message.ProcessInfo.Parser.ParseFrom(stream);
				return () => {
					try { handler.OnMessage(notifier, msg); }
					catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e) {
						RuntimeBindExceptionCallback(e, handler, notifier, msg.GetType());
					}
					catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderInternalCompilerException e) {
						RuntimeBinderInternalCompilerExceptionCallback(e, handler, msg.GetType());
					}
					catch { throw; }
				};
			});
			deserilizer.Add(2146828292, (stream) => {
				var msg = global::Framework.Protobuf.Message.ProcessInfo.Parser.ParseFrom(stream);
				return msg;
			});
            types.Add(2146828292, typeof(global::Framework.Protobuf.Message.ProcessInfo));
			Binders.Add(2146828293, (handler, notifier, stream) =>
			{
				var msg = global::Framework.Protobuf.Message.Patch.Parser.ParseFrom(stream);
				return () => {
					try { handler.OnMessage(notifier, msg); }
					catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e) {
						RuntimeBindExceptionCallback(e, handler, notifier, msg.GetType());
					}
					catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderInternalCompilerException e) {
						RuntimeBinderInternalCompilerExceptionCallback(e, handler, msg.GetType());
					}
					catch { throw; }
				};
			});
			deserilizer.Add(2146828293, (stream) => {
				var msg = global::Framework.Protobuf.Message.Patch.Parser.ParseFrom(stream);
				return msg;
			});
            types.Add(2146828293, typeof(global::Framework.Protobuf.Message.Patch));
			Binders.Add(2146828294, (handler, notifier, stream) =>
			{
				var msg = global::Framework.Protobuf.Message.Restart.Parser.ParseFrom(stream);
				return () => {
					try { handler.OnMessage(notifier, msg); }
					catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e) {
						RuntimeBindExceptionCallback(e, handler, notifier, msg.GetType());
					}
					catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderInternalCompilerException e) {
						RuntimeBinderInternalCompilerExceptionCallback(e, handler, msg.GetType());
					}
					catch { throw; }
				};
			});
			deserilizer.Add(2146828294, (stream) => {
				var msg = global::Framework.Protobuf.Message.Restart.Parser.ParseFrom(stream);
				return msg;
			});
            types.Add(2146828294, typeof(global::Framework.Protobuf.Message.Restart));
			Binders.Add(2146828295, (handler, notifier, stream) =>
			{
				var msg = global::Framework.Protobuf.Message.Terminate.Parser.ParseFrom(stream);
				return () => {
					try { handler.OnMessage(notifier, msg); }
					catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e) {
						RuntimeBindExceptionCallback(e, handler, notifier, msg.GetType());
					}
					catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderInternalCompilerException e) {
						RuntimeBinderInternalCompilerExceptionCallback(e, handler, msg.GetType());
					}
					catch { throw; }
				};
			});
			deserilizer.Add(2146828295, (stream) => {
				var msg = global::Framework.Protobuf.Message.Terminate.Parser.ParseFrom(stream);
				return msg;
			});
            types.Add(2146828295, typeof(global::Framework.Protobuf.Message.Terminate));
			Binders.Add(2146828296, (handler, notifier, stream) =>
			{
				var msg = global::Framework.Protobuf.Message.Config.Parser.ParseFrom(stream);
				return () => {
					try { handler.OnMessage(notifier, msg); }
					catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e) {
						RuntimeBindExceptionCallback(e, handler, notifier, msg.GetType());
					}
					catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderInternalCompilerException e) {
						RuntimeBinderInternalCompilerExceptionCallback(e, handler, msg.GetType());
					}
					catch { throw; }
				};
			});
			deserilizer.Add(2146828296, (stream) => {
				var msg = global::Framework.Protobuf.Message.Config.Parser.ParseFrom(stream);
				return msg;
			});
            types.Add(2146828296, typeof(global::Framework.Protobuf.Message.Config));
			Binders.Add(2146828297, (handler, notifier, stream) =>
			{
				var msg = global::Framework.Protobuf.Message.Notify.Parser.ParseFrom(stream);
				return () => {
					try { handler.OnMessage(notifier, msg); }
					catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e) {
						RuntimeBindExceptionCallback(e, handler, notifier, msg.GetType());
					}
					catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderInternalCompilerException e) {
						RuntimeBinderInternalCompilerExceptionCallback(e, handler, msg.GetType());
					}
					catch { throw; }
				};
			});
			deserilizer.Add(2146828297, (stream) => {
				var msg = global::Framework.Protobuf.Message.Notify.Parser.ParseFrom(stream);
				return msg;
			});
            types.Add(2146828297, typeof(global::Framework.Protobuf.Message.Notify));
			Binders.Add(2146828298, (handler, notifier, stream) =>
			{
				var msg = global::Framework.Protobuf.Message.Caspars.Parser.ParseFrom(stream);
				return () => {
					try { handler.OnMessage(notifier, msg); }
					catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e) {
						RuntimeBindExceptionCallback(e, handler, notifier, msg.GetType());
					}
					catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderInternalCompilerException e) {
						RuntimeBinderInternalCompilerExceptionCallback(e, handler, msg.GetType());
					}
					catch { throw; }
				};
			});
			deserilizer.Add(2146828298, (stream) => {
				var msg = global::Framework.Protobuf.Message.Caspars.Parser.ParseFrom(stream);
				return msg;
			});
            types.Add(2146828298, typeof(global::Framework.Protobuf.Message.Caspars));
		}

		public static Action Bind(dynamic handler, dynamic notifier, int code, global::System.IO.Stream stream) {

			Binder binder = null;
			if (Binders.TryGetValue(code, out binder) == false) { return null; }

			return binder(handler, notifier, stream);

		}
		public static global::Google.Protobuf.IMessage Deserialize(int code, global::System.IO.MemoryStream stream) {

			if (deserilizer.TryGetValue(code, out Deserializer callback) == true) {
				return callback(stream);
			}
			return null;
		}
		public static Type CodeToType(int code) {

			if (types.TryGetValue(code, out Type type) == true) {
				return type;
			}
			return null;
		}
	}
}
