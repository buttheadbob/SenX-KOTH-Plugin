using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;

namespace SenX_KOTH_Plugin.Bot
{
    internal sealed class GatewayConnection : IDisposable
    {
        private static readonly Logger Log = LogManager.GetLogger("KoTH Plugin => Gateway");

        private readonly string _token;
        private readonly int[] _backoff = { 1, 2, 4, 8, 16, 30, 60, 120 };
        private ClientWebSocket? _ws;
        private Timer? _heartbeatTimer;
        private CancellationTokenSource? _receiveCts;
        private int _heartbeatInterval;
        private int? _lastSequence;
        private string? _sessionId;
        private int _reconnectAttempt;
        private bool _disposed;

        public bool IsConnected { get; private set; }

        public GatewayConnection(string token)
        {
            _token = token;
        }

        public async Task StartAsync()
        {
            _disposed = false;
            _reconnectAttempt = 0;
            await ConnectAsync();
        }

        private async Task ConnectAsync()
        {
            try
            {
                _ws = new ClientWebSocket();
                await _ws.ConnectAsync(new Uri("wss://gateway.discord.gg/?v=10&encoding=json"), CancellationToken.None);

                _receiveCts = new CancellationTokenSource();
                _ = ReceiveLoopAsync(_receiveCts.Token);

                await SendPayloadAsync(new GatewayPayload { Op = 2, D = new IdentifyPayload { Token = _token, Intents = 0 } });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Gateway connection failed.");
                await ReconnectAsync();
            }
        }

        private async Task ReconnectAsync()
        {
            Cleanup();

            int delay = _backoff[Math.Min(_reconnectAttempt, _backoff.Length - 1)];
            _reconnectAttempt++;
            Log.Info("Gateway reconnecting in " + delay + "s (attempt " + _reconnectAttempt + ")");

            await Task.Delay(TimeSpan.FromSeconds(delay));
            await ConnectAsync();
        }

        private async Task ReceiveLoopAsync(CancellationToken ct)
        {
            var buffer = new byte[8192];
            var messageBuffer = new MemoryStream();

            try
            {
                while (!ct.IsCancellationRequested && _ws?.State == WebSocketState.Open)
                {
                    var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                    messageBuffer.Write(buffer, 0, result.Count);

                    if (result.EndOfMessage)
                    {
                        var json = Encoding.UTF8.GetString(messageBuffer.ToArray(), 0, (int)messageBuffer.Length);
                        messageBuffer.SetLength(0);
                        await ProcessMessageAsync(json);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Log.Error(ex, "Gateway receive loop error.");
                if (!_disposed) await ReconnectAsync();
            }
        }

        private async Task ProcessMessageAsync(string json)
        {
            try
            {
                var payload = JsonConvert.DeserializeObject<GatewayPayload>(json);
                if (payload == null) return;

                _lastSequence = payload.S ?? _lastSequence;

                switch (payload.Op)
                {
                    case 10: // Hello
                        var hello = JsonConvert.DeserializeObject<HelloData>(payload.D?.ToString() ?? "{}");
                        if (hello != null)
                        {
                            _heartbeatInterval = hello.HeartbeatInterval;
                            StartHeartbeat();
                        }
                        break;

                    case 0: // Dispatch
                        if (payload.T == "READY")
                        {
                            var ready = JsonConvert.DeserializeObject<ReadyData>(payload.D?.ToString() ?? "{}");
                            _sessionId = ready?.SessionId;
                            _reconnectAttempt = 0;
                            IsConnected = true;
                            Log.Info("Gateway connected. Session: " + _sessionId);
                        }
                        break;

                    case 7: // Reconnect
                        Log.Info("Gateway requested reconnect (op 7).");
                        await ReconnectAsync();
                        break;

                    case 9: // Invalid Session
                        var canResume = payload.D?.ToString() == "true";
                        if (canResume && _sessionId != null)
                        {
                            await SendPayloadAsync(new GatewayPayload { Op = 6, D = new ResumePayload { Token = _token, SessionId = _sessionId, Seq = _lastSequence } });
                        }
                        else
                        {
                            Log.Info("Invalid session — full re-identify.");
                            await Task.Delay(2000);
                            _sessionId = null;
                            await ConnectAsync();
                        }
                        break;

                    case 11: // Heartbeat ACK
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Gateway message processing error.");
            }
        }

        private void StartHeartbeat()
        {
            _heartbeatTimer?.Dispose();
            _heartbeatTimer = new Timer(_ =>
            {
                try
                {
                    var payload = new HeartbeatPayload(_lastSequence);
                    var json = JsonConvert.SerializeObject(payload);
                    var bytes = Encoding.UTF8.GetBytes(json);
                    _ws?.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch { }
            }, null, _heartbeatInterval, _heartbeatInterval);
        }

        private async Task SendPayloadAsync(object payload)
        {
            if (_ws?.State != WebSocketState.Open) return;
            try
            {
                var json = JsonConvert.SerializeObject(payload);
                var bytes = Encoding.UTF8.GetBytes(json);
                await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Gateway send failed.");
            }
        }

        private void Cleanup()
        {
            _receiveCts?.Cancel();
            _receiveCts?.Dispose();
            _receiveCts = null;
            _heartbeatTimer?.Dispose();
            _heartbeatTimer = null;
            try { _ws?.Abort(); _ws?.Dispose(); } catch { }
            _ws = null;
            IsConnected = false;
        }

        public void Dispose()
        {
            _disposed = true;
            Cleanup();
        }
    }
}
