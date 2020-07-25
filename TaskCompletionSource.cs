using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Concurrency.TaskCompletionSource
{
    public sealed class WampClient
    {
        private readonly BlockingCollection<(MessageType type, object payload, string uri, TaskCompletionSource<object> tcs, Type responseType)> _queue = new BlockingCollection<(MessageType type, object payload, string uri, TaskCompletionSource<object> tcs, Type responseType)>();
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
        private readonly ClientWebSocket _socket = new ClientWebSocket();
        private readonly ConcurrentDictionary<string, (TaskCompletionSource<object>, Type)> _ongoingTransactions = new ConcurrentDictionary<string, (TaskCompletionSource<object>, Type)>();
        
        public WampClient(Uri uri)
        {
            _socket.ConnectAsync(uri, _cancellation.Token);
            _ = Task.Run(()=>SendLoop(_cancellation.Token), _cancellation.Token);
            _ = Task.Run(()=>ReceiveLoop(_cancellation.Token), _cancellation.Token);
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            while(!token.IsCancellationRequested)
            {
                const int receiveBufferSize = 4096;
                var buffer = new byte[receiveBufferSize];
                var result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                if (result.MessageType == WebSocketMessageType.Close) break;
                var json = JArray.Parse(Encoding.UTF8.GetString(buffer));
                var (messageType, messageId, payloadJson) = (Enum.Parse<MessageType>(json[0].ToString()), json[1].ToString(), json[2].ToString());
                var (tcs, responseType) = _ongoingTransactions[messageId];
                var payload = JsonConvert.DeserializeObject(payloadJson, responseType);
                tcs.SetResult(payload);
            }
        }

        private async Task SendLoop(CancellationToken token)
        {
            while(!_queue.IsCompleted && !token.IsCancellationRequested)
            {
                if (_queue.TryTake(out var item, 1000))
                {
                    var (type, payload, uri, tcs, responseType) = item;
                    var messageId = Guid.NewGuid().ToString();
                    var json = JsonConvert.SerializeObject(new[]{type, messageId, uri, payload});
                    var bytesCount = Encoding.UTF8.GetByteCount(json);
                    var buffer = new byte[bytesCount];
                    Encoding.UTF8.GetBytes(json, 0, json.Length, buffer, 0);
                    await _socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, token);
                    _ongoingTransactions.AddOrUpdate(messageId, _ => (tcs, responseType),
                        (_, __) => (tcs, responseType));
                }
            }
        }

        public async Task<TResponse> Send<TRequest, TResponse>(TRequest request, string uri)
        {
            var source = new TaskCompletionSource<object>();
            _queue.Add((MessageType.Call, request, uri, source, typeof(TResponse)));
            var result = await source.Task;
            return (TResponse) result;
        }
    }

    internal enum MessageType
    {
        Call = 0,
        CallResult = 1
    }
}