<a name="readme-top"></a>
<!-- PROJECT LOGO -->
<br />
<div align="center">
  <h1 align="center">World Text</h1>
  <a align="center">A CS2 plugin that allows you to place configurable text on the map. <br>Optionally save placements to a database for multi-server support.</a>
  <br><br>
  <img src="https://github.com/user-attachments/assets/ab482c90-3d9b-4778-bfc8-d26f71e6b544" alt="" style="margin: 0;">
  <img src="https://github.com/user-attachments/assets/98cbcb3c-8192-4cab-9318-21c3717ad4b2" alt="" style="margin: 0;">
</div>

<!-- ABOUT THE PROJECT -->

### Dependencies

To use this plugin, you'll need the following dependencies installed:

- [**CounterStrikeSharp**](https://github.com/roflmuffin/CounterStrikeSharp): CounterStrikeSharp allows you to write server plugins in C# for Counter-Strike 2.
- [**K4-WorldText-API**](https://github.com/M-archand/K4-WorldText-API): This is a shared developer API to handle world text.
- [**CS2MenuManager (optional)**](https://github.com/schwarper/cs2menumanager): This is a shared developer API to handle menus. It's only required if you want to use the menu command (!mlist).

<!-- COMMANDS -->

## Commands

Default Access: @css/root, can be configured.
All commands can be configured, these are the default commands:
- !text # - Creates the wall text in front of the player and saves it to config file. E.g. `!text 1` will add the text from group 1 in the config. You can place each group in as many locations as you please.
- !rtext - Remove the closest world text from your position and deletes in from the config file.
- !mtext - Opens a menu that allows you to make location/angle adjustments to the text that you have placed (Requires [CS2MenuManager](https://github.com/schwarper/cs2menumanager) API)
- !importtext - Imports any existing JSON text placements into the database if you later decide to use a database.
- !reloadtext - Reloads the config and updates all text in the world.

<!-- CONFIG -->

## Configuration

- A config file will be generated on first use located in _/addons/counterstrikesharp/configs/WorldText_
- If `"EnableDatabase": false` the coordinates are saved in json files, located in _/addons/counterstrikesharp/plugins/WorldText/maps_
- Config example:
```
{
  "ConfigVersion": 3,
  "EnableDatabase": true,
  "DatabaseSettings": {
    "host": "",
    "database": "",
    "username": "",
    "password": "",
    "port": 3306,
    "sslmode": "None",
    "table-name": "world_text"
  },
  "RemoveCommand": "rtext", # !rtext will remove the closest text group
  "AddCommand": "text",     # !text 1 will place the text from group 1
  "MoveCommand": "mtext",   # !mtext will open the move menu
  "MenuType": "WasdMenu",   # The !mtext menu type. WasdMenu, ChatMenu, CenterHtmlMenu
  "MoveDistance": 5,
  "CommandPermission": "@css/root",
  "WorldText": {
    "1": {
      "bgEnable": true, # Enable/disable a background behind the text
      "bgIntensity": 2, # How dark the background is. E.g. 1 = see through, 3 = opaque black
      "bgWidth": 34,    # How wide the background should be
      "textAlignment": "center", # Left, Center, Right
      "fontSize": 24,
      "textScale": 0.45,
	    "zOffset": 0,     # How many units above the ground the bottom row of text will be
      "lines": [
        "{Red}First line of text from Group 1.",
        "{White}Second line of text from Group 1.",
        "{Red}Third line of text from Group 1."
      ]
    },
    "2": {
      "bgEnable": false,
      "bgIntensity": 1,
      "bgWidth": 34,
      "textAlignment": "left",
      "fontSize": 24,
      "textScale": 0.45,
	    "zOffset": 16,
      "lines": [
        "{Lime}First line of text from Group 2.",
        "{Magenta}Second line of text from Group 2.",
        "{White}Third line of text from Group 2."
      ]
    }
  }
}
```

<!-- ROADMAP -->

> [!IMPORTANT]
> Credits for the base plugin go to [K4ryuu](https://github.com/K4ryuu)! This plugin is built on top of the logic from his K4-WorldText-API.

<!-- LICENSE -->

## License

Distributed under the GPL-3.0 License. See `LICENSE.md` for more information.

<p align="right">(<a href="#readme-top">back to top</a>)</p>
