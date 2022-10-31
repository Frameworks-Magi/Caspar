using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using static Framework.Caspar.Api;

namespace Framework.Caspar
{
    public class Scheduler : global::Framework.Caspar.Layer.Entity
    {
        //internal static ConcurrentBag<Scheduler> Elements { get; } = new();
        //internal static System.Collections.Generic.List<Scheduler> Elements { get; } = new();
        public Scheduler(Layer layer) : base(layer)
        {
        }


        public Scheduler() : base(Singleton<global::Framework.Caspar.Layer>.Instance)
        {

        }

        internal protected override async Task OnClose()
        {
            try
            {
                //      timer?.Close();
                timer?.Dispose();
                await base.OnClose();
            }
            catch
            {

            }
        }

        protected virtual void OnSchedule() { }

        //System.Timers.Timer timer;
        System.Threading.Timer timer;
        public void Run(int millisecond)
        {
            if (Next != 0) { return; }
            if (timer != null) { return; }

            timer = new((state) =>
            {
                if (IsClose() == true) { timer.Dispose(); timer = null; }
                PostMessage(() => { OnSchedule(); });
            }, this, millisecond, millisecond);
            // timer.
            // timer.Elapsed += (o, e) =>
            // {
            //     try
            //     {
            //         if (Paused == true)
            //         {
            //             return;
            //         }
            //         if (IsClose() == true) { timer.Close(); timer.Dispose(); }
            //         PostMessage(() => { OnSchedule(); });

            //     }
            //     catch
            //     {

            //     }

            // };
            // timer.AutoReset = true;
            // timer.Enabled = true;

            //if (millisecond != 4000) { return; }
            // Next = Framework.Caspar.Api.KST.AddMilliseconds(millisecond).Ticks;
            Next = DateTime.UtcNow.AddMilliseconds(millisecond).Ticks;
            interval = millisecond;

            // lock (Elements)
            // {
            //     Elements.Add(this);
            // }
            Paused = false;
        }

        public void Stop()
        {
            interval = -1;
            Paused = true;
            //   timer?.Close();
            timer?.Dispose();
            timer = null;
            Next = 0;
        }

        public void Pause()
        {
            Paused = true; if (timer == null) { return; }
            //   timer.Change()
            //    timer.Enabled = false;
        }

        public void Resume()
        {
            Paused = false;
            if (timer == null) { return; }
            //  timer.Enabled = true;
        }

        internal int interval = -1;
        internal long Next = 0;
        public bool Paused { get; private set; } = false;
    }
}
