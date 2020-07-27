using System.Threading;
using System.Threading.Tasks;

namespace Concurrency
{
    public class SpinWait
    {
        public void StartBackgroundWork(CancellationToken token)
        {
            _ = Task.Run(()=>EndlessLoop(token), token);
        }

        private void EndlessLoop(CancellationToken token)
        {
            var wait = new System.Threading.SpinWait();

            while (!token.IsCancellationRequested)
            {
                //do smth
                wait.SpinOnce(100);
            }
        }
    }
}