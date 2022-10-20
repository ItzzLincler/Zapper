using Microsoft.VisualStudio.Threading;
using System.Diagnostics;
using Zapper.Api.Models;

namespace Zapper.Api.Services
{
    public class PeriodicScrapingTask : IDisposable
    {
        public IScraper Scraper { get; private set; }
        private readonly AsyncQueue<IEnumerable<IScraper>> jobQueue;
        private TimeSpan timeSpan;
        private CancellationTokenSource tokenSource;
        private Task task;
        private Stopwatch watch = new Stopwatch();

        public PeriodicScrapingTask(AsyncQueue<IEnumerable<IScraper>> jobQueue, IScraper scraper, TimeSpan timeSpan)
        {
            this.timeSpan = timeSpan;
            this.Scraper = scraper;
            this.jobQueue = jobQueue;
            this.tokenSource = new CancellationTokenSource();
            Start(tokenSource.Token);
        }

        public TimeSpan RemainingTime() => timeSpan - watch.Elapsed;

        public void Start(CancellationToken token)
        {
            task = Task.Run(async () =>
            {
                var timer = new PeriodicTimer(timeSpan);
                do
                {
                    //Console.WriteLine("Period task looped");
                    watch.Restart();
                    jobQueue.Enqueue(new[] { Scraper });
                }
                while (!token.IsCancellationRequested && await timer.WaitForNextTickAsync(token));
                //Console.WriteLine("Period task ended");
            }, token);
        }

        public void UpdatePeriod(TimeSpan newTimeSpan)
        {
            Stop();
            tokenSource = new CancellationTokenSource();
            timeSpan = newTimeSpan;
            Start(tokenSource.Token);
        }

        public void Stop()
        {
            tokenSource.Cancel();
        }

        public void Dispose()
        {
            Stop();
        }

        public ScrapedProductSource GetScraperSource() => Scraper.GetSource();


    }
}
