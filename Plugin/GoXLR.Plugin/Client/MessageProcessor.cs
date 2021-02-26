﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using GoXLR.Plugin.Models;
using Microsoft.Extensions.Logging;

namespace GoXLR.Plugin.Client
{
    public class MessageProcessor
    {
        private readonly ILogger _logger;
        private readonly AppSettings _settings;

        private readonly Encoding _encoding;
        private readonly Socket _touchPortalSocket;
        private readonly ManualResetEvent _waitForInfo;
        private StreamWriter _streamWriter;
        private StreamReader _streamReader;
        private Thread _listenerThread;

        public Action<InfoMessage> OnInfo { get; set; }
        public Action<Exception> OnDisconnect { get; set; }
        public Action<ListChangeMessage> OnListChange { get; set; }
        public Action<ActionMessage> OnActionEvent { get; set; }

        public MessageProcessor(ILogger logger, AppSettings settings)
        {
            _logger = logger;
            _settings = settings;

            _encoding = Encoding.ASCII;
            _touchPortalSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _waitForInfo = new ManualResetEvent(false);
        }

        public bool UpdateState(string stateId, string value)
        {
            if (string.IsNullOrWhiteSpace(stateId) ||
                string.IsNullOrWhiteSpace(value))
                return false;

            var message = new Dictionary<string, object>
            {
                ["type"] = "stateUpdate",
                ["id"] = _settings.PluginId + stateId,
                ["value"] = value,
            };

            var json = JsonSerializer.Serialize(message);

            _logger.LogInformation(json);

            return Send(json);
        }

        public bool UpdateChoice(string listId, string[] values, string instanceId = null)
        {
            if (string.IsNullOrWhiteSpace(listId))
                return false;

            var message = new Dictionary<string, object>
            {
                ["type"] = "choiceUpdate",
                ["id"] = _settings.PluginId + listId,
                ["value"] = values ?? Array.Empty<string>()
            };

            if (instanceId != null)
                message["instanceId"] = instanceId;

            var json = JsonSerializer.Serialize(message);

            _logger.LogInformation(json);

            return Send(json);
        }

        public void Connect()
        {
            try
            {
                //Connect
                var ipAddress = IPAddress.Parse(_settings.ServerIp);
                var socketAddress = new IPEndPoint(ipAddress, _settings.ServerPort);
                _touchPortalSocket.Connect(socketAddress);

                //Setup streams:
                _streamWriter = new StreamWriter(new NetworkStream(_touchPortalSocket), _encoding) { AutoFlush = true };
                _streamReader = new StreamReader(new NetworkStream(_touchPortalSocket), _encoding);

                //Send pair message:

                //Create listener thread:
                _listenerThread = new Thread(ListenerThreadSync) { IsBackground = false };
                _listenerThread.Start();

                //Wait for info:
                if (TimeSpan.TryParse(_settings.InfoMessageTimeout, out var timeout) && timeout > TimeSpan.Zero)
                {
                    SendPair();
                    //Success true if message was received in time. False if timeout occurred.
                    var success = _waitForInfo.WaitOne(timeout);
                    _logger.LogInformation("InfoMessage success: " + success);
                }
            }
            catch (SocketException)
            {
                //ignored
            }
        }

        private void SendPair()
        {
            var json = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["type"] = "pair",
                ["id"] = _settings.PluginId
            });

            Send(json);
        }

        public bool Send(string json)
        {
            try
            {
                _streamWriter.WriteLine(json);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
        }

        private void OnMessage(string json)
        {
            var jsonOptions = new JsonSerializerOptions {PropertyNamingPolicy = JsonNamingPolicy.CamelCase};
            var baseMessage = JsonSerializer.Deserialize<BaseMessage>(json, jsonOptions);
            switch (baseMessage?.Type)
            {
                case "info":
                    _waitForInfo?.Set();
                    var infoMessage = JsonSerializer.Deserialize<InfoMessage>(json, jsonOptions);
                    //Can contain settings from 2.3...
                    OnInfo?.Invoke(infoMessage);
                    break;
                case "closePlugin":
                    throw new IOException("Close Message Received");
                case "listChange":
                    var listChangeMessage = JsonSerializer.Deserialize<ListChangeMessage>(json, jsonOptions);
                    OnListChange?.Invoke(listChangeMessage);
                    break;
                case "broadcast":
                case "settings":
                case "down":
                case "up":
                    //Not needed for this plugin.
                    break;
                case "action":
                    var actionMessage = JsonSerializer.Deserialize<ActionMessage>(json, jsonOptions);
                    OnActionEvent?.Invoke(actionMessage);
                    break;
            }
        }
        
        private void ListenerThreadSync()
        {
            while (true)
            {
                try
                {
                    var socketMessage = _streamReader.ReadLine()
                                        ?? throw new IOException("Server Socket Closed");

                    _logger.LogInformation(socketMessage);

                    this.OnMessage(socketMessage);
                }
                catch (IOException exception)
                {
                    Close(exception);
                    return;
                }
                catch
                {
                    //Any other exception, ignore and retry.
                }
            }
        }

        public void Close(Exception exception)
        {
            _logger.LogInformation("Closing");

            _listenerThread?.Interrupt();
            _streamWriter?.Close();
            _streamReader?.Close();
            _touchPortalSocket?.Close();

            OnDisconnect?.Invoke(exception);
        }
    }
}