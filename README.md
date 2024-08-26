<a name="readme-top"></a>

> [!IMPORTANT]
> Credits for the base plugin go to [K4ryuu](https://github.com/K4ryuu)! I made some changes and added several extra features. Can't fork it twice so had to make a new repo.

<!-- PROJECT LOGO -->
<br />
<div align="center">
  <h1 align="center">Wall Text</h1>
  <a align="center">Creates text to display on the map.<br>Updates on map start.</a><br>
  <br>
  <img src="https://files.catbox.moe/fehppv.png" alt="" style="margin: 0;">

  <p align="center">
    <br />
    <a href="https://github.com/M-archand/WallText/releases/tag/1.0.0">Download</a>
  </p>
</div>

<!-- ABOUT THE PROJECT -->

### Dependencies

To use this server addon, you'll need the following dependencies installed:

- [**CounterStrikeSharp**](https://github.com/roflmuffin/CounterStrikeSharp/releases): CounterStrikeSharp allows you to write server plugins in C# for Counter-Strike 2.
- [**K4-WorldText-API**](https://github.com/K4ryuu/K4-WorldText-API): This is a shared developer API to handle world text.

<!-- COMMANDS -->

## Commands

Default Access: @css/root, can be configured.
All commands can be configured, these are the default commands:
- !walltext - Creates the wall text in front of the player and saves it to config file.
- !remove   - Remove the closest wall text from your position and deletes in from the config file. (100 units max)

<!-- CONFIG -->

## Configuration

- A config file will be generated on first use located in _/addons/counterstrikesharp/configs/WallText_
- The coordinates are saved in json files, located in _/addons/counterstrikesharp/plugins/WallText/maps_
- You can see an example with detailed comments here: [WallText.example.json](https://github.com/M-archand/WallText/blob/main/WallText.example.json)
<!-- ROADMAP -->

## Roadmap

- [X] Update for SharpTimer usage.
- [X] Add color configs. See [here](https://i.sstatic.net/lsuz4.png) for color names.
- [X] Add left alignment.
- [X] Add font size & scale to config.
- [X] Fix inconsistent results for !removelist
- [X] Add configurabled commands.
- [X] Add configurable permissions.
- [ ] Add minor adjustment commands

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- LICENSE -->

## License

Distributed under the GPL-3.0 License. See `LICENSE.md` for more information.

<p align="right">(<a href="#readme-top">back to top</a>)</p>
