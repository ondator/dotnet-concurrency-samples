using System;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Concurrency.EventLoop
{
    public class SyncHandler : IDisposable
    {
        private readonly IDisposable _subscription;

        public SyncHandler(RemoteBackendClient client)
        {
            _subscription = client.Inbox.Subscribe(Handle);
        }

        /// <summary>
        /// event handling logic should be here
        /// </summary>
        private void Handle(MyEvent e)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _subscription.Dispose();
        }
    }

    public class RemoteBackendClient : IDisposable
    {
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
        public RemoteBackendClient()
        {
            _ = Task.Run(() => EndlessLoop(_cancellation.Token), _cancellation.Token);
        }

        private async Task EndlessLoop(CancellationToken token)
        {
            while(!token.IsCancellationRequested)
            {
                var @event = await ReceiveData();
                Inbox.OnNext(@event);
            }
        }

        /// <summary>
        /// receiving logic should be here
        /// </summary>
        private Task<MyEvent> ReceiveData()
        {
            throw new NotImplementedException();
        }

        public Subject<MyEvent> Inbox { get; } = new Subject<MyEvent>();
        
        public void Dispose()
        {
            Inbox.OnCompleted();
            Inbox.Dispose();
            
            _cancellation.Cancel();
        }
    }

    public class MyEvent
    {
    }
}