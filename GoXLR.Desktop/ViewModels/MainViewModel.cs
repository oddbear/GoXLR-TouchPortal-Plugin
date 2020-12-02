using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GoXLR.Desktop.ViewModels.Models;
using GoXLR.Models.Configuration;
using GoXLR.Models.Models;
using GoXLR.Models.Models.Payloads;
using Microsoft.Extensions.Logging;
using WatsonWebsocket;

namespace GoXLR.Desktop.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ILogger<MainViewModel> _logger;
        private readonly WatsonWebsocket.WatsonWsServer _server;

#pragma warning disable CS0067 // False positive
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS0067

        public List<string> Clients { get; set; }
        public string SelectedClient { get; set; }

        private IEnumerable<ProfileModel> _allProfiles { get; set; }
        public IEnumerable<ProfileModel> Profiles { get; set; }
        public ProfileModel SelectedProfile { get; set; }

        public string[] Inputs { get; set; }
        public string SelectedInput { get; set; }

        public string[] Outputs { get; set; }
        public string SelectedOutput { get; set; }

        public string[] Actions { get; set; }
        public string SelectedAction { get; set; }

        public string Log { get; set; }

        public MainViewModel(ILogger<MainViewModel> logger)
        {
            _logger = logger;

            _allProfiles = Enumerable.Empty<ProfileModel>();

            Clients = new List<string>();

            Inputs = Routing.Inputs;
            SelectedInput = Inputs.First();

            Outputs = Routing.Outputs;
            SelectedOutput = Outputs.First();

            Actions = Routing.Actions;
            SelectedAction = Actions.First();

            _server = new WatsonWsServer("127.0.0.1", 6805, false);
            _server.ClientConnected += ServerOnClientConnected;
            _server.ClientDisconnected += ServerOnClientDisconnected;
            _server.MessageReceived += ServerOnMessageReceived;
            _server.ServerStopped += (sender, args) => _logger.LogInformation("Server Stopped");
            _server.Start();
        }

        public async Task GetProfiles(CancellationToken cancellationToken = default)
        {
            var model = GetProfilesRequest.Create();
            var json = JsonSerializer.Serialize(model);

            await _server.SendAsync(SelectedClient, json, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task SetProfile(CancellationToken cancellationToken = default)
        {
            var profile = SelectedProfile?.ProfileName;
            if (string.IsNullOrWhiteSpace(profile))
                return;

            var model = SetProfileRequest.Create(profile);
            var json = JsonSerializer.Serialize(model);

            await _server.SendAsync(SelectedClient, json, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task SetRouting(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(SelectedAction)
             || string.IsNullOrWhiteSpace(SelectedInput)
             || string.IsNullOrWhiteSpace(SelectedOutput))
                return;

            var model = SetRoutingRequest.Create(new SetRoutingPayload.SetRoutingSettings
            {
                RoutingAction = SelectedAction,
                RoutingInput = SelectedInput,
                RoutingOutput = SelectedOutput
            });

            var json = JsonSerializer.Serialize(model);
            await _server.SendAsync(SelectedClient, json, cancellationToken)
                .ConfigureAwait(false);
        }

        public void UpdateProfiles()
        {
            Profiles = _allProfiles
                .Where(profile => profile.ClientAddress == SelectedClient)
                .ToList();

            SelectedProfile = Profiles.FirstOrDefault();
        }

        private void ServerOnMessageReceived(object? sender, MessageReceivedEventArgs args)
        {
            try
            {
                var requestJson = Encoding.UTF8.GetString(args.Data);
                var entry = $"Message from '{args.IpPort}', length '{args.Data.Length}', data: {requestJson}";
                _logger.LogInformation(entry);
                Log += $"{DateTime.Now:s} {entry}{Environment.NewLine}";

                var response = JsonSerializer.Deserialize<GetProfilesResponse>(requestJson);
                if (response is null)
                    return;

                //Replace all the profiles from this client:
                var profileNames = response.Payload.Profiles;

                var profiles = profileNames
                    .Select(profile => new ProfileModel { ClientAddress = args.IpPort, ProfileName = profile });

                var allProfiles = _allProfiles
                    .Where(profile => profile.ClientAddress != args.IpPort)
                    .Union(profiles)
                    .ToArray();

                _allProfiles = allProfiles;
                UpdateProfiles();
            }
            catch (Exception e)
            {
                var entry = $"Error '{e.Message}'";
                _logger.LogInformation(entry);
                Log += $"{DateTime.Now:s} {entry}{Environment.NewLine}";
            }
        }

        private void ServerOnClientConnected(object? sender, ClientConnectedEventArgs args)
        {
            var entry = $"Client connected: '{args.IpPort}'";
            _logger.LogInformation(entry);
            Log += $"{DateTime.Now:s} {entry}{Environment.NewLine}";

            Clients.Add(args.IpPort);
            SelectedClient ??= args.IpPort;
            OnPropertyChanged(nameof(Clients));
        }

        private void ServerOnClientDisconnected(object? sender, ClientDisconnectedEventArgs args)
        {
            var entry = $"Client disconnected: {args.IpPort}";
            _logger.LogInformation(entry);
            Log += $"{DateTime.Now:s} {entry}{Environment.NewLine}";

            Clients.Remove(args.IpPort);
            if (SelectedClient == args.IpPort)
            {
                SelectedClient = null;
            }
            OnPropertyChanged(nameof(Clients));
        }

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    //If ObservableCollection or similar: Application.Current.Dispatcher.Invoke(() => Clients.Add(...));
}
