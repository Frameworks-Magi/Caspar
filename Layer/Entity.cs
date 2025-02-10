using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Framework.Caspar.Container;
using Framework.Caspar.Database;
using static Framework.Caspar.Api;

namespace Framework.Caspar
{
	public partial class Layer
	{
		public class Entity : SynchronizationContext
		{
			private long uid = 0;
			public virtual long UID
			{
				get => uid;
				set
				{
					if (uid == value) { return; }
					uid = value;
					Strand = (int)(uid % global::Framework.Caspar.Api.ThreadCount);
				}
			}

			//public static int _strand;
			public int Strand
			{
				get;
				private set;

			}
			public Entity(Layer layer)
			{
				this.layer = layer;
				UID = global::Framework.Caspar.Api.UniqueKey;
			}

			public Entity()
			{
				this.layer = Singleton<Layer>.Instance;
				UID = global::Framework.Caspar.Api.UniqueKey;
			}
			internal enum State
			{
				IDLE = 0,
				WAIT,
				RUN,
				CLOSING,
				CLOSE,
			}
			protected internal int post = 0;
			protected int state = (int)State.IDLE;
			internal bool interrupted = false;
			//internal bool locked = false;
			//public bool IsLocked { get { return locked; } }
			internal Layer layer;

			public Layer Layer { get { return layer; } }
			internal ConcurrentQueue<Action> messages = new ConcurrentQueue<Action>();
			internal ConcurrentQueue<Action> continuations = new ConcurrentQueue<Action>();

			public class TaskStatus
			{
				public System.Threading.Tasks.Task Task { get; set; } = null;
			}

			internal ConcurrentQueue<Action> asynchronouslies = new ConcurrentQueue<Action>();

			internal protected virtual async Task OnClose()
			{
				var temp = sessions;
				sessions = null;
				if (temp != null)
				{
					foreach (var item in temp.Values)
					{
						try
						{
							item.Dispose();
						}
						catch
						{

						}
					}
				}

				await Task.CompletedTask;
			}

			private ConcurrentHashSet<Framework.Caspar.Database.Session> sessions = new();

			internal void Add(Framework.Caspar.Database.Session session)
			{
				sessions.AddOrUpdate(session);
			}

			internal void Remove(Framework.Caspar.Database.Session session)
			{
				sessions?.Remove(session);
			}

			public virtual bool IsClose()
			{
				if (state == (int)State.CLOSING || state == (int)State.CLOSE)
				{
					return true;
				}
				return false;
			}

			//protected internal virtual void OnUpdate()
			//{
			//}

			//public void Update()
			//{
			//    messages.Enqueue(this.OnUpdate);

			//    if (ToWait())
			//    {
			//        layer.Post(this);
			//    }
			//}

			public override void Post(SendOrPostCallback d, object state)
			{
				PostMessage(() => { d(state); });
			}

			public override void Send(SendOrPostCallback d, object state)
			{
				base.Send(d, state);
			}


			public void Close()
			{

				if (Interlocked.Exchange(ref state, (int)State.CLOSE) == (int)State.CLOSE) { return; }

				try
				{
					var temp = sessions;
					sessions = null;
					if (temp != null)
					{
						foreach (var item in temp.Values)
						{
							try
							{
								item.Dispose();
							}
							catch
							{

							}
						}
					}

				}
				catch
				{

				}


				this.Interrupt();



				layer.Close(this);

				//Framework.Caspar.Layers.Entity.Close(this);

			}

			public void Interrupt()
			{
				interrupted = true;
			}

			internal bool ToRun()
			{
				Interlocked.Exchange(ref post, 0);
				if (Interlocked.CompareExchange(ref state, (int)State.RUN, (int)State.WAIT) == (int)State.WAIT)
				{
					return true;
				}
				return false;
			}
			internal bool ToWait()
			{

				Interlocked.Increment(ref post);
				if (Interlocked.CompareExchange(ref state, (int)State.WAIT, (int)State.IDLE) == (int)State.IDLE)
				{
					PostAt = DateTime.UtcNow;
					return true;
				}

				return false;
			}
			internal bool ToIdle()
			{
				if (Interlocked.CompareExchange(ref state, (int)State.IDLE, (int)State.RUN) == (int)State.RUN)
				{
					return true;
				}
				return false;
			}
			internal ConcurrentDictionary<System.Threading.Tasks.Task, System.Threading.Tasks.Task> locks = new();
			public bool IsLocked { get { return locks.Count > 0; } }

			public DateTime PostAt { get; set; }
			public Session Session { get; internal set; }

			public void Lock(System.Threading.Tasks.Task task)
			{
				if (locks.TryAdd(task, task) == true)
				{
				}
			}

			public void Unlock(System.Threading.Tasks.Task task)
			{
				locks.TryRemove(task, out task);
				if (ToWait())
				{
					layer.Post(this);
				}
			}

			internal void PostContinuation(System.Threading.Tasks.Task task, Action continuation)
			{
				try
				{
					if (locks.Count > 0)
					{
						continuations.Enqueue(continuation);
					}
					else
					{
						messages.Enqueue(continuation);
					}
				}
				catch (Exception ex)
				{
					Logger.Error(ex);
				}
				finally
				{
					if (task != null)
					{
						locks.TryRemove(task, out task);
					}
					if (ToWait())
					{
						layer.Post(this);
					}
				}

			}


			public bool PostMessage(Action callback)
			{
				if (callback == null) { return false; }
				if (state == (int)State.CLOSE)
				{
					global::Framework.Caspar.Api.Logger.Warning($"PostMessage To CLOSE Entity {this.GetType()} {new StackTrace()}");
					return false;
				}

				// if (CurrentEntity.Value == this)
				// {
				//     Logger.Error($"Deadlock detected. {this.GetType()} PostMessage self. this action going to deadlock. message enqueue continuations");
				//     Logger.Verbose(new StackTrace());
				//     continuations.Enqueue(callback);
				// }
				// else 
				if (FromDelegateUID.Value == UID)
				{
					continuations.Enqueue(callback);
				}
				else
				{
					messages.Enqueue(callback);
				}

				if (ToWait())
				{
					layer.Post(this);
				}
				return true;
			}

			public async Task<bool> PostMessage(Func<Task> callback)
			{
				if (callback == null) { return false; }
				if (state == (int)State.CLOSE)
				{
					global::Framework.Caspar.Api.Logger.Warning($"PostMessage To CLOSE Entity {this.GetType()} {new StackTrace()}");
					return false;
				}

				global::System.Threading.Tasks.TaskCompletionSource<bool> TCS = new TaskCompletionSource<bool>();

				asynchronouslies.Enqueue(async () =>
				{
					try
					{
						await callback();
						TCS.SetResult(true);
					}
					catch (Exception e)
					{
						TCS.SetException(e);
					}

				});

				if (ToWait())
				{
					layer.Post(this);
				}

				return await TCS.Task;
			}


			public async Task<T> PostMessage<T>(Func<Task<T>> callback)
			{
				if (callback == null) { return default(T); }
				if (state == (int)State.CLOSE)
				{
					global::Framework.Caspar.Api.Logger.Warning($"PostMessage To CLOSE Entity {this.GetType()} {new StackTrace()}");
					return default(T);
				}

				global::System.Threading.Tasks.TaskCompletionSource<T> TCS = new TaskCompletionSource<T>();
				asynchronouslies.Enqueue(async () =>
				{
					try
					{
						//						SynchronizationContext.SetSynchronizationContext(this);
						TCS.SetResult(await callback());
					}
					catch (Exception e)
					{
						TCS.SetException(e);
					}

				});
				if (ToWait())
				{
					layer.Post(this);
				}
				return await TCS.Task;
			}

			internal protected virtual void OnException(Exception e)
			{
				global::Framework.Caspar.Api.Logger.Error(e);
			}

			protected internal bool IsPost()
			{
				return (post > 0) || (messages.Count > 0) || (continuations.Count > 0);
			}

		}
	}

}
