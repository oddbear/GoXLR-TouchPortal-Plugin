using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using GoXLR.Server;
using GoXLR.Server.Models;
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
        
        public IEnumerable<string> Profiles { get; set; }
        public string SelectedProfile { get; set; }

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
            
            Clients = new List<ClientIdentifier>();

            Inputs = new [] { "Mic", "Chat", "Music", "Game", "Console", "Line In", "System", "Samples" };
            SelectedInput = Inputs.First();

            Outputs = new [] { "Headphones", "Broadcast Mix", "Line Out", "Chat Mic", "Sampler" };
            SelectedOutput = Outputs.First();

            Actions = new [] { "Turn On", "Turn Off", "Toggle" };
            SelectedAction = Actions.First();
            
            _server = server;

            _server.UpdateConnectedClientsEvent = UpdateClientState;
        }

        public void GetProfiles()
        {
            var client = SelectedClient;
            
            var profiles = _server.ClientData
                       .FirstOrDefault(clientData => clientData.ClientIdentifier == client)
                       ?.Profiles ?? Array.Empty<string>();

            Profiles = profiles;
            SelectedProfile = profiles.FirstOrDefault();

            LogInformation($"Profiles updated: {string.Join(", ", Profiles)}");
        }

        public void SetProfile()
        {
            var client = SelectedClient;

            var profile = SelectedProfile;

            _server.SetProfile(client, profile);

            LogInformation($"Set Profile: {profile}");
        }

        public void SetRouting()
        {
            var client = SelectedClient;

            var action = SelectedAction;
            var input = SelectedInput;
            var output = SelectedOutput;

            _server.SetRouting(client, action, input, output);
            
            LogInformation($"Set Routing: {input}, {output}, {action}");
        }
        
        public void UpdateClientState()
        {
            var clients = _server.ClientData
                .Select(clientData => clientData.ClientIdentifier)
                .ToList();

            Clients = clients;

            SelectedClient ??= clients.FirstOrDefault();
            //OnPropertyChanged(nameof(Clients));

            var entry = $"Clients updated: {string.Join(", ", clients.Select(client => $"{client.ClientIpAddress}:{client.ClientPort}"))}";
            LogInformation(entry);
        }

        //If ObservableCollection or similar: Application.Current.Dispatcher.Invoke(() => Clients.Add(...));
        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void LogInformation(string entry)
        {
            _logger.LogInformation(entry);
            Log += $"{DateTime.Now:s} {entry}{Environment.NewLine}";
        }
    }
}
