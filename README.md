# GoXLR TouchPortal Plugin
A TouchPortal port of the StreamDeck plugin by using the same API from the GoXLR App.

The plugin therefor supports:
- Get list of profiles
- Set a specific profile
- Doing changes to the routing table

### Setup:
Install build tools for .Net 5
Build and Publish to folder: `%appdata%\TouchPortal\plugins\GoXLR`
This folder should contain the `GoXLR.Worker.exe and` `entry.tp`

### Configuration
Start TouchPortal and accept the plugin
Start GoXLR and see that it would connect to the same PC

### Other
TouchPortal with the plugin must be started before GoXLR
If any StreamDeck changes is done in the GoXLR Global Settings, the GoXLR App could need to be restarted.

### Known issues
Unicode characters in profile names is not shown correctly.
