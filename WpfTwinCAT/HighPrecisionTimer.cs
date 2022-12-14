using System;
using System.Threading;
using System.Threading.Tasks;


namespace WpfTwinCAT
{
    public class HighPrecisionTimer : IDisposable
    {
        public class TickEventArgs : EventArgs
        {
            public TimeSpan Duration { get; private set; }
            public long TotalTicks { get; private set; }

            public TickEventArgs(TimeSpan totalDuration, long totalTicks)
            {
                this.Duration = totalDuration;
                this.TotalTicks = totalTicks;
            }
        }

        
        public event EventHandler<TickEventArgs> Tick;
        protected CancellationTokenSource cancelSource;

        public HighPrecisionTimer(int interval)
        {
            if (interval < 1)
            {
                throw new ArgumentOutOfRangeException();
            }
            //System.Diagnostics.Trace.Assert(interval >= 10, "Not reliable/tested, may use too much CPU");

            cancelSource = new CancellationTokenSource();         

            var watch = System.Diagnostics.Stopwatch.StartNew();
            long durationMs = 0;
            long totalTicks = 0;
            long nextStop = interval;
           
            var task = new Task(() =>
            {
                while (!this.cancelSource.IsCancellationRequested)
                {
                    long msLeft = nextStop - watch.ElapsedMilliseconds;
                    if (msLeft <= 0)
                    {
                        durationMs = watch.ElapsedMilliseconds;
                        totalTicks = durationMs / interval;

                        Tick?.Invoke(this, new TickEventArgs(TimeSpan.FromMilliseconds(durationMs), totalTicks));
                        
                        // Calculate when the next stop is. If we're too slow on the trigger then we'll skip ticks
                        nextStop = interval * (watch.ElapsedMilliseconds / interval + 1);
                    }
                    else if (msLeft < 16)
                    {
                        System.Threading.SpinWait.SpinUntil(() => watch.ElapsedMilliseconds >= nextStop);
                        continue;
                    }

                    System.Threading.Thread.Sleep(1);
                }
            }, cancelSource.Token, TaskCreationOptions.LongRunning);

            task.Start();
        }

        public void Dispose()
        {
            this.cancelSource.Cancel();
        }
    }
}
