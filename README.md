# GoXLR TouchPortal Plugin (unofficial)
A TouchPortal port of the StreamDeck plugin by using the same API from the GoXLR App.

Created for **both Windows and Mac OS X** (different builds)

The plugin therefor supports:
- Get list of profiles
- Set a specific profile
- Changes to the routing table

Additional features:
- Mac OS X Support
- Possible to control multiple PCs
- Status indicators: client count, and client name (when single pc)

### Setup:

Download latest .NET Runtime from (5.0.0 or higher) https://dotnet.microsoft.com/download

Download the latest tpp file from (under assets): https://github.com/oddbear/GoXLR-TouchPortal-Plugin/releases

Install tpp file from TouchPortal or unzip .tpp file to (Windows): `%appdata%\TouchPortal\plugins\GoXLR.Plugin`

This folder should then contain the `GoXLR.Plugin.exe and` `entry.tp`

### Configuration

Start TouchPortal and accept/trust the plugin
Start GoXLR and see that it would connect to the same PC

### Other

TouchPortal with the plugin must be started before GoXLR
If any StreamDeck changes is done in the GoXLR Global Settings, the GoXLR App could need to be restarted.

### Tooling (Windows only)

Tooling for development, GUI Server/Client for simulating a device.
Tooling needs .NET Desktop Runtime 5.0.0 from https://dotnet.microsoft.com/download/dotnet/5.0

### Known issues

- Unicode characters in profile names is not shown correctly.
- Cannot run at the same time as other plugins using same port.
- Routing on and off Status not shown (planned update to the GoXLR App).

### Dependencies

- [.Net TouchPortalApi](https://github.com/tlewis17/TouchPortalAPI)
- [Fleck](https://github.com/statianzo/Fleck)
- [Fody.PropertyChanged](https://github.com/Fody/PropertyChanged)
