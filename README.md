<p align="center" width="50%">
    <img width="20%" src="Bartender/images/icon.png">
</p><h1 align="center">Bartender (v1.1.7.0)</h1>

Bartender is a FFXIV plugin which allows you to save and load your hotbars, mimicking vanilla's `/hotbar copy` command.

This implementation allows you to have virtually an infinite number of hotbar configurations (profiles).

Please create an [issue](https://github.com/AtaeKurri/Bartender/issues/new) if you find any bugs or want to see a new feature.

## Features

* A GUI to save/load hotbar profiles.
* `/barload <profile name>` is used to load (and populate) one or multiple hotbars with the saved icons.
* `/barclear <profile name>` clears the bars used by the profile of any icons.
* Automation of profile loading with conditions.

Word to gamepad players: Bartender doesn't support cross hotbars.

## Planned features

* Export to xivbars

## Usage

**Basic usage goes as so:**
-> Create a new profile.
-> Fill your bars with actions.
-> Click `Save current hotbars`.
-> Check the hotbars you want to use/load.

You can then load your profile using the `/barload <profile name>` command inside the chat, in the config or in a macro.

## Variables

You can use variables inside `/barload` and `/barclear`.<br>
To do so, put the name of the variable between `{}` (e.g: `{job}`)<br>
Example: `/barload {jobShort}-test` translates to `/barload RPR-test` (if you're currently a Reaper)<br>
The currently available variables are:
- `job` - This is the name of the class/job in the game's lang. *Warning: in some languages, this name is all lowercase.* (e.g: reaper)
- `jobshort` - This is the abbreviated name of the class/job (e.g: RPR)
- `lvl`/`level` - The level of your current class/job

Note: The case doesn't matter for the variable name. For example, `job` will be parsed the same as `jOB`.

## Profiles Hotbar

Bartender allows you to display a permanent configurable hotbar-like interface on your HUD displaying your profiles.<br>
This allows you to not waste actual hotbar spaces for your macros if you don't wish to use them.
