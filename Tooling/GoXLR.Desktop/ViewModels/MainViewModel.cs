using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using GoXLR.Desktop.ViewModels.Models;
using GoXLR.Shared;
using GoXLR.Shared.Models;
using Microsoft.Extensions.Logging;

namespace GoXLR.Desktop.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ILogger<MainViewModel> _logger;
        private readonly GoXLRServer _server;

#pragma warning disable CS0067 // False positive
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS0067

        public List<ClientIdentifier> Clients { get; set; }
        public ClientIdentifier SelectedClient { get; set; }

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

        public MainViewModel(ILogger<MainViewModel> logger,
            GoXLRServer server)
        {
            _logger = logger;

            _allProfiles = Enumerable.Empty<ProfileModel>();

            Clients = new List<ClientIdentifier>();

            Inputs = new [] { "Mic", "Chat", "Music", "Game", "Console", "Line In", "System", "Samples" };
            SelectedInput = Inputs.First();

            Outputs = new [] { "Headphones", "Broadcast Mix", "Line Out", "Chat Mic", "Sampler" };
            SelectedOutput = Outputs.First();

            Actions = new [] { "Turn On", "Turn Off", "Toggle" };
            SelectedAction = Actions.First();
            
            _server = server;

            _server.FetchedProfilesEvent = UpdateProfiles;
            _server.UpdateConnectedClientsEvent = UpdateClientState;
        }

        public void GetProfiles()
        {
            var client = SelectedClient;

            var instanceId = $"{client.ClientIpAddress}:{client.ClientPort}";

            _server.FetchProfiles(client, instanceId);
        }

        public void SetProfile()
        {
            var client = SelectedClient;

            var profile = SelectedProfile?.ProfileName;

            _server.SetProfile(client, profile);
        }

        public void SetRouting()
        {
            var client = SelectedClient;

            var action = SelectedAction;
            var input = SelectedInput;
            var output = SelectedOutput;

            _server.SetRouting(client, action, input, output);
        }

        public void UpdateProfiles()
        {
            Profiles = _allProfiles
                .Where(profile => profile.ClientIdentifier == SelectedClient)
                .ToList();

            SelectedProfile = Profiles.FirstOrDefault();
        }
        
        public void UpdateProfiles(FetchedProfilesMessage message)
        {
            var identifier = message.ClientIdentifier;
            var profiles = message.Profiles;

            //Replace all the profiles from this client:
            var allProfiles = _allProfiles
                .Where(profile => profile.ClientIdentifier != identifier)
                .Union(profiles
                    .Select(profile => new ProfileModel { ClientIdentifier = identifier, ProfileName = profile }))
                .ToArray();

            _allProfiles = allProfiles;

            UpdateProfiles();
        }

        public void UpdateClientState(ClientIdentifier[] clients)
        {
            var entry = $"Clients updated.";
            _logger.LogInformation(entry);
            Log += $"{DateTime.Now:s} {entry}{Environment.NewLine}";

            Clients = clients.ToList();

            //TODO: What if selected client is the disconnected one?
            SelectedClient ??= clients.FirstOrDefault();
            OnPropertyChanged(nameof(Clients));
        }
        
        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    //If ObservableCollection or similar: Application.Current.Dispatcher.Invoke(() => Clients.Add(...));
}
