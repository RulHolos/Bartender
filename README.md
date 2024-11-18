# <img src="Bartender/images/icon.png" alt="bartender" width="32"> Bartender

Bartender is a FFXIV plugin which allows you to save and load your hotbars, mimicking vanilla's `/hotbar copy` command.

This implementation allows you to have virtually an infinite number of hotbar configurations (profiles).

Please create an [issue](https://github.com/AtaeKurri/Bartender/issues/new) if you find any bugs or want to see a new feature.

## Features

* A GUI to save/load hotbar profiles.
* `/barload <profile name>` is used to load (and populate) one or multiple hotbars with the saved icons.
* `/barclear <profile name>` clears the bars used by the profile of any icons.
* Automation of profile loading with conditions.

## Planned features

* Variables in `/barload` and `/barclear` commands.

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
Example: `/barload {jobShort}-test` is the same as `/boarload RPR-test` (if you're currently a Reaper)<br>
The currently available variables are:
- `job` - This is the name of the class/job in the game's lang. *Warning: in some languages, this name is all lowercase.* (e.g: reaper)
- `jobshort` - This is the abbreviated name of the class/job (e.g: RPR)
- `lvl` - The level of your current class/job

Note: The case doesn't matter for the variable name. `job` will be parsed the same as `jOB`.
