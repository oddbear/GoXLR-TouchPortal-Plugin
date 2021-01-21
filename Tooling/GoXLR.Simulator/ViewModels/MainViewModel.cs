using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GoXLR.Simulator.Client;
using Microsoft.Extensions.Logging;

namespace GoXLR.Simulator.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ILogger<MainViewModel> _logger;
        private WebSocketClient _client;

#pragma warning disable CS0067 // False positive
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS0067

        public string Profiles { get; set; }

        public string Log { get; set; }

        public MainViewModel(ILogger<MainViewModel> logger)
        {
            _logger = logger;
            Profiles = FetchProfiles();
        }

        private string FetchProfiles()
        {
            var myDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var goXLRProfilesPath = Path.Combine(myDocumentsPath, @"GoXLR\Profiles");

            try
            {
                var profiles = Directory.GetFiles(goXLRProfilesPath).Select(Path.GetFileNameWithoutExtension);

                return string.Join(Environment.NewLine, profiles);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e.ToString());

                var entry = $"Could not read profiles from '{goXLRProfilesPath}', using test profiles instead.";
                _logger.LogInformation(entry);
                Log += $"{DateTime.Now:s} {entry}{Environment.NewLine}";

                return string.Join(Environment.NewLine, "Test profile 1", "Test profile 2", "Test profile 3");
            }
        }

        public async Task ConnectAsync(string serverIp)
        {
            var port = 6805;
            if (serverIp.Contains(":"))
            {
                var segments = serverIp.Split(":");
                serverIp = segments[0].Trim();
                port = int.Parse(segments[1]);
            }
            
            _client = new WebSocketClient(new Uri($"ws://{serverIp}:{port}/?GOXLRApp"));
            _client.OnOpen = (message) =>
            {
                var entry = $"Connected to '{serverIp}:6805'";
                _logger.LogInformation(entry);
                Log += $"{DateTime.Now:s} {entry}{Environment.NewLine}";
            };
            _client.OnClose += (message) =>
            {
                var entry = $"Disconnected from '{serverIp}:6805'";
                _logger.LogInformation(entry);
                Log += $"{DateTime.Now:s} {entry}{Environment.NewLine}";
            };
            _client.OnMessage = ClientOnMessageReceived;

            await _client.StartAsync();
        }

        private async void ClientOnMessageReceived(string json)
        {
            try
            {
                _logger.LogInformation(json);
                Log += $"{DateTime.Now:s} {json}{Environment.NewLine}";

                var document = JsonSerializer.Deserialize<JsonDocument>(json);
                if (document is null)
                    return;

                var root = document.RootElement;
                var propertyAction = root.GetProperty("action").GetString();
                var propertyEvent = root.GetProperty("event").GetString();

                var propertyContext = root.TryGetProperty("context", out var jsonPropertyContext)
                    ? jsonPropertyContext.GetString()
                    : null;

                switch (propertyAction)
                {
                    case "com.tchelicon.goxlr.profilechange" when propertyEvent == "propertyInspectorDidAppear":
                        await SendProfilesResponse(propertyContext);
                        break;
                    case "com.tchelicon.goxlr.profilechange" when propertyEvent == "keyUp":
                        LogProfileChange(json);
                        break;
                    case "com.tchelicon.goxlr.routingtable"  when propertyEvent == "keyUp":
                        LogRouteChange(json);
                        break;
                }
            }
            catch (Exception e)
            {
                var entry = $"Error '{e.Message}'";
                _logger.LogInformation(entry);
                Log += $"{DateTime.Now:s} {entry}{Environment.NewLine}";
            }
        }

        private async Task SendProfilesResponse(string contextId)
        {
            //Sanitize:
            contextId = JsonSerializer.Serialize(contextId);

            var profiles = Profiles
                .Split(Environment.NewLine)
                .Select(profile => profile);

            var profilesStr = JsonSerializer.Serialize(profiles);

            //Build:
            var json = $"{{\"action\":\"com.tchelicon.goXLR.ChangeProfile\",\"context\":{contextId},\"event\":\"sendToPropertyInspector\",\"payload\":{{\"Profiles\":{profilesStr}}}}}";
            
            //Send:
            await _client.SendAsync(json, CancellationToken.None)
                .ConfigureAwait(false);
        }

        private void LogProfileChange(string json)
        {
            var requestedProfile = JsonSerializer.Deserialize<JsonDocument>(json)
                .RootElement
                .GetProperty("payload")
                .GetProperty("SelectedProfile")
                .GetString();

            var entry = $"Profile requested: '{requestedProfile}'";

            _logger.LogInformation(entry);
            Log += $"{DateTime.Now:s} {entry}{Environment.NewLine}";
        }

        private void LogRouteChange(string json)
        {
            var settings = JsonSerializer.Deserialize<JsonDocument>(json)
                .RootElement
                .GetProperty("payload")
                .GetProperty("settings");

            var action = settings.GetProperty("RoutingAction").GetString();
            var input = settings.GetProperty("RoutingInput").GetString();
            var output = settings.GetProperty("RoutingOutput").GetString();

            var entry = $"Route change requested: Action '{action}', Input: '{input}', Output: '{output}'";

            _logger.LogInformation(entry);
            Log += $"{DateTime.Now:s} {entry}{Environment.NewLine}";
        }
        
        public void Disconnect()
        {
            _ = _client?.StopAsync();
        }
    }
}
