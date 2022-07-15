# GoXLR TouchPortal Plugin (unofficial)
A TouchPortal port of the StreamDeck plugin by using the same API from the GoXLR App.

Created for **both Windows and Mac OS X** (different builds)

**Preview build** with support for states [availible for download](https://github.com/oddbear/GoXLR-TouchPortal-Plugin/releases/tag/v0.17.1-pre)

WARNING: From next version (1.4.4+) of the GoXLR App there will be some breaking changes for this plugin. Therefor a updated version of this plugin is required (for SD v0.17.0+). <br />
All versions before this, you will have problems fetching the list of the profiles.

The plugin therefor supports:
- Get list of profiles
- Set a specific profile
- Changes to the routing table

Additional features:
- Mac OS X Support
- Possible to control multiple PCs
- Status indicators: client count, and client name (when single pc)

For aditional information, check the [Wiki](https://github.com/oddbear/GoXLR-TouchPortal-Plugin/wiki)

### Setup:

Download latest [.NET 5.0 Console Runtime](https://dotnet.microsoft.com/download/dotnet/5.0/runtime) from Microsoft.

- Only the correct runtime will work. For a plugin targeting .Net 5, only .Net runtime will work (not .Net 6, or .Net 4.x).

Download the latest tpp file from [releases](https://github.com/oddbear/GoXLR-TouchPortal-Plugin/releases) (under assets):

- Plugin for [Windows](https://github.com/oddbear/GoXLR-TouchPortal-Plugin/releases/download/v0.7.0/TouchPortal.GoXLR.Plugin.Windows.tpp)
- Plugin for [Mac OS X](https://github.com/oddbear/GoXLR-TouchPortal-Plugin/releases/download/v0.7.0/TouchPortal.GoXLR.Plugin.MacOSX.zip)

Install tpp file from TouchPortal or unzip .tpp file to (Windows): `%appdata%\TouchPortal\plugins\GoXLR.Plugin`

This folder should then contain the `GoXLR.Plugin.exe` and `entry.tp`

### Configuration

1. Start TouchPortal and accept/trust the plugin, and the firewall prompt [(se Wiki for more)](https://github.com/oddbear/GoXLR-TouchPortal-Plugin/wiki/Setting-up-Firewall).
2. Start GoXLR App, and check that the Connect/Reconnect is configured (under 'SYSTEM' > 'Global Settings').
   1. TouchPortal on same computer, uncheck 'Streamdeck on non-GOXLR PC'.
   2. TouchPortal on another computer, check, and provide the IP.
3. If you changed anything, restart the GoXLR App (this is important, as it only connects to the plugin on startup).

### Other Information

TouchPortal with the plugin **must** be started **before** the GoXLR App
If **any** StreamDeck changes is done in the GoXLR App 'Global Settings', the GoXLR App would **must** be restarted *(this is a limitation by the GoXLR App)*.
If there is any problems, try restarting the GoXLR App.

### Other plugins and tools for GoXLR users

- [Audio Monitor](https://github.com/oddbear/TouchPortal.Plugin.AudioMonitor)
> Audio Monitor Meter for your `Char Mic`, `RTX Voice` etc. on your device through Touch Portal.<br />
> This is helpfull after the 2021 patch that made it possible to free up a fader by moving the `Chat Mic` to the `Cough` button.
- [GoXLR Force Defaults](https://github.com/oddbear/GoXLR-Force-Defaults)
> A Windows Service that automaticly changes the default audio devices to be correctly for the GoXLR.<br />
> It's a known problem that Windows tries to be helpfull and changes the default devices, volume, mute etc. at it's own will.
- [HotKey](https://github.com/oddbear/TouchPortal.Plugin.HotKey)
> Adds keyboard hotkeys to Touch Portal.

### Tooling (Windows only)

Tooling used for development/troubleshooting. GUI Server and Client for simulating a device or the plugin.
Tooling needs .NET 5.0 Desktop Runtime https://dotnet.microsoft.com/download/dotnet/5.0/runtime

- [GoXLR Simulator](https://github.com/oddbear/GoXLR-TouchPortal-Plugin/releases/download/v0.7.0/Windows.GUI.Tooling.GoXLR.Simulator.zip)
- [Plugin Simulator](https://github.com/oddbear/GoXLR-TouchPortal-Plugin/releases/download/v0.7.0/Windows.GUI.Tooling.Plugin.Simulator.zip)

### Known issues

- Cannot run at the same time as other plugins using same port (external limitation).
- Routing on and off Status not shown (planned update to the GoXLR App).
- Or check out [Issues](https://github.com/oddbear/GoXLR-TouchPortal-Plugin/issues) for things being working on.

### Dependencies

- [Fleck](https://github.com/statianzo/Fleck)
- [TouchPortalSDK](https://github.com/oddbear/TouchPortalSDK)
- [Fody.PropertyChanged](https://github.com/Fody/PropertyChanged) (Tooling only)
