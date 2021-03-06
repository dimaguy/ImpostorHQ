﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using Fleck;
using ImpostorHQ.Core.Api.Message;
using ImpostorHQ.Core.Config;
using ImpostorHQ.Core.Cryptography;
using ImpostorHQ.Core.Extensions;
using ImpostorHQ.Core.Logs;
using Microsoft.Extensions.Logging;

namespace ImpostorHQ.Core.Api
{
    public class WebApi : IDisposable, IWebApi
    {
        private readonly ICryptoManager _crypto;

        private readonly ILogger<IWebApi> _logger;

        private readonly ILogManager _logManager;

        private readonly IMessageFactory _messageFactory;

        private readonly IMetricsProvider _metricsProvider;

        private readonly List<Password> _passwords;

        private readonly WebSocketServer _server;

        private readonly ConcurrentDictionary<IWebSocketConnection, int> _timeouts;

        private readonly Timer _timer1S;

        private readonly IWebApiUserFactory _userFactory;

        private readonly ConcurrentDictionary<IWebSocketConnection, WebApiUser> _users;
        
        public WebApi(
            ILogger<IWebApi> logger, 
            IMessageFactory messageFactory, 
            ImpostorHqConfig config,
            IPasswordFile passwordProvider, 
            ICryptoManager crypto, 
            IWebApiUserFactory webApiUserFactory,
            IMetricsProvider metricsProvider,
            ILogManager logManager)
        {
            _logger = logger;
            _messageFactory = messageFactory;
            _passwords = passwordProvider.Passwords;
            _crypto = crypto;
            _userFactory = webApiUserFactory;
            _metricsProvider = metricsProvider;
            _logManager = logManager;

            _users = new ConcurrentDictionary<IWebSocketConnection, WebApiUser>();
            _timeouts = new ConcurrentDictionary<IWebSocketConnection, int>();

            _server = new WebSocketServer($"ws://{config.Host}:{config.ApiPort}");

            _timer1S = new Timer(1000) {AutoReset = true};
            _timer1S.Elapsed += _timer1s_Elapsed;
            Start();
        }

        public ICollection<WebApiUser> Users => _users.Values;

        private void _timer1s_Elapsed(object sender, object args)
        {
            foreach (var (key, _) in _timeouts)
            {
                key.Close();
                _logger.LogWarning("ImpostorHQ: Api timeout for {Address}", key.ConnectionInfo.ClientIpAddress);
                _timeouts.Remove(key, out _);

                _logManager.LogInformation(
                    $"User {key.ConnectionInfo.ClientIpAddress} timed out on log-in.");
            }

            foreach (var webApiUser in _users)
            {
                _ = webApiUser.Value.Write(_messageFactory.CreateHeartBeat(_metricsProvider.GameCount,
                    _metricsProvider.PlayerCount, _metricsProvider.CpuUsagePercent,
                    (int) _metricsProvider.MemoryUsageBytes));
            }
        }

        private void Start()
        {
            _timer1S.Start();
            _server.Start(socket =>
            {
                //a client connects.
                socket.OnOpen += () => OnOpen(socket);
            });
        }

        private void OnOpen(IWebSocketConnection socket)
        {
            socket.OnMessage += async s =>
            {
                if (!_users.TryGetValue(socket, out var user))
                {
                    if (_crypto.TryDecrypt(s, out var decryptResult) && _passwords.Select(x=>x.User).Contains(decryptResult.password.User))
                    {
                        var item = _userFactory.Create(socket, decryptResult.password);
                        _users.TryAdd(socket, item);

                        socket.OnClose += () => { _users.TryRemove(socket, out _); };

                        item.OnDisconnected += RemoveUser;

                        await item.Write(_messageFactory.CreateLoginApiAccepted(), false);

                        await _logManager.LogInformation(
                            $"User {decryptResult.password.User} logged in.");
                    }
                    else
                    {
                        RemoveFromTimeout();
                        await socket.Send(_messageFactory.CreateLoginApiRejected());
                        socket.Close();
                    }
                }
                else
                {
                    var data = _crypto.Decrypt(s, user.Password.ToString());
                    if (string.IsNullOrEmpty(data))
                    {
                        return;
                    }

                    ApiMessage message;
                    try
                    {
                        message = JsonSerializer.Deserialize<ApiMessage>(data);
                    }
                    catch
                    {
                        _logger.LogWarning("ImpostorHQ Danger: user {Address} is sending malformed API messages.",
                            socket.ConnectionInfo.ClientIpAddress);
                        await user.Kick();
                        RemoveUser();
                        return;
                    }

                    var (success, exception) = await user.HandleMessage(message);

                    if (!success)
                    {
                        _logger.LogWarning("ImpostorHQ Danger: user {Address} tried to execute an invalid operation.", socket.ConnectionInfo.ClientIpAddress);
                        await user.Kick();
                        RemoveUser();

                        await _logManager.LogError(
                            $"User {socket.ConnectionInfo.ClientIpAddress} created error on HandleMessage.", exception);
                    }
                }
            };

            void RemoveFromTimeout()
            {
                _timeouts.TryRemove(socket, out _);
            }

            void RemoveUser()
            {
                _users.TryRemove(socket, out _);
                socket.Close();
            }
        }

        public ValueTask<int> BroadcastAsync(ApiMessage message) => BroadcastAsync(message.Serialize());

        public async ValueTask<int> BroadcastAsync(string message)
        {
            var successes = 0;
            foreach (var webApiUser in _users)
            {
                if (await webApiUser.Value.Write(message))
                {
                    successes++;
                }
            }

            return successes;
        }

        public void Dispose()
        {
            BroadcastAsync(_messageFactory.CreateKick("Impostor Shutting down.", "API")).AsTask().Wait();
            _timer1S.Dispose();
            _server.Dispose();
        }
    }

    public interface IWebApi
    {
        ICollection<WebApiUser> Users { get; }

        ValueTask<int> BroadcastAsync(ApiMessage message);

        ValueTask<int> BroadcastAsync(string message);
    }
}