<h1 align="center">Home Screen Sections (Modular Home)</h1>
<h2 align="center">A Jellyfin Plugin</h2>
<p align="center">
	<img alt="Logo" width="256" height="256" src="https://camo.githubusercontent.com/ab4b1ec289bed0a0ac8dd2828c41b695dbfeaad8c82596339f09ce23b30d3eb3/68747470733a2f2f63646e2e6a7364656c6976722e6e65742f67682f73656c666873742f69636f6e732f776562702f6a656c6c7966696e2e77656270" />
	<br />
	<sub>Custom Logo Coming Soon</sub>
	<br />
	<br />
	<a href="https://github.com/IAmParadox27/jellyfin-plugin-home-sections">
		<img alt="GPL 3.0 License" src="https://img.shields.io/github/license/IAmParadox27/jellyfin-plugin-home-sections.svg" />
	</a>
	<a href="https://github.com/IAmParadox27/jellyfin-plugin-home-sections/releases">
		<img alt="Current Release" src="https://img.shields.io/github/release/IAmParadox27/jellyfin-plugin-home-sections.svg" />
	</a>
</p>

## Introduction
Home Screen Sections (commonly referred to as Modular Home) is a Jellyfin plugin which allows users to update their web client's home screen to be a bit more dynamic and more "Netflixy".

### Sections Included
A lot of the sections included are the base sections that would appear in a vanilla instance of Jellyfin. This has been done because using Modular Home hasn't been integrated to work side by side with the vanilla home screen and instead wholesale replaces it. Since a lot of the sections are useful and contain everything you'd want in a home screen they've been included for convenience.

> **NOTE**: Its worth noting that the sections that have been created are one's that I myself use for my own instance, if there is a section that's missing check the "Adding your own sections" heading below for info on how you can create your own.

These vanilla sections are listed here:

- My Media
	- Same as vanilla Jellyfin
- Continue Watching
	- Same as vanilla Jellyfin
- Next Up
	- Same as vanilla Jellyfin
- Recently Added Movies/TV Shows
	- Same as vanilla Jellyfin
- Live TV
	- Mostly the same as vanilla Jellyfin. _Current State is untested since updating to 10.10.3 so may find that there are issues_

The sections that are new for this plugin (and most likely the reason you would use this plugin in the first place) are outlined here:

- Because You Watched
	- Very similar to Netflix's "because you watched" section, a maximum of 5 of these will appear when the section is enabled
<img src="https://raw.githubusercontent.com/IAmParadox27/jellyfin-plugin-home-sections/refs/heads/main/screenshots/because-you-watched.png" alt="Because You Watched Preview" />

- Watch Again
	- Again similar to Netflix's feature of the same name, this will request Movies in a Collection and TV Shows that have been watched to their completion and will provide the user an option to watch the show/movie collection again. The listed entry will be the first movie to be released in that collection (done by Premiere Date) or the first episode in the series
<img src="https://raw.githubusercontent.com/IAmParadox27/jellyfin-plugin-home-sections/refs/heads/main/screenshots/watch-again.png" alt="Watch Again Preview" />

- My List
	- Again similar to Netflix's feature. This requires a bit more setup than the others to get working. It looks for a playlist called "My List" and retrieves the entries in that playlist.
<img src="https://raw.githubusercontent.com/IAmParadox27/jellyfin-plugin-home-sections/refs/heads/main/screenshots/my-list.png" alt="My List Preview" />

## Installation

### Prerequisites
- This plugin is based on Jellyfin Version `10.10.3`
- A custom deployment of `jellyfin-web` is required to run this plugin. It can be cloned from https://github.com/IAmParadox27/jellyfin-web or there is release with the data pre-packaged for simpler install available to download.
- The `jellyfin-web` directory should be writable by whatever user is running the Jellyfin server instance. If on Windows this might mean changing the permissions for `C:\Program Files\Jellyfin\Server\jellyfin-web` to allow write access the Jellyfin user.
- **Plugin Pages** (https://github.com/IAmParadox27/jellyfin-plugin-pages) is required to be installed. It allow this plugin to be enabled/configured per user, rather than server configured. This was a decision taken at the start so that I could use it, while my users didn't have to.
### Installation
1. Add `https://www.iamparadox.dev/jellyfin/plugins/manifest.json` to your plugin repositories.
2. Install `Home Screen Sections` from the Catalogue.
3. Restart Jellyfin.
4. On the user's homepage, open the hamburger menu and you should see a link for settings to "Modular Home". Click this.
5. At the top there is a button to enable support and it will retrieve all sections that are available on your instance. Select all that apply.
6. Save the settings. _Please note currently the user is not provided any feedback when the settings are saved_.
7. Force refresh your webpage (or app) and you should see your new sections instead of the original ones.
## Upcoming Features/Known Issues
If you find an issue with any of the sections or usage of the plugin, please open an issue on GitHub, see below the issues I know about already which are planned to be solved in due course.

| Feature                     | Status  | Comments                                                                                                                                                                                                                               |
| --------------------------- | ------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Server Side Configuration   | Planned | The plugin doesn't have any server side configuration at the moment, there are plans to add some configuration on the server side to configure things like "which sections are available" and "how many of some sections are allowed". |
| Save Settings User Feedback | Bug     | Currently when the user saves their settings the webpage doesn't tell them that the settings have been saved successfully.                                                                                                             |
### FAQ

#### How can I tell if its worked?

> The easiest way to confirm whether the user is using the modular home settings is to check whether the movie posters are portrait or landscape. Due to how the cards are delivered from the backend all cards are forced to be landscape
## Contribution
### Adding your own sections
> This is great an' all but I want a section that doesn't exist here. Can I make one?

Yep! Home Screen Sections supports "plugins" 😅. 

Currently the plugin isn't available on NuGet (this is planned) however you can reference the DLL in a .NET library project.

When you have a project created. Make a new type and inherit from `IHomeScreenSection`. Implement the required properties/functions and away you go. 

_When referencing Jellyfin NuGet packages please ensure that you reference the same version that is references by Home Screen Sections to avoid any conflicts._

### Pull Requests
I'm open to any and all pull requests that expand the functionality of this plugin, while keeping within the scope of what its outlined to do, however if the PR includes new sections which **are not** vanilla implementations it will be rejected as the above approach is preferred.

I don't want this plugin to bloat with lots of section types another server admin might not want, best to keep those as "plugin plugins".
