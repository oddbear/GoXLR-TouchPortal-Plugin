# GoXLR TouchPortal Plugin (unofficial)
A TouchPortal port of the StreamDeck plugin by using the same API from the GoXLR App.

The plugin therefor supports:
- Get list of profiles
- Set a specific profile
- Changes to the routing table

### Setup:
Download .NET Runtime 5.0.0 from https://dotnet.microsoft.com/download/dotnet/5.0
Install tpp file from TouchPortal or unzip .tpp file to: `%appdata%\TouchPortal\plugins\GoXLR`
This folder should then contain the `GoXLR.Worker.exe and` `entry.tp`

### Configuration
Start TouchPortal and accept the plugin
Start GoXLR and see that it would connect to the same PC

### Other
TouchPortal with the plugin must be started before GoXLR
If any StreamDeck changes is done in the GoXLR Global Settings, the GoXLR App could need to be restarted.

### Tooling
Tooling for development, GUI Server/Client for simulating a device.
Tooling needs .NET Desktop Runtime 5.0.0 from https://dotnet.microsoft.com/download/dotnet/5.0

### Known issues
Unicode characters in profile names is not shown correctly.

### Dependencies

- [.Net TouchPortalApi](https://github.com/tlewis17/TouchPortalAPI)
- [Watson Websocket](https://github.com/jchristn/WatsonWebsocket)
