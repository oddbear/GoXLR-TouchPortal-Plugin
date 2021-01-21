using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GoXLR.Simulator.Client
{
    public class WebSocketClient
    {
        private readonly Uri _uri;

        private ClientWebSocket _client;
        private Task _handleWorker;

        public Action<string> OnMessage { get; set; }
        public Action<string> OnOpen { get; set; }
        public Action<string> OnClose { get; set; }
        public Action<Exception> OnError { get; set; }

        public WebSocketClient(Uri uri)
        {
            _uri = uri;
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            //TODO: Cleanup this code. But it's not THAT important, as this is just tooling.
            _client = new ClientWebSocket();
            await _client.ConnectAsync(_uri, cancellationToken);

            OnOpen?.Invoke($"Connected. {_uri}");

            //Start handler for receiving messages:
            _handleWorker = Task.Run(() => Handle(cancellationToken), cancellationToken);
        }

        public async Task SendAsync(string message, CancellationToken cancellationToken = default)
        {
            try
            {
                var arraySegment = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
                await _client.SendAsync(arraySegment, WebSocketMessageType.Text, true, cancellationToken);
            }
            catch (Exception e)
            {
                OnError?.Invoke(e);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationToken);
        }

        private async Task Handle(CancellationToken cancellationToken)
        {
            try
            {
                var buffer = new ArraySegment<byte>(new byte[256]);
                var messageBuilder = new StringBuilder();

                while (_client.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var receiveResult = await _client.ReceiveAsync(buffer, cancellationToken);

                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        await _client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationToken);
                    }
                    else if (receiveResult.MessageType == WebSocketMessageType.Text)
                    {
                        var messagePart = System.Text.Encoding.UTF8.GetString(buffer[..receiveResult.Count]);
                        messageBuilder.Append(messagePart);

                        if (receiveResult.EndOfMessage)
                        {
                            var message = messageBuilder.ToString();
                            OnMessage?.Invoke(message);

                            //Reset message builder.
                            messageBuilder = new StringBuilder();
                        }
                    }
                }
            }
            catch (WebSocketException ex)
                when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
            {
                OnError?.Invoke(ex);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
            }

            OnClose?.Invoke($"Closed: {_uri}");
        }
    }
}
