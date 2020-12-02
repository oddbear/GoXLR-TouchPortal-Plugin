using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GoXLR.Models.Models;
using GoXLR.Models.Models.Shared;
using Microsoft.Extensions.Logging;

namespace GoXLR.Simulator.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ILogger<MainViewModel> _logger;
        private WatsonWebsocket.WatsonWsClient _client;

#pragma warning disable CS0067 // False positive
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS0067

        public string Profiles { get; set; }

        public string Log { get; set; }

        public MainViewModel(ILogger<MainViewModel> logger)
        {
            _logger = logger;
            var myDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var goXLRProfilesPath = Path.Combine(myDocumentsPath, @"GoXLR\Profiles");
            var profiles = Directory.GetFiles(goXLRProfilesPath).Select(Path.GetFileNameWithoutExtension);

            Profiles = string.Join(Environment.NewLine, profiles);
        }

        public void Connect(string serverIp)
        {
            _client = new WatsonWebsocket.WatsonWsClient(serverIp, 6805, false);
            _client.ServerConnected += (sender, args) =>
            {
                var entry = $"Connected to '{serverIp}:6805'";
                _logger.LogInformation(entry);
                Log += $"{DateTime.Now:s} {entry}{Environment.NewLine}";
            };
            _client.ServerDisconnected += (sender, args) =>
            {
                var entry = $"Disconnected from '{serverIp}:6805'";
                _logger.LogInformation(entry);
                Log += $"{DateTime.Now:s} {entry}{Environment.NewLine}";
            };
            _client.MessageReceived += ClientOnMessageReceived;
            _client.Start();
        }

        private async void ClientOnMessageReceived(object? sender, WatsonWebsocket.MessageReceivedEventArgs args)
        {
            try
            {
                var requestJson = Encoding.UTF8.GetString(args.Data);
                var entry = $"Message from '{args.IpPort}', length '{args.Data.Length}', data: {requestJson}";
                _logger.LogInformation(entry);
                Log += $"{DateTime.Now:s} {entry}{Environment.NewLine}";

                var requestBase = JsonSerializer.Deserialize<RequestModelBase>(requestJson);
                if (Match(requestBase, "com.tchelicon.goxlr.profilechange", "propertyInspectorDidAppear"))
                {
                    await SendProfilesResponse();
                    return;
                }

                if (Match(requestBase, "com.tchelicon.goxlr.profilechange", "keyUp"))
                {
                    LogProfileChange(requestJson);
                    return;
                }

                if (Match(requestBase, "com.tchelicon.goxlr.routingtable", "keyUp"))
                {
                    LogRouteChange(requestJson);
                    return;
                }
            }
            catch (Exception e)
            {
                var entry = $"Error '{e.Message}'";
                _logger.LogInformation(entry);
                Log += $"{DateTime.Now:s} {entry}{Environment.NewLine}";
            }
        }

        private async Task SendProfilesResponse()
        {
            var profiles = Profiles.Split(Environment.NewLine);
            var responseModel = GetProfilesResponse.Create(profiles);
            var responseJson = JsonSerializer.Serialize(responseModel);
            var responseData = Encoding.UTF8.GetBytes(responseJson);
            await _client.SendAsync(responseData, CancellationToken.None)
                .ConfigureAwait(false);
        }

        private void LogProfileChange(string requestJson)
        {
            var request = JsonSerializer.Deserialize<SetProfileRequest>(requestJson);
            var requestedProfile = request?.Payload?.Settings?.SelectedProfile;
            if (requestedProfile == null)
                throw new InvalidOperationException("No Selected Profile payload.");

            var entry = $"Profile requested: '{requestedProfile}'";
            _logger.LogInformation(entry);
            Log += $"{DateTime.Now:s} {entry}{Environment.NewLine}";
        }

        private void LogRouteChange(string requestJson)
        {
            var request = JsonSerializer.Deserialize<SetRoutingRequest>(requestJson);

            var settings = request?.Payload?.Settings;
            if (settings == null)
                throw new InvalidOperationException("No Settings payload.");

            var action = settings.RoutingAction;
            var input = settings.RoutingInput;
            var output = settings.RoutingOutput;
            var entry = $"Route change requested: Action '{action}', Input: '{input}', Output: '{output}'";
            _logger.LogInformation(entry);
            Log += $"{DateTime.Now:s} {entry}{Environment.NewLine}";
        }

        private static bool Match(RequestModelBase model, string action, string @event)
            => model != null && model.Action == action && model.Event == @event;

        public void Disconnect()
        {
            if (_client != null)
            {
                _client.Stop();
                _client.Dispose();
            }
        }
    }
}
