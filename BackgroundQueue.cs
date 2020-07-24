using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Concurrency.BackgroundQueue
{
    public class MySyncDomainObject
    {
        public void DoSomeStuff(RemoteBackendClient client)
        {
            client.CallApi("hello!");
        }
    }

    public class RemoteBackendClient : IDisposable
    {
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
        private readonly BlockingCollection<string> _queue = new BlockingCollection<string>();

        public RemoteBackendClient()
        {        
            _ = Task.Run(()=>EndlessLoop(_cancellation.Token), _cancellation.Token);
        }

        private async Task EndlessLoop(CancellationToken token)
        {
            while(!_queue.IsCompleted && !token.IsCancellationRequested)
            {
                if (_queue.TryTake(out var item, 1000))
                {
                    await CallRemoteApi(item);
                }
            }
        }

        /// <summary>
        /// here should be real API call
        /// </summary>
        private Task CallRemoteApi(string message)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// just add message to queue
        /// </summary>
        public void CallApi(string message)
        {
            _queue.Add(message);
        }

        public void Dispose()
        {
            _cancellation.Cancel();
        }
    }
}